using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS
{
    public partial class BNBChainOASIS_Legacy
    {
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Send NFT using smart contract
                var nftData = new
                {
                    fromAddress = request.FromWalletAddress,
                    toAddress = request.ToWalletAddress,
                    nftTokenId = request.TokenId.ToString(),
                    amount = request.Amount,
                    metadata = JsonSerializer.Serialize(new Dictionary<string, object>())
                };

                var sendNFTFunction = _contract.GetFunction("sendNFT");
                var gasEstimate = await sendNFTFunction.EstimateGasAsync(
                    nftData.fromAddress,
                    nftData.toAddress,
                    nftData.nftTokenId,
                    nftData.amount,
                    nftData.metadata
                );

                var transactionReceipt = await sendNFTFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    nftData.fromAddress,
                    nftData.toAddress,
                    nftData.nftTokenId,
                    nftData.amount,
                    nftData.metadata
                );

                if (transactionReceipt != null && transactionReceipt.Status.Value == 1)
                {
                    var nftResponse = new Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionReceipt.TransactionHash,
                        SendNFTTransactionResult = transactionReceipt.TransactionHash,
                        // IsSuccessful removed - not in Web3NFTTransactionResponse
                    };

                    result.Result = nftResponse;
                    result.IsError = false;
                    result.Message = $"NFT sent successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT on BNB Chain: {ex.Message}");
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Mint NFT using smart contract
                var nftData = new
                {
                    mintedByAvatarId = request.MintedByAvatarId.ToString(),
                    title = request.Title,
                    description = request.Description,
                    imageUrl = request.ImageUrl,
                    thumbnailUrl = request.ThumbnailUrl,
                    price = request.Price,
                    discount = request.Discount,
                    memoText = request.MemoText,
                    numberToMint = request.NumberToMint,
                    storeNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                    metadata = JsonSerializer.Serialize(request.MetaData ?? new Dictionary<string, string>()),
                    tags = JsonSerializer.Serialize(request.Tags ?? new List<string>()),
                    offChainProvider = request.OffChainProvider?.ToString() ?? "None",
                    onChainProvider = request.OnChainProvider?.ToString() ?? "None",
                    nftStandardType = request.NFTStandardType?.ToString() ?? "ERC721",
                    nftOffChainMetaType = request.NFTOffChainMetaType?.ToString() ?? "JSON",
                    symbol = request.Symbol,
                    jsonMetaDataURL = request.JSONMetaDataURL,
                    jsonMetaData = request.JSONMetaData,
                    waitTillNFTMinted = request.WaitTillNFTMinted,
                    waitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                    attemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                    sendToAddressAfterMinting = request.SendToAddressAfterMinting,
                    sendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId.ToString(),
                    sendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                    sendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                    waitTillNFTSent = request.WaitTillNFTSent,
                    waitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                    attemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds
                };

                var mintNFTFunction = _contract.GetFunction("mintNFT");
                var gasEstimate = await mintNFTFunction.EstimateGasAsync(
                    nftData.mintedByAvatarId,
                    nftData.title,
                    nftData.description,
                    nftData.imageUrl,
                    nftData.thumbnailUrl,
                    nftData.price,
                    nftData.discount,
                    nftData.memoText,
                    nftData.numberToMint,
                    nftData.storeNFTMetaDataOnChain,
                    nftData.metadata,
                    nftData.tags,
                    nftData.offChainProvider,
                    nftData.onChainProvider,
                    nftData.nftStandardType,
                    nftData.nftOffChainMetaType,
                    nftData.symbol,
                    nftData.jsonMetaDataURL,
                    nftData.jsonMetaData,
                    nftData.waitTillNFTMinted,
                    nftData.waitForNFTToMintInSeconds,
                    nftData.attemptToMintEveryXSeconds,
                    nftData.sendToAddressAfterMinting,
                    nftData.sendToAvatarAfterMintingId,
                    nftData.sendToAvatarAfterMintingUsername,
                    nftData.sendToAvatarAfterMintingEmail,
                    nftData.waitTillNFTSent,
                    nftData.waitForNFTToSendInSeconds,
                    nftData.attemptToSendEveryXSeconds
                );

                var transactionReceipt = await mintNFTFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    nftData.mintedByAvatarId,
                    nftData.title,
                    nftData.description,
                    nftData.imageUrl,
                    nftData.thumbnailUrl,
                    nftData.price,
                    nftData.discount,
                    nftData.memoText,
                    nftData.numberToMint,
                    nftData.storeNFTMetaDataOnChain,
                    nftData.metadata,
                    nftData.tags,
                    nftData.offChainProvider,
                    nftData.onChainProvider,
                    nftData.nftStandardType,
                    nftData.nftOffChainMetaType,
                    nftData.symbol,
                    nftData.jsonMetaDataURL,
                    nftData.jsonMetaData,
                    nftData.waitTillNFTMinted,
                    nftData.waitForNFTToMintInSeconds,
                    nftData.attemptToMintEveryXSeconds,
                    nftData.sendToAddressAfterMinting,
                    nftData.sendToAvatarAfterMintingId,
                    nftData.sendToAvatarAfterMintingUsername,
                    nftData.sendToAvatarAfterMintingEmail,
                    nftData.waitTillNFTSent,
                    nftData.waitForNFTToSendInSeconds,
                    nftData.attemptToSendEveryXSeconds
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    var nftResponse = new BNBChainTransactionResponse
                    {
                        TransactionResult = transactionReceipt.TransactionHash,
                        MemoText = $"NFT minted successfully: {nftData.title}"
                    };

                    result.Result = (IWeb3NFTTransactionResponse)nftResponse;
                    result.IsError = false;
                    result.Message = $"NFT minted successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction failed on BNB Chain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT on BNB Chain: {ex.Message}");
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Smart contract not initialized");
                    return result;
                }

                // Real BNB Chain implementation: Load NFT data using smart contract
                // Use token address as hash for now (in production, use proper token ID hash)
                var tokenIdHash = nftTokenAddress.Replace("0x", "").PadLeft(64, '0').Substring(0, 64);
                var loadRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "eth_call",
                    @params = new object[]
                    {
                        new
                        {
                            to = _contractAddress,
                            data = "0x" + GetFunctionSelector("getNFTData") + EncodeParameter(tokenIdHash)
                        },
                        "latest"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultData) && resultData.GetString() != "0x")
                    {
                        // Parse NFT data from blockchain response
                        var nftData = JsonSerializer.Deserialize<JsonElement>(resultData.GetString());
                        var web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = nftTokenAddress,
                            Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "BNB NFT",
                            Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                            Symbol = nftData.TryGetProperty("symbol", out var symbol) ? symbol.GetString() : "BNB"
                        };

                        result.Result = web3NFT;
                        result.IsError = false;
                        result.Message = $"NFT data loaded from BNB Chain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "NFT not found on BNB Chain");
                    }
                    }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load NFT data from BNB Chain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from BNB Chain: {ex.Message}");
            }
            return result;
        }

        // NFT-specific lock/unlock methods
        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                // Lock NFT by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000"; // Would be configured
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = string.Empty, // Would be retrieved from request in real implementation
                    ToWalletAddress = bridgePoolAddress,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
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
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // BurnNFTAsync requires BNB Chain API integration
                OASISErrorHandling.HandleError(ref result, "BurnNFTAsync requires BNB Chain API integration for NFT burning");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on BNB Chain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                // Unlock NFT by transferring from bridge pool back to owner
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAddress,
                    ToWalletAddress = string.Empty, // Would be retrieved from request in real implementation
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
            }
            return result;
        }

        // NFT Bridge Methods
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BNB Chain provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                    return result;
                }

                // Use LockNFTAsync internally for withdrawal
                var lockRequest = new LockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
                    LockedByAvatarId = Guid.Empty
                };

                var lockResult = await LockNFTAsync(lockRequest);
                if (lockResult.IsError || lockResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = lockResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !lockResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
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

    }
}
