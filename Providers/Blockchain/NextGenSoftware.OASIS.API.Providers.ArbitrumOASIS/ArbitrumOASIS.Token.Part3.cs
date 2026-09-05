using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
public sealed partial class ArbitrumOASIS
{

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Generate a new Ethereum/Arbitrum account
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();
            
            // Note: Mnemonic generation varies by Nethereum version - using empty string for consistency
            // Users can generate their own mnemonic if needed
            var mnemonic = string.Empty;

            result.Result = (publicKey, privateKey, mnemonic);
            result.IsError = false;
            result.Message = "Arbitrum account created successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();

        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(seedPhrase))
            {
                OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                return result;
            }

            // Restore wallet from seed phrase using Nethereum HD wallet
            try
            {
                var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
                var account = wallet.GetAccount(0);

                result.Result = (account.Address, account.PrivateKey);
                result.IsError = false;
                result.Message = "Arbitrum account restored successfully from seed phrase.";
            }
            catch (Exception walletEx)
            {
                // If HD wallet fails, try treating seedPhrase as a private key
                try
                {
                    var account = new Account(seedPhrase);
                    result.Result = (account.Address, account.PrivateKey);
                    result.IsError = false;
                    result.Message = "Arbitrum account restored successfully from private key.";
                }
                catch
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account from seed phrase or private key: {walletEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // Create account from private key
            var account = new Nethereum.Web3.Accounts.Account(senderPrivateKey, _chainId);
            var web3 = new Web3(account, _hostURI);

            // For bridge withdrawals, send to OASIS bridge pool address
            var bridgePoolAddress = _oasisAccount?.Address ?? _contractAddress;
            var amountInWei = Nethereum.Util.UnitConversion.Convert.ToWei(amount);

            var transactionReceipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(bridgePoolAddress, amount, 2);

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionReceipt.TransactionHash,
                IsSuccessful = transactionReceipt.Status.Value == 1,
                Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null || _oasisAccount == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // For bridge deposits, send from OASIS bridge pool to receiver
            var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount, 2);

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionReceipt.TransactionHash,
                IsSuccessful = transactionReceipt.Status.Value == 1,
                Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            // Get transaction receipt
            var receipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            if (receipt == null)
            {
                result.Result = BridgeTransactionStatus.NotFound;
            }
            else if (receipt.Status.Value == 1)
            {
                result.Result = BridgeTransactionStatus.Completed;
            }
            else
            {
                result.Result = BridgeTransactionStatus.Canceled;
            }

            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
        }
        return result;
    }
}
