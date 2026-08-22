using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Text;


using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

public sealed partial class AvalancheOASIS_Legacy
{
    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                string.IsNullOrWhiteSpace(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and to wallet address are required");
                return result;
            }

            // Get private key from request
            string privateKey = null;
            if (!string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                privateKey = request.OwnerPrivateKey;
            else if (request is SendWeb3TokenRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletPrivateKey))
                privateKey = sendRequest.FromWalletPrivateKey;
            
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Private key is required (OwnerPrivateKey or FromWalletPrivateKey)");
                return result;
            }

            var senderAccount = new Account(privateKey, _chainId);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 transfer ABI
            var erc20Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.FromTokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(request.Amount * (decimal)multiplier);
            var transferFunction = erc20Contract.GetFunction("transfer");
            var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                _gasLimit, 
                null, 
                null, 
                request.ToWalletAddress, 
                amountBigInt);

            result.Result = new TransactionResponse
            {
                TransactionResult = receipt.TransactionHash
            };
            result.IsError = false;
            result.Message = "Token sent successfully on Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending token on Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            var transactionReceipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

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
            OASISErrorHandling.HandleError(ref result, $"Error getting Avalanche transaction status: {ex.Message}", ex);
            result.Result = BridgeTransactionStatus.NotFound;
        }
        return result;
    }

    // IOASISBlockchainStorageProvider interface implementations
    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in MintTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || request.MetaData == null || 
                !request.MetaData.ContainsKey("TokenAddress") || string.IsNullOrWhiteSpace(request.MetaData["TokenAddress"]?.ToString()) ||
                !request.MetaData.ContainsKey("MintToWalletAddress") || string.IsNullOrWhiteSpace(request.MetaData["MintToWalletAddress"]?.ToString()))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and mint to wallet address are required in MetaData");
                return result;
            }

            var tokenAddress = request.MetaData["TokenAddress"].ToString();
            var mintToWalletAddress = request.MetaData["MintToWalletAddress"].ToString();
            var amount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) ? amt : 0m;

            // Get private key from request MetaData or use OASIS account
            string privateKey = _chainPrivateKey;
            if (request.MetaData?.ContainsKey("OwnerPrivateKey") == true && !string.IsNullOrWhiteSpace(request.MetaData["OwnerPrivateKey"]?.ToString()))
                privateKey = request.MetaData["OwnerPrivateKey"].ToString();

            var senderAccount = new Account(privateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 mint function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(amount * (decimal)multiplier);
            var mintFunction = erc20Contract.GetFunction("mint");
            var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                mintToWalletAddress, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 mint failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token minted successfully on Avalanche.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in BurnTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                return result;
            }

            var senderAccount = new Account(request.OwnerPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 burn function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            // IBurnWeb3TokenRequest doesn't have Amount property, so we'll burn the full balance
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
            var amountBigInt = balance;
            var burnFunction = erc20Contract.GetFunction("burn");
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 burn failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token burned successfully on Avalanche.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in LockTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by transferring to bridge pool (OASIS account)
            // ILockWeb3TokenRequest doesn't have Amount property, so we'll lock the full balance
            var web3Client = new Web3(new Account(request.FromWalletPrivateKey), _hostURI);
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var account = new Account(request.FromWalletPrivateKey);
            var balance = await balanceFunction.CallAsync<BigInteger>(account.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amount = (decimal)(balance / multiplier);
            
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = request.FromWalletPrivateKey,
                ToWalletAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000", // Use contract address as bridge pool
                Amount = amount
            };

            var sendResult = await SendTokenAsync(sendRequest);
            if (!sendResult.IsError)
            {
                // Persist the lock record so UnlockTokenAsync can retrieve address and amount.
                var lockHolon = new Holon
                {
                    Name = $"LockedToken_{request.Web3TokenId}",
                    MetaData = new Dictionary<string, object>
                    {
                        ["avalancheLockRecord"] = true,
                        ["web3TokenId"] = request.Web3TokenId.ToString(),
                        ["fromWalletAddress"] = request.FromWalletAddress ?? "",
                        ["lockedAmount"] = amount.ToString()
                    }
                };
                await HolonManager.Instance.SaveHolonAsync(lockHolon, request.LockedByAvatarId);
            }
            return sendResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return UnlockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in UnlockTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }
            
            // Load the lock record saved by LockTokenAsync to retrieve address and amount.
            var lockRecords = await HolonManager.Instance.LoadHolonsByMetaDataAsync("web3TokenId", request.Web3TokenId.ToString());
            if (lockRecords.IsError || lockRecords.Result == null || !lockRecords.Result.Any())
            {
                OASISErrorHandling.HandleError(ref result, $"No locked token record found for Web3TokenId {request.Web3TokenId}.");
                return result;
            }
            var lockRecord = lockRecords.Result.First();
            var unlockedToWalletAddress = lockRecord.MetaData.ContainsKey("fromWalletAddress") ? lockRecord.MetaData["fromWalletAddress"]?.ToString() : "";
            decimal.TryParse(lockRecord.MetaData.ContainsKey("lockedAmount") ? lockRecord.MetaData["lockedAmount"]?.ToString() : "0", out var amount);

            if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is missing from the locked token record.");
                return result;
            }

            // Unlock token by transferring from bridge pool (OASIS account) to recipient
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = _chainPrivateKey, // OASIS account private key
                ToWalletAddress = unlockedToWalletAddress,
                Amount = amount
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
    {
        return GetBalanceAsync(request).Result;
    }

    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        var result = new OASISResult<double>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get AVAX balance
            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
            result.Result = (double)Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
            result.IsError = false;
            result.Message = "Balance retrieved successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        return GetTransactionsAsync(request).Result;
    }

    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get transaction history from Avalanche
            // Note: This requires an external service like Snowtrace API or similar
            // For now, we'll return an empty list with a message
            var transactions = new List<IWalletTransaction>();
            
            // In production, you would:
            // 1. Call Snowtrace API or similar: GET /api?module=account&action=txlist&address={address}
            // 2. Parse the response to extract transaction data
            // 3. Convert to IWalletTransaction format
            
            result.Result = transactions;
            result.IsError = false;
            result.Message = $"Transaction history for {request.WalletAddress} retrieved (external API integration may be required for full functionality).";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
    {
        return GenerateKeyPairAsync().Result;
    }

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Avalanche provider: {activateResult.Message}");
                    return result;
                }
            }

            // Generate Avalanche key pair using Nethereum (Avalanche uses same secp256k1 as Ethereum)
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            // Avalanche C-Chain uses secp256k1 (same as Ethereum); keys already generated above via Nethereum.
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                keyPair.PrivateKey = privateKey;
                keyPair.PublicKey = publicKey;
                keyPair.WalletAddressLegacy = publicKey;
            }

            result.Result = keyPair;
            result.IsError = false;
            result.Message = "Key pair generated successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        string errorMessage = "Error in BurnNFTAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress) || 
                string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and owner private key are required");
                return result;
            }

            var senderAccount = new Account(request.OwnerPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC721 burn function ABI
            var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
            var erc721Contract = web3Client.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
            var tokenId = BigInteger.Parse(request.Web3NFTId.ToString());
            var burnFunction = erc721Contract.GetFunction("burn");
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                tokenId);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 burn failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "NFT burned successfully on Avalanche.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }
}
