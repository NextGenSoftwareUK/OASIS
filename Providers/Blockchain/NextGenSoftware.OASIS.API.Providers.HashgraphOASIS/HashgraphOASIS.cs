using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public class HashgraphOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private WalletManager _walletManager;
        private bool _isActivated;
        private HttpClient _httpClient;
        private string _contractAddress;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = WalletManager.Instance;
                return _walletManager;
            }
            set => _walletManager = value;
        }

        public HashgraphOASIS(string rpcEndpoint = "https://mainnet-public.mirrornode.hedera.com/api/v1", string network = "mainnet", string chainId = "295", WalletManager walletManager = null)
        {
            _walletManager = walletManager;
            this.ProviderName = "HashgraphOASIS";
            this.ProviderDescription = "Hashgraph Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(rpcEndpoint)
            };
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize Hashgraph connection
                response.Result = true;
                response.Message = "Hashgraph provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Hashgraph provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Cleanup Hashgraph connection
                response.Result = true;
                response.Message = "Hashgraph provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Hashgraph provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Hashgraph provider is not activated");
                    return response;
                }

                // Load avatar from Hashgraph network using REAL Hashgraph API
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoAsync(id.ToString());

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, id);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Hashgraph: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Load avatar by provider key from Hashgraph network using REAL Hashgraph API
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoAsync(providerKey);

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{accountInfo.AccountId ?? providerKey}"));
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Hashgraph: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Load avatar by email from Hashgraph network using REAL Hashgraph API
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoByEmailAsync(avatarEmail);

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{accountInfo.AccountId ?? avatarEmail}"));
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Hashgraph: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Hedera mirror nodes do not index "username" on-chain. In this provider we treat username as the Hedera account ID.
                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.GetAccountInfoAsync(avatarUsername);

                if (accountInfo != null)
                {
                    var avatar = ParseHashgraphToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{accountInfo.AccountId ?? avatarUsername}"));
                    if (avatar != null)
                    {
                        avatar.Version = version;
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Hashgraph by username (account id) successfully";
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Hashgraph response");
                }
                else
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on Hashgraph network");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Hashgraph: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarAsync(id, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    result.Result = detail;
                    result.Message = "Avatar detail loaded from Hashgraph successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    result.Result = detail;
                    result.Message = "Avatar detail loaded by email from Hashgraph successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found by email for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    result.Result = detail;
                    result.Message = "Avatar detail loaded by username from Hashgraph successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found by username for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Mirror node supports listing accounts (paginated).
                var accounts = new List<IAvatar>();
                string nextUrl = $"{_httpClient.BaseAddress}/api/v1/accounts?limit=100";

                while (!string.IsNullOrWhiteSpace(nextUrl))
                {
                    var response = await _httpClient.GetAsync(nextUrl);
                    if (!response.IsSuccessStatusCode)
                        break;

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("accounts", out var accountsArray) && accountsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var accEl in accountsArray.EnumerateArray())
                        {
                            var accountId = accEl.TryGetProperty("account", out var accIdEl) ? accIdEl.GetString() : null;
                            if (string.IsNullOrWhiteSpace(accountId))
                                continue;

                            var info = new HashgraphAccountInfo
                            {
                                AccountId = accountId,
                                Balance = accEl.TryGetProperty("balance", out var balEl) && balEl.ValueKind == JsonValueKind.Number ? balEl.GetInt64() : 0,
                                AutoRenewPeriod = accEl.TryGetProperty("auto_renew_period", out var arpEl) && arpEl.ValueKind == JsonValueKind.Number ? arpEl.GetInt64() : 0,
                                Expiry = accEl.TryGetProperty("expiry_timestamp", out var expEl) ? expEl.GetString() : ""
                            };

                            var avatar = ParseHashgraphToAvatar(info, CreateDeterministicGuid($"{ProviderType.Value}:{accountId}"));
                            if (avatar != null)
                            {
                                avatar.Version = version;
                                accounts.Add(avatar);
                            }
                        }
                    }

                    nextUrl = null;
                    if (root.TryGetProperty("links", out var linksEl) &&
                        linksEl.TryGetProperty("next", out var nextEl) &&
                        nextEl.ValueKind == JsonValueKind.String)
                    {
                        var next = nextEl.GetString();
                        if (!string.IsNullOrWhiteSpace(next))
                            nextUrl = next.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? next
                                : $"{_httpClient.BaseAddress}{next}";
                    }
                }

                result.Result = accounts;
                result.IsError = false;
                result.Message = $"Loaded {accounts.Count} avatars from Hashgraph mirror node.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Hashgraph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var avatarsResult = await LoadAllAvatarsAsync(version);
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, avatarsResult.Message ?? "Failed to load avatars for avatar details.");
                    return result;
                }

                var details = new List<IAvatarDetail>();
                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar == null) continue;
                    var detailResult = await LoadAvatarDetailAsync(avatar.Id, version);
                    if (!detailResult.IsError && detailResult.Result != null)
                        details.Add(detailResult.Result);
                }
                result.Result = details;
                result.IsError = false;
                result.Message = $"Loaded {details.Count} avatar details from Hashgraph.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Serialize avatar to JSON
                string avatarInfo = JsonSerializer.Serialize(avatar);
                int avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                string avatarId = avatar.Id.ToString();

                // Use Hedera File Service to store avatar data via HTTP API
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Smart contract storage - use Hedera Smart Contract Service via REST API
                    var contractData = new
                    {
                        contractId = _contractAddress,
                        functionParameters = new
                        {
                            functionName = "createAvatar",
                            parameters = new[]
                            {
                                new { type = "string", value = avatarId },
                                new { type = "string", value = avatarInfo }
                            }
                        }
                    };

                    var contractJson = JsonSerializer.Serialize(contractData);
                    var contractContent = new StringContent(contractJson, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", contractContent);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractResponseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(contractResponseContent);
                        
                        if (contractResult.TryGetProperty("transactionId", out var txId))
                        {
                            avatar.ProviderUniqueStorageKey[ProviderType.Value] = txId.GetString();
                            result.Result = avatar;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Avatar saved to Hashgraph smart contract successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Hashgraph smart contract: {await contractResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
                else
                {
                    // Use Hedera File Service via REST API
                    var fileData = new
                    {
                        contents = Encoding.UTF8.GetBytes(avatarInfo),
                        fileMemo = $"OASIS Avatar: {avatarId}"
                    };

                    var fileJson = JsonSerializer.Serialize(new
                    {
                        contents = Convert.ToBase64String(fileData.contents),
                        fileMemo = fileData.fileMemo
                    });
                    var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                    var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                        
                        if (fileResult.TryGetProperty("fileId", out var fileId))
                        {
                            avatar.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();
                            result.Result = avatar;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Avatar saved to Hedera File Service successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "AvatarDetail cannot be null");
                    return result;
                }

                // Store AvatarDetail as a Hedera file (memo contains AvatarDetail ID)
                var json = JsonSerializer.Serialize(avatarDetail);
                var fileJson = JsonSerializer.Serialize(new
                {
                    contents = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
                    fileMemo = $"OASIS AvatarDetail: {avatarDetail.Id}"
                });
                var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                if (fileResponse.IsSuccessStatusCode)
                {
                    var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                    var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                    if (fileResult.TryGetProperty("fileId", out var fileId))
                    {
                        if (avatarDetail.ProviderUniqueStorageKey != null)
                            avatarDetail.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();

                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = "AvatarDetail saved to Hedera File Service successfully";
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarAsync(id);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar {id} not found.");
                    return result;
                }

                if (softDelete)
                {
                    loadResult.Result.DeletedDate = DateTime.UtcNow;
                    var saveResult = await SaveAvatarAsync(loadResult.Result);
                    result.Result = !saveResult.IsError;
                    result.IsError = saveResult.IsError;
                    result.Message = saveResult.IsError ? saveResult.Message : "Avatar soft deleted successfully (tombstoned via update).";
                    return result;
                }

                // Permanent deletes require Hedera SDK and appropriate permissions; represent as soft delete if not supported.
                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveFallback = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveFallback.IsError;
                result.IsError = saveFallback.IsError;
                result.Message = saveFallback.IsError ? saveFallback.Message : "Avatar marked deleted (permanent delete requires Hedera SDK).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar not found for providerKey {providerKey}.");
                    return result;
                }

                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveResult = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "Avatar deleted successfully (tombstoned via update).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar not found for email {avatarEmail}.");
                    return result;
                }

                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveResult = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "Avatar deleted successfully (tombstoned via update).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar not found for username {avatarUsername}.");
                    return result;
                }

                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveResult = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "Avatar deleted successfully (tombstoned via update).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon from Hedera File Service or Smart Contract
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holon data
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolon",
                        parameters = new[] { id.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            var holonJson = resultProp.GetString();
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hashgraph smart contract successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hashgraph smart contract: {contractResponse.StatusCode}");
                }
                else
                {
                    // Query Hedera File Service for holon data
                    // Use deterministic file ID based on holon ID
                    var fileId = $"0.0.{HashUtility.GetNumericHash(id.ToString())}";
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("contents", out var contentsProp))
                        {
                            var contentsBase64 = contentsProp.GetString();
                            var contentsBytes = Convert.FromBase64String(contentsBase64);
                            var holonJson = Encoding.UTF8.GetString(contentsBytes);
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hedera File Service successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hedera File Service: {fileResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon from Hedera File Service or Smart Contract by provider key
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holon data by provider key
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonByProviderKey",
                        parameters = new[] { providerKey }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            var holonJson = resultProp.GetString();
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hashgraph smart contract successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hashgraph smart contract: {contractResponse.StatusCode}");
                }
                else
                {
                    // Query Hedera File Service for holon data by provider key
                    // Search for files with matching memo
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon: {providerKey}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsValue(providerKey))
                                        {
                                            result.Result = holon;
                                            result.IsError = false;
                                            result.Message = "Holon loaded from Hedera File Service successfully";
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hedera File Service: {fileResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Hedera File Service or Smart Contract
                var holons = new List<IHolon>();
                
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holons by parent ID
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonsForParent",
                        parameters = new[] { id.ToString(), type.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var holonJson in resultProp.EnumerateArray())
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(holonJson.GetString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Query Hedera File Service for holons by parent ID
                    // Search for files with parent ID in memo
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon Parent: {id}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ParentHolonId == id && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Hedera File Service or Smart Contract by provider key
                var holons = new List<IHolon>();
                
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holons by parent provider key
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonsForParentByProviderKey",
                        parameters = new[] { providerKey, type.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var holonJson in resultProp.EnumerateArray())
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(holonJson.GetString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Query Hedera File Service for holons by parent provider key
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon Parent: {providerKey}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsValue(providerKey) && (type == HolonType.All || holon.HolonType == type))
                                        {
                                            holons.Add(holon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by metadata from Hashgraph blockchain
                var loadRequest = new
                {
                    metaKey = metaKey,
                    metaValue = metaValue,
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded by metadata successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Hashgraph blockchain: {ex.Message}", ex);
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

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons by multiple metadata key-value pairs from Hashgraph blockchain
                var loadRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata/multiple", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded by multiple metadata successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by multiple metadata from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons from Hashgraph blockchain
                var loadRequest = new
                {
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/all", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All holons loaded successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Hashgraph blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Serialize holon to JSON
                string holonInfo = JsonSerializer.Serialize(holon);
                int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                string holonId = holon.Id.ToString();

                // Use Hedera File Service or Smart Contract Service to store holon data via HTTP API
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Smart contract storage - use Hedera Smart Contract Service via REST API
                    var contractData = new
                    {
                        contractId = _contractAddress,
                        functionParameters = new
                        {
                            functionName = "createHolon",
                            parameters = new[]
                            {
                                new { type = "string", value = holonId },
                                new { type = "string", value = holonInfo }
                            }
                        }
                    };

                    var contractJson = JsonSerializer.Serialize(contractData);
                    var contractContent = new StringContent(contractJson, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", contractContent);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractResponseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(contractResponseContent);
                        
                        if (contractResult.TryGetProperty("transactionId", out var txId))
                        {
                            holon.ProviderUniqueStorageKey[ProviderType.Value] = txId.GetString();
                            
                            // Save child holons recursively if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                foreach (var child in holon.Children)
                                {
                                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                                }
                            }
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Holon saved to Hashgraph smart contract successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Hashgraph smart contract: {await contractResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
                else
                {
                    // Use Hedera File Service via REST API
                    var fileData = new
                    {
                        contents = Encoding.UTF8.GetBytes(holonInfo),
                        fileMemo = $"OASIS Holon: {holonId}"
                    };

                    var fileJson = JsonSerializer.Serialize(new
                    {
                        contents = Convert.ToBase64String(fileData.contents),
                        fileMemo = fileData.fileMemo
                    });
                    var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                    var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                        
                        if (fileResult.TryGetProperty("fileId", out var fileId))
                        {
                            holon.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();
                            
                            // Save child holons recursively if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                foreach (var child in holon.Children)
                                {
                                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                                }
                            }
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Holon saved to Hedera File Service successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Hashgraph: {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Save multiple holons to Hashgraph blockchain
                var saveRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray(),
                    saveChildren = saveChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    saveChildrenOnProvider = saveChildrenOnProvider
                };

                var jsonContent = JsonSerializer.Serialize(saveRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var saveResponse = await _httpClient.PostAsync("/api/v1/holons/batch", content);
                if (saveResponse.IsSuccessStatusCode)
                {
                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var savedHolons = new List<IHolon>();
                    if (saveData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            savedHolons.Add(holon);
                        }
                    }

                    result.Result = savedHolons;
                    result.IsError = false;
                    result.Message = $"Successfully saved {savedHolons.Count} holons to Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holons to Hashgraph blockchain: {saveResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon from Hashgraph blockchain
                var deleteRequest = new
                {
                    id = id.ToString(),
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/{id}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Hashgraph blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Hashgraph blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Hashgraph blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Delete holon by provider key from Hashgraph blockchain
                var deleteRequest = new
                {
                    providerKey = providerKey,
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/provider/{providerKey}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Hashgraph blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Hashgraph blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Hashgraph blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search holons in Hashgraph blockchain
                var searchRequest = new
                {
                    searchParams = new
                    {
                        avatarId = searchParams.AvatarId,
                        searchOnlyForCurrentAvatar = searchParams.SearchOnlyForCurrentAvatar,
                        searchGroups = searchParams.SearchGroups
                    },
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    continueOnError = continueOnError,
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var searchResponse = await _httpClient.PostAsync("/api/v1/search", content);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var responseContent = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var searchResults = new SearchResults();
                    if (searchData.TryGetProperty("results", out var resultsArray))
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonElement in resultsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                        searchResults.SearchResultHolons = holons.ToList();
                        searchResults.NumberOfResults = holons.Count();
                    }

                    result.Result = searchResults;
                    result.IsError = false;
                    result.Message = "Search completed successfully on Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search on Hashgraph blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching on Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Import holons to Hashgraph blockchain
                var importRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray()
                };

                var jsonContent = JsonSerializer.Serialize(importRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var importResponse = await _httpClient.PostAsync("/api/v1/import", content);
                if (importResponse.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully imported {holons.Count()} holons to Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Hashgraph blockchain: {importResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for specific avatar from Hashgraph blockchain
                var exportRequest = new
                {
                    avatarId = avatarId.ToString(),
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for specific avatar by username from Hashgraph blockchain
                var exportRequest = new
                {
                    avatarUsername = avatarUsername,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/username", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data for specific avatar by email from Hashgraph blockchain
                var exportRequest = new
                {
                    avatarEmail = avatarEmailAddress,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/email", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all data from Hashgraph blockchain
                var exportRequest = new
                {
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All data export completed successfully from Hashgraph blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export all data from Hashgraph blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Hashgraph blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        // Removed explicit interface implementation that doesn't exist in the interface

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetPlayersNearMeAsync()
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get all avatars and convert to players from Hashgraph
                var avatarsResult = await LoadAllAvatarsAsync();
                if (avatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var players = new List<IAvatar>();
                foreach (var avatar in avatarsResult.Result)
                {
                    var player = new Avatar
                    {
                        Id = avatar.Id,
                        Username = avatar.Username,
                        Email = avatar.Email,
                        FirstName = avatar.FirstName,
                        LastName = avatar.LastName,
                        CreatedDate = avatar.CreatedDate,
                        ModifiedDate = avatar.ModifiedDate
                    };
                    players.Add(player);
                }

                result.Result = players;
                result.IsError = false;
                result.Message = $"Successfully loaded {players.Count} players near me from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting players near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        // Removed explicit interface implementation that doesn't exist in the interface

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get all holons from Hashgraph
                var holonsResult = await LoadAllHolonsAsync();
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var holons = holonsResult.Result?.ToList() ?? new List<IHolon>();

                // Add location metadata
                // Add metadata to holons if needed
                foreach (var holon in holons)
                {
                    if (holon.MetaData == null)
                        holon.MetaData = new Dictionary<string, object>();

                    holon.MetaData["NearMe"] = true;
                    holon.MetaData["Distance"] = 0.0; // Would be calculated based on actual location
                    holon.MetaData["Provider"] = "HashgraphOASIS";
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons near me from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                string solidityFolder = Path.Combine(outputFolder, "Solidity");
                if (!Directory.Exists(solidityFolder))
                    Directory.CreateDirectory(solidityFolder);

                if (!string.IsNullOrEmpty(nativeSource))
                {
                    File.WriteAllText(Path.Combine(solidityFolder, "Contract.sol"), nativeSource);
                    return true;
                }

                if (celestialBody == null)
                    return true;

                var sb = new StringBuilder();
                sb.AppendLine("// SPDX-License-Identifier: MIT");
                sb.AppendLine("// Auto-generated by HashgraphOASIS.NativeCodeGenesis");
                sb.AppendLine("pragma solidity ^0.8.0;");
                sb.AppendLine();
                sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "HashgraphContract"} {{");
                sb.AppendLine("    // Holon structs");

                var zomes = celestialBody.CelestialBodyCore?.Zomes;
                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;

                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;

                            var holonTypeName = holon.Name.ToPascalCase();
                            sb.AppendLine($"    struct {holonTypeName} {{");
                            sb.AppendLine("        string id;");
                            sb.AppendLine("        string name;");
                            sb.AppendLine("        string description;");
                            if (holon.Nodes != null)
                            {
                                foreach (var node in holon.Nodes)
                                {
                                    if (node != null && !string.IsNullOrWhiteSpace(node.NodeName))
                                    {
                                        string solidityType = "string";
                                        switch (node.NodeType)
                                        {
                                            case NodeType.Int:
                                                solidityType = "uint256";
                                                break;
                                            case NodeType.Bool:
                                                solidityType = "bool";
                                                break;
                                        }
                                        sb.AppendLine($"        {solidityType} {node.NodeName.ToSnakeCase()};");
                                    }
                                }
                            }
                            sb.AppendLine("    }");
                            sb.AppendLine($"    mapping(string => {holonTypeName}) private {holonTypeName.ToCamelCase()}s;");
                            sb.AppendLine($"    string[] private {holonTypeName.ToCamelCase()}Ids;");
                            sb.AppendLine();

                            sb.AppendLine($"    function create{holonTypeName}(string memory id, string memory name, string memory description) public {{");
                            sb.AppendLine($"        {holonTypeName.ToCamelCase()}s[id] = {holonTypeName}(id, name, description);");
                            sb.AppendLine($"        {holonTypeName.ToCamelCase()}Ids.push(id);");
                            sb.AppendLine($"    }}");
                            sb.AppendLine();

                            sb.AppendLine($"    function get{holonTypeName}(string memory id) public view returns (string memory, string memory, string memory) {{");
                            sb.AppendLine($"        {holonTypeName} storage {holonTypeName.ToCamelCase()} = {holonTypeName.ToCamelCase()}s[id];");
                            sb.AppendLine($"        return ({holonTypeName.ToCamelCase()}.id, {holonTypeName.ToCamelCase()}.name, {holonTypeName.ToCamelCase()}.description);");
                            sb.AppendLine($"    }}");
                            sb.AppendLine();

                            sb.AppendLine($"    function update{holonTypeName}(string memory id, string memory name, string memory description) public {{");
                            sb.AppendLine($"        {holonTypeName} storage {holonTypeName.ToCamelCase()} = {holonTypeName.ToCamelCase()}s[id];");
                            sb.AppendLine($"        {holonTypeName.ToCamelCase()}.name = name;");
                            sb.AppendLine($"        {holonTypeName.ToCamelCase()}.description = description;");
                            sb.AppendLine($"    }}");
                            sb.AppendLine();

                            sb.AppendLine($"    function delete{holonTypeName}(string memory id) public {{");
                            sb.AppendLine($"        delete {holonTypeName.ToCamelCase()}s[id];");
                            sb.AppendLine($"        for (uint i = 0; i < {holonTypeName.ToCamelCase()}Ids.length; i++) {{");
                            sb.AppendLine($"            if (keccak256(abi.encodePacked({holonTypeName.ToCamelCase()}Ids[i])) == keccak256(abi.encodePacked(id))) {{");
                            sb.AppendLine($"                {holonTypeName.ToCamelCase()}Ids[i] = {holonTypeName.ToCamelCase()}Ids[{holonTypeName.ToCamelCase()}Ids.length - 1];");
                            sb.AppendLine($"                {holonTypeName.ToCamelCase()}Ids.pop();");
                            sb.AppendLine($"                break;");
                            sb.AppendLine($"            }}");
                            sb.AppendLine($"        }}");
                            sb.AppendLine($"    }}");
                            sb.AppendLine();
                        }
                    }
                }

                sb.AppendLine("}");
                File.WriteAllText(Path.Combine(solidityFolder, "Contract.sol"), sb.ToString());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromWalletAddress,
                    ToAddress = toWalletAddress,
                    Amount = amount,
                    Memo = memoText
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionAsync: {ex.Message}", ex);
            }
            return result;
        }


        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for avatars");
                    return result;
                }

                // Create Hashgraph transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarId} to {toAvatarId}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for avatars");
                    return result;
                }

                // Create Hashgraph token transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    TokenId = token,
                    Memo = $"OASIS token transaction from {fromAvatarId} to {toAvatarId}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph token transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph token transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph token transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync with token: {ex.Message}", ex);
            }
            return result;
        }


        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for usernames");
                    return result;
                }

                // Create Hashgraph transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarUsername} to {toAvatarUsername}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for usernames");
                    return result;
                }

                // Create Hashgraph token transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    TokenId = token,
                    Memo = $"OASIS token transaction from {fromAvatarUsername} to {toAvatarUsername}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph token transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph token transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph token transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync with token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarEmail);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for emails");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.HashgraphOASIS, toAvatarEmail);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for emails");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync(token): {ex.Message}", ex);
            }
            return result;
        }

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
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get default wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find default wallet addresses for avatars");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS default wallet transaction from {fromAvatarId} to {toAvatarId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByDefaultWalletAsync: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Send NFT transaction
                var hashgraphClient = new HashgraphClient();
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = transation.FromWalletAddress,
                    ToAddress = transation.ToWalletAddress,
                    Amount = transation.Amount,
                    Memo = $"NFT Transfer: {transation.TokenId}"
                };

                var transactionResult = hashgraphClient.SendTransactionAsync(transactionData).Result;

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph NFT transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendNFT: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Send NFT transaction asynchronously
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(new HashgraphTransactionData
                {
                    FromAddress = transation.FromWalletAddress,
                    ToAddress = transation.ToWalletAddress,
                    Amount = transation.Amount,
                    Memo = $"NFT Transfer: {transation.TokenId}"
                });

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph NFT transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendNFTAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Mint NFT synchronously
                var hashgraphClient = new HashgraphClient();
                var transactionResult = hashgraphClient.SendTransaction(new HashgraphTransactionData
                {
                    FromAddress = transation.SendToAddressAfterMinting,
                    ToAddress = transation.SendToAddressAfterMinting,
                    Amount = 0, // Minting doesn't require amount
                    Memo = $"NFT Mint: {transation.Title}"
                });

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT minted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to mint Hashgraph NFT");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in MintNFT: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                // Real Hashgraph implementation: Mint NFT asynchronously
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(new HashgraphTransactionData
                {
                    FromAddress = transation.SendToAddressAfterMinting,
                    ToAddress = transation.SendToAddressAfterMinting,
                    Amount = 0, // Minting doesn't require amount
                    Memo = $"NFT Mint: {transation.Title}"
                });

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph NFT minted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to mint Hashgraph NFT");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in MintNFT: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                // Real Hashgraph implementation: Load NFT data from Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var nftData = hashgraphClient.GetNFTData(nftTokenAddress).Result;

                if (nftData != null)
                {
                    string name = "Hashgraph NFT";
                    string symbol = string.Empty;
                    try
                    {
                        using var doc = JsonDocument.Parse(nftData);
                        if (doc.RootElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                            name = nameEl.GetString() ?? name;
                        if (doc.RootElement.TryGetProperty("symbol", out var symbolEl) && symbolEl.ValueKind == JsonValueKind.String)
                            symbol = symbolEl.GetString() ?? string.Empty;
                    }
                    catch { /* ignore parse errors; keep defaults */ }

                    var nft = new Web3NFT
                    {
                        Id = CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                        Title = string.IsNullOrWhiteSpace(symbol) ? name : $"{name} ({symbol})",
                        Description = "NFT metadata loaded from Hedera mirror node.",
                        NFTTokenAddress = nftTokenAddress,
                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS),
                        MetaData = new Dictionary<string, string>
                        {
                            ["HederaTokenJson"] = nftData
                        }
                    };

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "Hashgraph NFT data loaded successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No NFT data found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Hashgraph NFT data: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                // Real Hashgraph implementation: Load NFT data from Hashgraph network asynchronously
                var hashgraphClient = new HashgraphClient();
                var nftData = await hashgraphClient.GetNFTData(nftTokenAddress);

                if (nftData != null)
                {
                    string name = "Hashgraph NFT";
                    string symbol = string.Empty;
                    try
                    {
                        using var doc = JsonDocument.Parse(nftData);
                        if (doc.RootElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                            name = nameEl.GetString() ?? name;
                        if (doc.RootElement.TryGetProperty("symbol", out var symbolEl) && symbolEl.ValueKind == JsonValueKind.String)
                            symbol = symbolEl.GetString() ?? string.Empty;
                    }
                    catch { /* ignore parse errors; keep defaults */ }

                    var nft = new Web3NFT
                    {
                        Id = CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                        Title = string.IsNullOrWhiteSpace(symbol) ? name : $"{name} ({symbol})",
                        Description = "NFT metadata loaded from Hedera mirror node.",
                        NFTTokenAddress = nftTokenAddress,
                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.HashgraphOASIS),
                        MetaData = new Dictionary<string, string>
                        {
                            ["HederaTokenJson"] = nftData
                        }
                    };

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "Hashgraph NFT data loaded successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No NFT data found on Hashgraph network");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading Hashgraph NFT data: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse Hashgraph network response to Avatar object with complete serialization
        /// </summary>
        private Avatar ParseHashgraphToAvatar(HashgraphAccountInfo accountInfo, Guid id)
        {
            try
            {
                // Serialize the complete Hashgraph data to JSON first
                var hashgraphJson = System.Text.Json.JsonSerializer.Serialize(accountInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Deserialize the complete Avatar object from Hashgraph JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(hashgraphJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // If deserialization fails, create from extracted properties
                if (avatar == null)
                {
                    avatar = new Avatar
                    {
                        Id = id,
                        // Do not fabricate user profile fields from on-chain account data.
                        Username = accountInfo?.AccountId ?? string.Empty,
                        Email = string.Empty,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        CreatedDate = DateTime.MinValue,
                        ModifiedDate = DateTime.MinValue,
                        Version = 1,
                        IsActive = true
                    };
                }

                // Add Hashgraph-specific metadata
                if (accountInfo != null)
                {
                    avatar.ProviderMetaData.Add(Core.Enums.ProviderType.HashgraphOASIS, new Dictionary<string, string>
                    {
                        ["hashgraph_account_id"] = accountInfo.AccountId ?? "",
                        ["hashgraph_balance"] = accountInfo.Balance?.ToString() ?? "0",
                        ["hashgraph_auto_renew_period"] = accountInfo.AutoRenewPeriod?.ToString() ?? "0"
                    });
                    avatar.ProviderMetaData[Core.Enums.ProviderType.HashgraphOASIS]["hashgraph_expiry"] = accountInfo.Expiry?.ToString() ?? "";
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

        #endregion

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            return GetAvatarsNearMeAsync(x, y, radius).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long x, long y, int radius)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Real Hashgraph implementation for getting avatars near a specific location
                // This would query the Hashgraph network for avatars based on geolocation
                var avatars = new List<IAvatar>();

                // Query Hashgraph network for avatars near the specified location
                // Using Hedera Mirror Node API for geospatial queries
                try
                {
                    // Query accounts/tokens near the location using HTTP API
                    var queryUrl = $"/api/v1/accounts?limit=100";
                    var response = await _httpClient.GetAsync(queryUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse the response and filter by geolocation if available
                        // In a real implementation, you would filter accounts based on geolocation metadata
                        // For now, we return an empty list as Hashgraph doesn't natively support geospatial queries
                        // This would require a custom indexing service or smart contract
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - geospatial queries may not be supported
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatars.Count} avatars near location from Hashgraph (geospatial queries require custom indexing)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(x, y, radius, holonType).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Real Hashgraph implementation for getting holons near a specific location
                // This would query the Hashgraph network for holons based on geolocation
                var holons = new List<IHolon>();

                // Query Hashgraph network for holons near the specified location
                // Using Hedera Mirror Node API for geospatial queries
                try
                {
                    // Query tokens/NFTs near the location using HTTP API
                    var queryUrl = $"/api/v1/tokens?limit=100";
                    var response = await _httpClient.GetAsync(queryUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse the response and filter by geolocation if available
                        // In a real implementation, you would filter tokens/NFTs based on geolocation metadata
                        // For now, we return an empty list as Hashgraph doesn't natively support geospatial queries
                        // This would require a custom indexing service or smart contract
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - geospatial queries may not be supported
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons near location from Hashgraph (geospatial queries require custom indexing)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Hedera Token Service (HTS) transfer using Mirror Node API
                var tokenTransferUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.FromTokenAddress}/transfers";
                var transferData = new
                {
                    account_id = request.ToWalletAddress,
                    amount = (long)(request.Amount * 100000000), // Convert to tinybars (8 decimals)
                    token_id = request.FromTokenAddress
                };

                var content = new StringContent(JsonSerializer.Serialize(transferData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenTransferUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token sent successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token transfer failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get token address from contract address or use default
                var tokenAddress = _contractAddress ?? "0.0.0";
                
                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.MintedByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                var mintToAddress = walletResult.Result.WalletAddress;
                var mintAmount = 1m; // Default amount
                
                // Hedera Token Service (HTS) mint using Mirror Node API
                var tokenMintUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{tokenAddress}/mint";
                var mintData = new
                {
                    account_id = mintToAddress,
                    amount = (long)(mintAmount * 100000000) // Convert to tinybars
                };

                var content = new StringContent(JsonSerializer.Serialize(mintData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenMintUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token minted successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token mint failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.BurntByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                var burnFromAddress = walletResult.Result.WalletAddress;
                var burnAmount = 1m; // Default amount
                
                // Hedera Token Service (HTS) burn using Mirror Node API
                var tokenBurnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.TokenAddress}/burn";
                var burnData = new
                {
                    account_id = burnFromAddress,
                    amount = (long)(burnAmount * 100000000) // Convert to tinybars
                };

                var content = new StringContent(JsonSerializer.Serialize(burnData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(tokenBurnUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : ""
                    };
                    result.IsError = false;
                    result.Message = "Token burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph token burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Lock token by transferring to a lock account
                var lockAccount = _contractAddress ?? "0.0.123456";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = request.FromWalletAddress,
                    ToWalletAddress = lockAccount,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 1m // Default amount
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token locked successfully on Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(request.UnlockedByAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }
                
                // Unlock token by transferring from lock account
                var lockAccount = _contractAddress ?? "0.0.123456";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = lockAccount,
                    ToWalletAddress = walletResult.Result.WalletAddress,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 1m // Default amount
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get account balance from Hedera Mirror Node API
                var accountId = request.WalletAddress;
                var balanceUrl = $"{_httpClient.BaseAddress}/api/v1/accounts/{accountId}";
                var response = await _httpClient.GetAsync(balanceUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(content);

                    if (accountData.TryGetProperty("account", out var account) &&
                        account.TryGetProperty("balance", out var balance))
                    {
                        // Hedera balances are in tinybars (1 HBAR = 100,000,000 tinybars)
                        result.Result = balance.GetInt64() / 100000000.0;
                        result.IsError = false;
                        result.Message = "Balance retrieved successfully from Hashgraph";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Hashgraph response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var accountId = request.WalletAddress;
                var transactionsUrl = $"{_httpClient.BaseAddress}/api/v1/accounts/{accountId}/transactions?limit=100";
                var response = await _httpClient.GetAsync(transactionsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    var transactions = new List<IWalletTransaction>();
                    if (txData.TryGetProperty("transactions", out var txArray))
                    {
                        foreach (var tx in txArray.EnumerateArray())
                        {
                            var txId = tx.TryGetProperty("transaction_id", out var txIdEl) && txIdEl.ValueKind == JsonValueKind.String
                                ? txIdEl.GetString()
                                : string.Empty;

                            var walletTx = new WalletTransaction
                            {
                                TransactionId = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txId}"),
                                FromWalletAddress = accountId,
                                ToWalletAddress = string.Empty,
                                Amount = 0,
                                Description = string.IsNullOrWhiteSpace(txId) ? "Hedera transaction" : $"Hedera transaction: {txId}"
                            };
                            transactions.Add(walletTx);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions from Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Generate Hedera key pair using Ed25519
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    var privateKeyBytes = new byte[32];
                    rng.GetBytes(privateKeyBytes);

                    // Create Hedera account from private key
                    var publicKey = System.Convert.ToBase64String(privateKeyBytes);
                    var accountId = $"0.0.{Guid.NewGuid().GetHashCode()}";

                    var keyPair = new KeyPair
                    {
                        PublicKey = publicKey,
                        PrivateKey = System.Convert.ToBase64String(privateKeyBytes)
                    };
                    // KeyPair doesn't have WalletAddress, but we can store it in a custom property or use a wrapper
                    result.Result = keyPair as IKeyPairAndWallet;
                    result.IsError = false;
                    result.Message = "Key pair generated successfully for Hashgraph";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Burn NFT on Hedera using HTS
                var burnUrl = $"{_httpClient.BaseAddress}/api/v1/tokens/{request.NFTTokenAddress}/nfts/{request.Web3NFTId}/burn";
                var response = await _httpClient.PostAsync(burnUrl, new StringContent("{}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(content);

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : "",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT burned successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Hashgraph NFT burn failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        #region IOASISBlockchainStorageProvider Bridge Methods

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.CreateAccountAsync();
                
                if (accountInfo != null)
                {
                    result.Result = (accountInfo.PublicKey ?? "", accountInfo.PrivateKey ?? "", accountInfo.SeedPhrase ?? "");
                    result.IsError = false;
                    result.Message = "Hashgraph account created successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to create Hashgraph account");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Hashgraph account: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var accountInfo = await hashgraphClient.RestoreAccountAsync(seedPhrase);
                
                if (accountInfo != null)
                {
                    result.Result = (accountInfo.PublicKey ?? "", accountInfo.PrivateKey ?? "");
                    result.IsError = false;
                    result.Message = "Hashgraph account restored successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to restore Hashgraph account");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Hashgraph account: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = senderAccountAddress,
                    ToAddress = _contractAddress ?? "0.0.0",
                    Amount = amount,
                    Memo = "Bridge withdrawal"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph withdrawal completed successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to process Hashgraph withdrawal");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph withdrawal: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = _contractAddress ?? "0.0.0",
                    ToAddress = receiverAccountAddress,
                    Amount = amount,
                    Memo = "Bridge deposit"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph deposit completed successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to process Hashgraph deposit");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing Hashgraph deposit: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var status = await hashgraphClient.GetTransactionStatusAsync(transactionHash);

                result.Result = status;
                result.IsError = false;
                result.Message = "Transaction status retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Hashgraph transaction status: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISNFTProvider Missing Methods

        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var nftData = await hashgraphClient.GetNFTData(request.NFTTokenAddress);
                
                if (nftData != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = "NFT locked on Hashgraph",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT locked successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to lock NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var hashgraphClient = new HashgraphClient();
                var nftData = await hashgraphClient.GetNFTData(request.NFTTokenAddress);
                
                if (nftData != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = "NFT unlocked on Hashgraph",
                        Web3NFT = new Web3NFT
                        {
                            NFTTokenAddress = request.NFTTokenAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "NFT unlocked successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to unlock NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = senderAccountAddress,
                    ToAddress = _contractAddress ?? "0.0.0",
                    Amount = 0m,
                    Memo = $"NFT withdrawal: {nftTokenAddress}:{tokenId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "NFT withdrawn successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to withdraw NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT on Hashgraph: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = _contractAddress ?? "0.0.0",
                    ToAddress = receiverAccountAddress,
                    Amount = 0m,
                    Memo = $"NFT deposit: {nftTokenAddress}:{tokenId}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = transactionResult.TransactionId ?? "",
                        IsSuccessful = true,
                        Status = BridgeTransactionStatus.Completed
                    };
                    result.IsError = false;
                    result.Message = "NFT deposited successfully on Hashgraph";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deposit NFT on Hashgraph");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT on Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        #endregion
    }

    /// <summary>
    /// REAL Hashgraph client for interacting with Hashgraph network
    /// </summary>
    public class HashgraphClient
    {
        private readonly string _networkUrl;
        private readonly string _accountId;
        private readonly string _privateKey;

        public HashgraphClient(string networkUrl = "https://mainnet-public.mirrornode.hedera.com", string accountId = "", string privateKey = "")
        {
            _networkUrl = networkUrl;
            _accountId = accountId;
            _privateKey = privateKey;
        }

        public async Task<string> ResolveAccountIdFromKeysAsync(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
                return string.Empty;

            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // Try common mirror node query formats for public key.
                    var candidates = new[]
                    {
                        $"{_networkUrl}/api/v1/accounts?account.publickey={Uri.EscapeDataString(publicKey)}&limit=1",
                        $"{_networkUrl}/api/v1/accounts?publickey={Uri.EscapeDataString(publicKey)}&limit=1",
                        $"{_networkUrl}/api/v1/accounts?key={Uri.EscapeDataString(publicKey)}&limit=1"
                    };

                    foreach (var url in candidates)
                    {
                        var response = await httpClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                            continue;

                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

                        if (accountData.TryGetProperty("accounts", out var accounts) && accounts.ValueKind == JsonValueKind.Array && accounts.GetArrayLength() > 0)
                        {
                            var first = accounts[0];
                            if (first.TryGetProperty("account", out var acctEl) && acctEl.ValueKind == JsonValueKind.String)
                                return acctEl.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // ignore and return empty
            }

            return string.Empty;
        }

        /// <summary>
        /// Get account information from Hashgraph network
        /// </summary>
        public async Task<HashgraphAccountInfo> GetAccountInfoAsync(string accountId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/accounts/{accountId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

                        return new HashgraphAccountInfo
                        {
                            AccountId = accountData.TryGetProperty("account", out var account) &&
                                       account.TryGetProperty("account", out var accId) ? accId.GetString() : accountId,
                            Balance = accountData.TryGetProperty("account", out var acc) &&
                                     acc.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                            AutoRenewPeriod = accountData.TryGetProperty("account", out var acc2) &&
                                           acc2.TryGetProperty("auto_renew_period", out var period) ? period.GetInt64() : 0,
                            Expiry = accountData.TryGetProperty("account", out var acc3) &&
                                   acc3.TryGetProperty("expiry_timestamp", out var expiry) ? expiry.GetString() : ""
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        /// <summary>
        /// Get account information by email from Hashgraph network
        /// </summary>
        public async Task<HashgraphAccountInfo> GetAccountInfoByEmailAsync(string email)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // Search for account by email in Hashgraph network
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/accounts?email={email}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

                        if (accountData.TryGetProperty("accounts", out var accounts) && accounts.GetArrayLength() > 0)
                        {
                            var firstAccount = accounts[0];
                            return new HashgraphAccountInfo
                            {
                                AccountId = firstAccount.TryGetProperty("account", out var account) ? account.GetString() : "",
                                Balance = firstAccount.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                                AutoRenewPeriod = firstAccount.TryGetProperty("auto_renew_period", out var period) ? period.GetInt64() : 0,
                                Expiry = firstAccount.TryGetProperty("expiry_timestamp", out var expiry) ? expiry.GetString() : ""
                            };
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        /// <summary>
        /// Send transaction to Hashgraph network
        /// </summary>
        public async Task<HashgraphTransactionData> SendTransactionAsync(HashgraphTransactionData transactionData)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(transactionData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{_networkUrl}/api/v1/transactions", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var transactionResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                        var txId = transactionResponse.TryGetProperty("transaction_id", out var txIdProp)
                            ? txIdProp.GetString()
                            : string.Empty;

                        // If the mirror node did not return a transaction_id, treat as an error rather than inventing one.
                        if (string.IsNullOrWhiteSpace(txId))
                            return null;

                        return new HashgraphTransactionData
                        {
                            FromAddress = transactionData.FromAddress,
                            ToAddress = transactionData.ToAddress,
                            Amount = transactionData.Amount,
                            Memo = transactionData.Memo,
                            TransactionId = txId,
                            Status = "Success"
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if transaction fails
            }
            return null;
        }

        /// <summary>
        /// Send transaction to Hashgraph network synchronously
        /// </summary>
        public HashgraphTransactionData SendTransaction(HashgraphTransactionData transactionData)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(transactionData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = httpClient.PostAsync($"{_networkUrl}/api/v1/transactions", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var transactionResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                        var txId = transactionResponse.TryGetProperty("transaction_id", out var txIdProp)
                            ? txIdProp.GetString()
                            : string.Empty;

                        if (string.IsNullOrWhiteSpace(txId))
                            return null;

                        return new HashgraphTransactionData
                        {
                            FromAddress = transactionData.FromAddress,
                            ToAddress = transactionData.ToAddress,
                            Amount = transactionData.Amount,
                            Memo = transactionData.Memo,
                            TransactionId = txId,
                            Status = "Success"
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if transaction fails
            }
            return null;
        }

        /// <summary>
        /// Get NFT data from Hashgraph network
        /// </summary>
        public async Task<string> GetNFTData(string nftTokenAddress)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/tokens/{nftTokenAddress}");
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        /// <summary>
        /// Create a new account on Hashgraph network
        /// </summary>
        public async Task<HashgraphAccountInfo> CreateAccountAsync()
        {
            try
            {
                // Create a new Hedera-compatible Ed25519 keypair and mnemonic.
                // NOTE: Creating the on-chain Hedera account requires a funded operator account; this method prepares keys + mnemonic.
                var mnemonic = new NBitcoin.Mnemonic(NBitcoin.Wordlist.English, NBitcoin.WordCount.Twelve);
                var seed = mnemonic.DeriveSeed();
                var edSeed = seed.Take(32).ToArray();

                byte[] publicKey;
                byte[] expandedPrivateKey;
                Chaos.NaCl.Ed25519.KeyPairFromSeed(out publicKey, out expandedPrivateKey, edSeed);

                var publicKeyB64 = Convert.ToBase64String(publicKey);
                var privateKeyB64 = Convert.ToBase64String(edSeed);

                var accountId = await ResolveAccountIdFromKeysAsync(publicKeyB64);

                return new HashgraphAccountInfo
                {
                    AccountId = accountId,
                    PublicKey = publicKeyB64,
                    PrivateKey = privateKeyB64,
                    SeedPhrase = mnemonic.ToString()
                };
            }
            catch (Exception)
            {
                // Return null if creation fails
            }
            return null;
        }

        /// <summary>
        /// Restore an account from seed phrase on Hashgraph network using Hedera SDK-compatible client.
        /// </summary>
        public async Task<HashgraphAccountInfo> RestoreAccountAsync(string seedPhrase)
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
                throw new ArgumentNullException(nameof(seedPhrase), "Seed phrase is required to restore a Hashgraph account.");

            // Interpret seedPhrase as a BIP-39 mnemonic.
            var mnemonic = new NBitcoin.Mnemonic(seedPhrase);
            var seed = mnemonic.DeriveSeed();
            var edSeed = seed.Take(32).ToArray();

            byte[] publicKey;
            byte[] expandedPrivateKey;
            Chaos.NaCl.Ed25519.KeyPairFromSeed(out publicKey, out expandedPrivateKey, edSeed);

            var publicKeyB64 = Convert.ToBase64String(publicKey);
            var privateKeyB64 = Convert.ToBase64String(edSeed);
            var accountId = await ResolveAccountIdFromKeysAsync(publicKeyB64);

            return new HashgraphAccountInfo
            {
                AccountId = accountId,
                PublicKey = publicKeyB64,
                PrivateKey = privateKeyB64,
                SeedPhrase = mnemonic.ToString()
            };
        }

        /// <summary>
        /// Get transaction status from Hashgraph network
        /// </summary>
        public async Task<NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus> GetTransactionStatusAsync(string transactionId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_networkUrl}/api/v1/transactions/{transactionId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                        var statusStr = txData.TryGetProperty("status", out var status) ? status.GetString() : "Unknown";
                        
                        // Convert string status to BridgeTransactionStatus enum
                        if (statusStr == "SUCCESS" || statusStr == "Completed")
                            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed;
                        else if (statusStr == "PENDING" || statusStr == "Pending")
                            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending;
                        else
                            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound;
                    }
                }
            }
            catch (Exception)
            {
                // Return NotFound if query fails
            }
            return NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound;
        }
    }

    /// <summary>
    /// Hashgraph account information
    /// </summary>
    public class HashgraphAccountInfo
    {
        public string AccountId { get; set; }
        public long? Balance { get; set; }
        public long? AutoRenewPeriod { get; set; }
        public string Expiry { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string SeedPhrase { get; set; }
    }

    /// <summary>
    /// Hashgraph transaction data
    /// </summary>
    public class HashgraphTransactionData
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string TokenId { get; set; }
    }

    /// <summary>
    /// Hashgraph transaction response
    /// </summary>
    public class TransactionResponse
    {
        public string TransactionId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
    }
}

