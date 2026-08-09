using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                var balance = await Web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Ethereum account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                // Ethereum doesn't use seed phrases directly for account creation via Nethereum
                result.Result = (publicKey, privateKey, string.Empty);
                result.IsError = false;
                result.Message = "Ethereum account created successfully. Seed phrase not applicable for direct key generation.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Ethereum account: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
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
                    result.Message = "Ethereum account restored successfully from seed phrase.";
                }
                catch (Exception walletEx)
                {
                    // If HD wallet fails, try treating seedPhrase as a private key
                    try
                    {
                        var account = new Account(seedPhrase);
                        result.Result = (account.Address, account.PrivateKey);
                        result.IsError = false;
                        result.Message = "Ethereum account restored successfully from private key.";
                    }
                    catch
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to restore account from seed phrase or private key: {walletEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Ethereum account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
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

                var account = new Account(senderPrivateKey, ChainId);
                var web3 = CreateWeb3WithAccount(account, HostURI);

                // For bridge withdrawals, send to OASIS bridge pool address
                var bridgePoolAddress = _oasisAccount?.Address ?? ContractAddress;
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }
                if (_oasisAccount == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum OASIS account is not initialized");
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
                var transactionReceipt = await Web3Client.Eth.GetEtherTransferService()
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                var transactionReceipt = await Web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

                if (transactionReceipt == null)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found.";
                }
                else if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = BridgeTransactionStatus.Completed;
                    result.IsError = false;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Canceled;
                    result.IsError = true;
                    result.Message = "Transaction failed on chain.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Ethereum transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in SendNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (transaction == null || string.IsNullOrWhiteSpace(transaction.TokenAddress) ||
                    string.IsNullOrWhiteSpace(transaction.ToWalletAddress) ||
                    string.IsNullOrWhiteSpace(transaction.FromWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address, from wallet address, and to wallet address are required");
                    return result;
                }

                // Get private key for sender
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(Guid.Empty, Core.Enums.ProviderType.EthereumOASIS);
                string privateKey = null;
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    // Try to get from request if available
                    if (transaction is SendWeb3NFTRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletAddress))
                    {
                        // For now, we need the private key - this should come from KeyManager based on FromWalletAddress
                        OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for sender wallet");
                        return result;
                    }
                }
                else
                {
                    privateKey = keysResult.Result[0];
                }

                var senderAccount = new Account(privateKey, ChainId);
                var web3 = CreateWeb3WithAccount(senderAccount, HostURI);

                // ERC-721 transferFrom function ABI
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, transaction.TokenAddress);
                var transferFunction = erc721Contract.GetFunction("transferFrom");

                var tokenId = BigInteger.Parse(transaction.TokenId ?? "0");
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    transaction.FromWalletAddress,
                    transaction.ToWalletAddress,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 transfer failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = transaction.TokenAddress,
                    SendNFTTransactionHash = receipt.TransactionHash
                };
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transaction)
        {
            return MintNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in MintNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (transaction == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // Get private key from KeyManager using MintedByAvatarId
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(transaction.MintedByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderAccount = new Account(keysResult.Result[0], ChainId);
                var web3 = CreateWeb3WithAccount(senderAccount, HostURI);

                // Use contract address or default NFT contract
                var nftContractAddress = _contractAddress ?? ContractAddress ?? "0x0000000000000000000000000000000000000000";
                
                // ERC-721 mint function ABI (assuming contract has mint function)
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, nftContractAddress);
                var mintFunction = erc721Contract.GetFunction("mint");

                // Generate token ID (in production, this should be managed properly)
                var tokenId = new BigInteger(DateTime.UtcNow.Ticks);
                var mintToAddress = transaction.SendToAddressAfterMinting ?? senderAccount.Address;

                var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    mintToAddress,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 mint failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = nftContractAddress,
                    MintTransactionHash = receipt.TransactionHash,
                    NFTMintedUsingWalletAddress = senderAccount.Address,
                    OASISMintWalletAddress = _oasisAccount?.Address ?? senderAccount.Address
                };
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

    }
}
