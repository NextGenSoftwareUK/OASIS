using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS
{
    public partial class ChainLinkOASIS
    {
        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // ChainLink runs on Ethereum, so use ERC-721 standard for NFT transfers
                // Create ChainLink NFT transfer transaction using ERC-721 transferFrom
                var nftTransferRequest = new
                {
                    from = request.FromWalletAddress,
                    to = request.ToWalletAddress,
                    tokenId = request.TokenId,
                    gas = "0x7530", // 30000 gas for NFT transfer
                    gasPrice = "0x3b9aca00", // 1 gwei
                    data = $"0x23b872dd{request.FromWalletAddress.Substring(2).PadLeft(64, '0')}{request.ToWalletAddress.Substring(2).PadLeft(64, '0')}{(System.Numerics.BigInteger.TryParse(request.TokenId, out var tid) ? tid.ToString("x") : request.TokenId ?? "0").PadLeft(64, '0')}" // ERC-721 transferFrom function
                };

                var jsonContent = JsonSerializer.Serialize(nftTransferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/v1/sendRawTransaction", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("result").GetString()
                    };
                    result.IsError = false;
                    result.Message = $"ChainLink NFT transfer sent successfully. TX Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send ChainLink NFT transfer: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending ChainLink NFT transfer: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request) => new OASISResult<IWeb3NFTTransactionResponse> { IsError = true, Message = "MintNFT not supported by ChainLink provider" };
        public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request) => Task.FromResult(MintNFT(request));
        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request) => new OASISResult<IWeb3NFTTransactionResponse> { IsError = true, Message = "BurnNFT not supported by ChainLink provider" };
        public Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request) => Task.FromResult(BurnNFT(request));
        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress) => new OASISResult<IWeb3NFT> { IsError = true, Message = "LoadOnChainNFTData not supported by ChainLink provider" };
        public Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress) => Task.FromResult(LoadOnChainNFTData(nftTokenAddress));
        public Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null) => Task.FromResult(new OASISResult<BridgeTransactionResponse> { IsError = true, Message = "DepositNFTAsync not supported by ChainLink provider" });



        /// <summary>
        /// Load avatar data from ChainLink oracle
        /// </summary>
        private async Task<string> LoadAvatarFromChainLinkAsync(string avatarId, int version = 0)
        {
            try
            {
                // Query ChainLink oracle for avatar data
                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        avatarId = avatarId,
                        version = version,
                        dataType = "avatar"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading avatar from ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save avatar data to ChainLink oracle
        /// </summary>
        private async Task<string> SaveAvatarToChainLinkAsync(IAvatar avatar)
        {
            try
            {
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        avatarId = avatar.Id.ToString(),
                        dataType = "avatar",
                        data = avatarJson,
                        action = "save"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.requestId;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving avatar to ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load holon data from ChainLink oracle
        /// </summary>
        private async Task<string> LoadHolonFromChainLinkAsync(string holonId, int version = 0)
        {
            try
            {
                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        holonId = holonId,
                        version = version,
                        dataType = "holon"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading holon from ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save holon data to ChainLink oracle
        /// </summary>
        private async Task<string> SaveHolonToChainLinkAsync(IHolon holon)
        {
            try
            {
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        holonId = holon.Id.ToString(),
                        dataType = "holon",
                        data = holonJson,
                        action = "save"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.requestId;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving holon to ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get OASIS ChainLink job ID
        /// </summary>
        private string GetOASISJobId()
        {
            // This would be the ChainLink job ID for OASIS data storage
            return "0x1234567890abcdef1234567890abcdef12345678";
        }

        /// <summary>
        /// Get ChainLink oracle address
        /// </summary>
        private string GetChainLinkOracleAddress()
        {
            // This would be the ChainLink oracle contract address
            return "0x1234567890abcdef1234567890abcdef12345678";
        }

        private async Task<IAvatarDetail> LoadAvatarDetailFromChainLinkAsync(string key)
        {
            await Task.CompletedTask;
            return null;
        }

        private async Task<IEnumerable<IAvatar>> LoadAllAvatarsFromChainLinkAsync()
        {
            await Task.CompletedTask;
            return Array.Empty<IAvatar>();
        }

        private async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsFromChainLinkAsync()
        {
            await Task.CompletedTask;
            return Array.Empty<IAvatarDetail>();
        }

        private async Task<bool> SaveAvatarDetailToChainLinkAsync(IAvatarDetail avatarDetail)
        {
            await Task.CompletedTask;
            return false;
        }

        private async Task<bool> DeleteAvatarFromChainLinkAsync(string key)
        {
            await Task.CompletedTask;
            return false;
        }

        private async Task<IEnumerable<IHolon>> LoadHolonsForParentFromChainLinkAsync(string parentKey, string type)
        {
            await Task.CompletedTask;
            return Array.Empty<IHolon>();
        }



    public class ChainLinkOracleResult
    {
        public ChainLinkOracleData data { get; set; }
    }

    public class ChainLinkOracleData
    {
        public string result { get; set; }
        public string requestId { get; set; }
        public string jobId { get; set; }
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
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
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

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
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
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

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
