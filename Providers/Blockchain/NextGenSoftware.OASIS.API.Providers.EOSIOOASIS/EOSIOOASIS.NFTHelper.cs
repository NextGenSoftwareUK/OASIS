using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in LoadHolonsByMetaDataAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(metaKey) || string.IsNullOrEmpty(metaValue))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MetaKey and MetaValue cannot be null or empty");
                    return result;
                }

                // Search for holons by metadata using EOSIO repository
                var holons = await _holonRepository.ReadAllByMetaData(metaKey, metaValue, type);

                if (holons != null && holons.Any())
                {
                    var holonList = new List<IHolon>();
                    foreach (var holonDto in holons)
                    {
                        var holon = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                        if (holon != null)
                        {
                            holonList.Add(holon);
                        }
                    }

                    result.Result = holonList;
                    result.IsError = false;
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in LoadHolonsByMetaDataAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (metaKeyValuePairs == null || !metaKeyValuePairs.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MetaKeyValuePairs cannot be null or empty");
                    return result;
                }

                // Search for holons by multiple metadata key-value pairs using EOSIO repository
                var holons = await _holonRepository.ReadAllByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, type);

                if (holons != null && holons.Any())
                {
                    var holonList = new List<IHolon>();
                    foreach (var holonDto in holons)
                    {
                        var holon = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                        if (holon != null)
                        {
                            holonList.Add(holon);
                        }
                    }

                    result.Result = holonList;
                    result.IsError = false;
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Query EOSIO NFT data from chain using HTTP API
                // EOSIO NFTs are typically stored in a table on the contract
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var apiUrl = $"{HostURI}/v1/chain/get_table_rows";
                    var requestData = new
                    {
                        json = true,
                        code = nftTokenAddress,
                        scope = nftTokenAddress,
                        table = "nfts",
                        limit = 10,
                        lower_bound = ""
                    };

                    var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var nftData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);

                        if (nftData?.rows != null && nftData.rows.Count > 0)
                        {
                            // Parse first NFT from the table
                            var nftRow = nftData.rows[0];
                            var nft = new Web3NFT
                            {
                                NFTTokenAddress = nftTokenAddress,
                                Title = nftRow.name?.ToString() ?? "",
                                Description = nftRow.description?.ToString() ?? ""
                            };

                            result.Result = nft;
                            result.IsError = false;
                            result.Message = "NFT data loaded successfully from EOSIO chain";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "NFT not found on EOSIO chain");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to query EOSIO chain: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from EOSIO chain: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                var bridgePoolAccount = _contractAddress ?? "oasisbridge";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = string.Empty,
                    ToWalletAddress = bridgePoolAccount,
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                var bridgePoolAccount = _contractAddress ?? "oasisbridge";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAccount,
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
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
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
                    Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{Core.Enums.ProviderType.EOSIOOASIS}:nft:{nftTokenAddress}"),
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

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                    return result;
                }

                var mintRequest = new MintWeb3NFTRequest
                {
                    SendToAddressAfterMinting = receiverAccountAddress,
                };

                var mintResult = await MintNFTAsync(mintRequest);
                if (mintResult.IsError || mintResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = mintResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !mintResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
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


        /// <summary>
        /// Parse EOSIO blockchain response to Avatar object with complete serialization
        /// </summary>
        private Avatar ParseEOSIOToAvatar(GetAccountResponseDto eosioData, string username)
        {
            try
            {
                // Serialize the complete EOSIO data to JSON first
                var eosioJson = JsonConvert.SerializeObject(eosioData, Formatting.Indented);

                // Deserialize the complete Avatar object from EOSIO JSON
                var avatar = JsonConvert.DeserializeObject<Avatar>(eosioJson);

                // If deserialization fails, create from extracted properties
                if (avatar == null)
                {
                    avatar = new Avatar
                    {
                        Id = CreateDeterministicGuid($"{Core.Enums.ProviderType.EOSIOOASIS}:{username}"),
                        Username = username,
                        Email = $"user@{username}.eosio",
                        FirstName = "EOSIO",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = 1,
                        IsActive = true
                    };
                }

                // Add EOSIO-specific metadata
                if (eosioData != null)
                {
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_account_name", username);
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_net_weight", eosioData.NetWeight?.ToString() ?? "0");
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_cpu_weight", eosioData.CpuWeight?.ToString() ?? "0");
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_ram_quota", eosioData.RamQuota?.ToString() ?? "0");
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_ram_usage", eosioData.RamUsage?.ToString() ?? "0");
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetEOSIOAccountNameForAvatarUsername(string avatarUsername)
        {
            try
            {
                // Get avatar detail by username to get the avatar ID
                var avatarDetailResult = AvatarManager.LoadAvatarDetailByUsername(avatarUsername);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    LoggingManager.Log($"No avatar detail found for username {avatarUsername}", LogType.Warning);
                    return null;
                }

                // Get EOSIO account names for the avatar
                var accountNames = GetEOSIOAccountNamesForAvatar(avatarDetailResult.Result.Id);
                if (accountNames == null || !accountNames.Any())
                {
                    LoggingManager.Log($"No EOSIO account names found for avatar {avatarDetailResult.Result.Id}", LogType.Warning);
                    return null;
                }

                return accountNames[0]; // Return the first account name
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO account name for avatar username {avatarUsername}: {ex.Message}", LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// Parse EOSIO blockchain data to OASIS Holon
        /// </summary>
        private static IHolon ParseEOSIOToHolon(object holonData)
        {
            try
            {
                if (holonData == null) return null;

                // Parse the actual EOSIO blockchain data
                var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(holonData.ToString());
                if (dataDict == null) return null;

                var holon = new Holon
                {
                    Id = dataDict.ContainsKey("id") && dataDict["id"] != null ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{Core.Enums.ProviderType.EOSIOOASIS}:holon:{JsonConvert.SerializeObject(dataDict)}"),
                    Name = dataDict.GetValueOrDefault("name")?.ToString() ?? "EOSIO Holon",
                    Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "Holon from EOSIO blockchain",
                    ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                    {
                        [Core.Enums.ProviderType.EOSIOOASIS] = dataDict.GetValueOrDefault("eosioId")?.ToString() ?? CreateDeterministicGuid($"{Core.Enums.ProviderType.EOSIOOASIS}:holon:{JsonConvert.SerializeObject(dataDict)}").ToString()
                    },
                    IsActive = dataDict.GetValueOrDefault("isActive")?.ToString()?.ToLower() == "true",
                    CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                    ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                    Version = dataDict.ContainsKey("version") ? int.Parse(dataDict["version"].ToString()) : 1,
                    MetaData = new Dictionary<string, object>
                    {
                        ["EOSIOData"] = holonData,
                        ["EOSIOAccountName"] = dataDict.GetValueOrDefault("accountName")?.ToString(),
                        ["EOSIOBlockNum"] = dataDict.GetValueOrDefault("blockNum")?.ToString(),
                        ["EOSIOTimestamp"] = dataDict.GetValueOrDefault("timestamp")?.ToString(),
                        ["ParsedAt"] = DateTime.UtcNow,
                        ["Provider"] = "EOSIOOASIS"
                    }
                };

                return holon;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Error parsing EOSIO holon data: {ex.Message}", LogType.Error);
                return null;
            }
        }

    }
}
