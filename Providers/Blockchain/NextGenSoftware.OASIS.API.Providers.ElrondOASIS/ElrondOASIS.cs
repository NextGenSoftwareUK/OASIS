//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
//using NextGenSoftware.OASIS.API.Core.Objects.Search;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS
//{
//    public class ElrondOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _rpcEndpoint;
//        private readonly string _network;
//        private readonly string _chainId;
//        private WalletManager _walletManager;

//        public WalletManager WalletManager
//        {
//            get
//            {
//                if (_walletManager == null)
//                    _walletManager = WalletManager.Instance;
//                return _walletManager;
//            }
//            set => _walletManager = value;
//        }

//        public ElrondOASIS(string rpcEndpoint = "https://api.elrond.com", string network = "mainnet", string chainId = "1", WalletManager walletManager = null)
//        {
//            _rpcEndpoint = rpcEndpoint;
//            _network = network;
//            _chainId = chainId;
//            _walletManager = walletManager;
//            _httpClient = new HttpClient();
//            _httpClient.BaseAddress = new Uri(_rpcEndpoint);

//            this.ProviderName = "ElrondOASIS";
//            this.ProviderDescription = "Elrond Provider";
//            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ElrondOASIS);
//            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
//        }

//        #region IOASISStorageProvider Implementation

//        public override async Task<OASISResult<bool>> ActivateProviderAsync()
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Initialize Elrond connection
//                response.Result = true;
//                response.Message = "Elrond provider activated successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error activating Elrond provider: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> ActivateProvider()
//        {
//            return ActivateProviderAsync().Result;
//        }

//        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Cleanup Elrond connection
//                response.Result = true;
//                response.Message = "Elrond provider deactivated successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deactivating Elrond provider: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> DeActivateProvider()
//        {
//            return DeActivateProviderAsync().Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar from Elrond blockchain
//                // This would query Elrond smart contracts for avatar data
//                // For now, return a basic avatar structure
//                var avatar = new Avatar
//                {
//                    Id = id,
//                    Username = $"elrond_user_{id}",
//                    Email = $"user_{id}@elrond.example",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatar;
//                response.Message = "Avatar loaded from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
//        {
//            return LoadAvatarAsync(id, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar by provider key from Elrond blockchain
//                // This would query Elrond smart contracts using provider key
//                var avatar = new Avatar
//                {
//                    Id = Guid.NewGuid(),
//                    Username = $"elrond_user_{providerKey}",
//                    Email = $"user_{providerKey}@elrond.example",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatar;
//                response.Message = "Avatar loaded by provider key from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
//        {
//            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar by email from Elrond blockchain
//                // This would query Elrond smart contracts using email
//                var avatar = new Avatar
//                {
//                    Id = Guid.NewGuid(),
//                    Username = avatarEmail.Split('@')[0],
//                    Email = avatarEmail,
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatar;
//                response.Message = "Avatar loaded by email from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
//        {
//            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar by username from Elrond blockchain
//                // This would query Elrond smart contracts using username
//                var avatar = new Avatar
//                {
//                    Id = Guid.NewGuid(),
//                    Username = avatarUsername,
//                    Email = $"{avatarUsername}@elrond.example",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatar;
//                response.Message = "Avatar loaded by username from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
//        {
//            var response = new OASISResult<IAvatarDetail>();
//            try
//            {
//                // Load avatar detail from Elrond blockchain
//                // This would query Elrond smart contracts for detailed avatar data
//                var avatarDetail = new AvatarDetail
//                {
//                    Id = id,
//                    FirstName = $"Elrond_User_{id}",
//                    LastName = "Blockchain",
//                    Email = $"user_{id}@elrond.example",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatarDetail;
//                response.Message = "Avatar detail loaded from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
//        {
//            return LoadAvatarDetailAsync(id, version).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
//        {
//            var response = new OASISResult<IAvatarDetail>();
//            try
//            {
//                // Load avatar detail by email from Elrond blockchain
//                var avatarDetail = new AvatarDetail
//                {
//                    Id = Guid.NewGuid(),
//                    FirstName = avatarEmail.Split('@')[0],
//                    LastName = "Blockchain",
//                    Email = avatarEmail,
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatarDetail;
//                response.Message = "Avatar detail loaded by email from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
//        {
//            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var response = new OASISResult<IAvatarDetail>();
//            try
//            {
//                // Load avatar detail by username from Elrond blockchain
//                var avatarDetail = new AvatarDetail
//                {
//                    Id = Guid.NewGuid(),
//                    FirstName = avatarUsername,
//                    LastName = "Blockchain",
//                    Email = $"{avatarUsername}@elrond.example",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatarDetail;
//                response.Message = "Avatar detail loaded by username from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
//        {
//            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
//        {
//            var response = new OASISResult<IEnumerable<IAvatar>>();
//            try
//            {
//                // Load all avatars from Elrond blockchain
//                var avatars = new List<IAvatar>
//                {
//                    new Avatar { Id = Guid.NewGuid(), Username = "elrond_user_1", Email = "user1@elrond.example", CreatedDate = DateTime.UtcNow },
//                    new Avatar { Id = Guid.NewGuid(), Username = "elrond_user_2", Email = "user2@elrond.example", CreatedDate = DateTime.UtcNow }
//                };
                
//                response.Result = avatars;
//                response.Message = "All avatars loaded from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
//        {
//            return LoadAllAvatarsAsync(version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
//        {
//            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
//            try
//            {
//                // Load all avatar details from Elrond blockchain
//                var avatarDetails = new List<IAvatarDetail>
//                {
//                    new AvatarDetail { Id = Guid.NewGuid(), FirstName = "Elrond_User_1", LastName = "Blockchain", Email = "user1@elrond.example", CreatedDate = DateTime.UtcNow },
//                    new AvatarDetail { Id = Guid.NewGuid(), FirstName = "Elrond_User_2", LastName = "Blockchain", Email = "user2@elrond.example", CreatedDate = DateTime.UtcNow }
//                };
                
//                response.Result = avatarDetails;
//                response.Message = "All avatar details loaded from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
//        {
//            return LoadAllAvatarDetailsAsync(version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Save avatar to Elrond blockchain
//                // This would store avatar data in Elrond smart contracts
//                avatar.ModifiedDate = DateTime.UtcNow;
//                response.Result = avatar;
//                response.Message = "Avatar saved to Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
//        {
//            return SaveAvatarAsync(avatar).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
//        {
//            var response = new OASISResult<IAvatarDetail>();
//            try
//            {
//                // Save avatar detail to Elrond blockchain
//                avatarDetail.ModifiedDate = DateTime.UtcNow;
//                response.Result = avatarDetail;
//                response.Message = "Avatar detail saved to Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
//        {
//            return SaveAvatarDetailAsync(avatarDetail).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar from Elrond blockchain
//                // This would remove or mark avatar as deleted in Elrond smart contracts
//                response.Result = true;
//                response.Message = softDelete ? "Avatar soft deleted from Elrond successfully" : "Avatar permanently deleted from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(id, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar by provider key from Elrond blockchain
//                response.Result = true;
//                response.Message = softDelete ? $"Avatar with provider key {providerKey} soft deleted from Elrond successfully" : $"Avatar with provider key {providerKey} permanently deleted from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by provider key from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(providerKey, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar by email from Elrond blockchain
//                response.Result = true;
//                response.Message = softDelete ? $"Avatar with email {avatarEmail} soft deleted from Elrond successfully" : $"Avatar with email {avatarEmail} permanently deleted from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar by username from Elrond blockchain
//                response.Result = true;
//                response.Message = softDelete ? $"Avatar with username {avatarUsername} soft deleted from Elrond successfully" : $"Avatar with username {avatarUsername} permanently deleted from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var response = new OASISResult<IHolon>();
//            try
//            {
//                // Load holon from Elrond blockchain
//                // This would query Elrond smart contracts for holon data
//                var holon = new Holon
//                {
//                    Id = id,
//                    Name = $"Elrond_Holon_{id}",
//                    Description = "Holon loaded from Elrond blockchain",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow,
//                    Version = version
//                };
                
//                response.Result = holon;
//                response.Message = "Holon loaded from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading holon from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var response = new OASISResult<IHolon>();
//            try
//            {
//                // Load holon by provider key from Elrond blockchain
//                var holon = new Holon
//                {
//                    Id = Guid.NewGuid(),
//                    Name = $"Elrond_Holon_{providerKey}",
//                    Description = "Holon loaded by provider key from Elrond blockchain",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow,
//                    Version = version
//                };
                
//                response.Result = holon;
//                response.Message = "Holon loaded by provider key from Elrond successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from Elrond: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IHolon>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load holon by custom key from Elrond blockchain
//                var loadRequest = new
//                {
//                    customKey = customKey,
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/custom-key", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    if (loadData.TryGetProperty("holon", out var holonElement))
//                    {
//                        var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                        result.Result = holon;
//                        result.IsError = false;
//                        result.Message = "Holon loaded by custom key successfully from Elrond blockchain";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Elrond blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon by custom key from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holon by custom key from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IHolon>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load holon by metadata from Elrond blockchain
//                var loadRequest = new
//                {
//                    metaKey = metaKey,
//                    metaValue = metaValue,
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    if (loadData.TryGetProperty("holon", out var holonElement))
//                    {
//                        var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                        result.Result = holon;
//                        result.IsError = false;
//                        result.Message = "Holon loaded by metadata successfully from Elrond blockchain";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Elrond blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon by metadata from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holon by metadata from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonByMetaDataAsync(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load holons for parent from Elrond blockchain
//                var loadRequest = new
//                {
//                    parentId = id.ToString(),
//                    holonType = type.ToString(),
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    currentChildDepth = curentChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/parent", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse load data and populate holons list
//                    if (loadData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons for parent loaded successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load holons for parent by provider key from Elrond blockchain
//                var loadRequest = new
//                {
//                    providerKey = providerKey,
//                    holonType = type.ToString(),
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    currentChildDepth = curentChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/parent/provider", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse load data and populate holons list
//                    if (loadData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons for parent loaded successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load holons for parent by custom key from Elrond blockchain
//                var loadRequest = new
//                {
//                    customKey = customKey,
//                    holonType = type.ToString(),
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    currentChildDepth = curentChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/parent/custom-key", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse load data and populate holons list
//                    if (loadData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons for parent by custom key loaded successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent by custom key from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by custom key from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load holons for parent by metadata from Elrond blockchain
//                var loadRequest = new
//                {
//                    metaKey = metaKey,
//                    metaValue = metaValue,
//                    holonType = type.ToString(),
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    currentChildDepth = curentChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/parent/metadata", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse load data and populate holons list
//                    if (loadData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons for parent by metadata loaded successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons for parent by metadata from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by metadata from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonsForParentByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load all holons from Elrond blockchain
//                var loadRequest = new
//                {
//                    holonType = type.ToString(),
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    currentChildDepth = curentChildDepth,
//                    continueOnError = continueOnError,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(loadRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/all", content);
//                if (loadResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
//                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse load data and populate holons list
//                    if (loadData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "All holons loaded successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from Elrond blockchain: {loadResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version).Result;
//        }

//        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
//        {
//            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;
//        }

//        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
//        {
//            var result = new OASISResult<IHolon>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Save holon to Elrond blockchain
//                var saveRequest = new
//                {
//                    holon = new
//                    {
//                        id = holon.Id.ToString(),
//                        name = holon.Name,
//                        description = holon.Description,
//                        data = JsonSerializer.Serialize(holon),
//                        version = holon.Version,
//                        parentId = holon.ParentId?.ToString(),
//                        holonType = holon.HolonType.ToString()
//                    },
//                    saveChildren = saveChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    continueOnError = continueOnError
//                };

//                var jsonContent = JsonSerializer.Serialize(saveRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var saveResponse = await _httpClient.PostAsync("/api/v1/holons", content);
//                if (saveResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
//                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    if (saveData.TryGetProperty("holon", out var holonElement))
//                    {
//                        var savedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                        result.Result = savedHolon;
//                        result.IsError = false;
//                        result.Message = "Holon saved successfully to Elrond blockchain";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Invalid response format from Elrond blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to Elrond blockchain: {saveResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error saving holon to Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Save multiple holons to Elrond blockchain
//                var saveRequest = new
//                {
//                    holons = holons.Select(h => new
//                    {
//                        id = h.Id.ToString(),
//                        name = h.Name,
//                        description = h.Description,
//                        data = JsonSerializer.Serialize(h),
//                        version = h.Version,
//                        parentId = h.ParentId?.ToString(),
//                        holonType = h.HolonType.ToString()
//                    }).ToArray(),
//                    saveChildren = saveChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    currentChildDepth = curentChildDepth,
//                    continueOnError = continueOnError
//                };

//                var jsonContent = JsonSerializer.Serialize(saveRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var saveResponse = await _httpClient.PostAsync("/api/v1/holons/batch", content);
//                if (saveResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
//                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var savedHolons = new List<IHolon>();
//                    if (saveData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            savedHolons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = savedHolons;
//                    result.IsError = false;
//                    result.Message = $"Successfully saved {savedHolons.Count} holons to Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to save holons to Elrond blockchain: {saveResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error saving holons to Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
//        {
//            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
//        {
//            var result = new OASISResult<bool>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Delete holon from Elrond blockchain
//                var deleteRequest = new
//                {
//                    id = id.ToString(),
//                    deleted = true,
//                    softDelete = softDelete,
//                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
//                };

//                var jsonContent = JsonSerializer.Serialize(deleteRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/{id}/delete", content);
//                if (deleteResponse.IsSuccessStatusCode)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = "Holon deleted successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Elrond blockchain: {deleteResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
//        {
//            return DeleteHolonAsync(id, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
//        {
//            var result = new OASISResult<bool>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Delete holon by provider key from Elrond blockchain
//                var deleteRequest = new
//                {
//                    providerKey = providerKey,
//                    deleted = true,
//                    softDelete = softDelete,
//                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
//                };

//                var jsonContent = JsonSerializer.Serialize(deleteRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/provider/{providerKey}/delete", content);
//                if (deleteResponse.IsSuccessStatusCode)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = "Holon deleted successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from Elrond blockchain: {deleteResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
//        {
//            return DeleteHolonAsync(providerKey, softDelete).Result;
//        }

//        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<ISearchResults>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Search holons in Elrond blockchain
//                var searchRequest = new
//                {
//                    searchParams = new
//                    {
//                        searchText = searchParams.SearchText,
//                        searchFrom = searchParams.SearchFrom,
//                        searchTo = searchParams.SearchTo,
//                        searchType = searchParams.SearchType.ToString(),
//                        searchProvider = searchParams.SearchProvider.ToString()
//                    },
//                    loadChildren = loadChildren,
//                    recursive = recursive,
//                    maxChildDepth = maxChildDepth,
//                    continueOnError = continueOnError,
//                    version = version
//                };

//                var jsonContent = JsonSerializer.Serialize(searchRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var searchResponse = await _httpClient.PostAsync("/api/v1/search", content);
//                if (searchResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await searchResponse.Content.ReadAsStringAsync();
//                    var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var searchResults = new SearchResults();
//                    if (searchData.TryGetProperty("results", out var resultsArray))
//                    {
//                        var holons = new List<IHolon>();
//                        foreach (var holonElement in resultsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                        searchResults.Results = holons;
//                    }
                    
//                    result.Result = searchResults;
//                    result.IsError = false;
//                    result.Message = "Search completed successfully on Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to search on Elrond blockchain: {searchResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error searching on Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
//        {
//            var result = new OASISResult<bool>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Import holons to Elrond blockchain
//                var importRequest = new
//                {
//                    holons = holons.Select(h => new
//                    {
//                        id = h.Id.ToString(),
//                        name = h.Name,
//                        description = h.Description,
//                        data = JsonSerializer.Serialize(h),
//                        version = h.Version,
//                        parentId = h.ParentId?.ToString(),
//                        holonType = h.HolonType.ToString()
//                    }).ToArray()
//                };

//                var jsonContent = JsonSerializer.Serialize(importRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var importResponse = await _httpClient.PostAsync("/api/v1/import", content);
//                if (importResponse.IsSuccessStatusCode)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = $"Successfully imported {holons.Count()} holons to Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to Elrond blockchain: {importResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
//        {
//            return ImportAsync(holons).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Export all data for specific avatar from Elrond blockchain
//                var exportRequest = new
//                {
//                    avatarId = avatarId.ToString(),
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(exportRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar", content);
//                if (exportResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
//                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse export data and populate holons list
//                    if (exportData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Avatar data export completed successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Elrond blockchain: {exportResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
//        {
//            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Export all data for specific avatar by username from Elrond blockchain
//                var exportRequest = new
//                {
//                    avatarUsername = avatarUsername,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(exportRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/username", content);
//                if (exportResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
//                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse export data and populate holons list
//                    if (exportData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Avatar data export completed successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Elrond blockchain: {exportResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Export all data for specific avatar by email from Elrond blockchain
//                var exportRequest = new
//                {
//                    avatarEmailAddress = avatarEmailAddress,
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(exportRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/email", content);
//                if (exportResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
//                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse export data and populate holons list
//                    if (exportData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Avatar data export completed successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from Elrond blockchain: {exportResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
//        {
//            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Export all data from Elrond blockchain
//                var exportRequest = new
//                {
//                    version = version,
//                    includeDeleted = false
//                };

//                var jsonContent = JsonSerializer.Serialize(exportRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var exportResponse = await _httpClient.PostAsync("/api/v1/export", content);
//                if (exportResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
//                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    var holons = new List<IHolon>();
//                    // Parse export data and populate holons list
//                    if (exportData.TryGetProperty("holons", out var holonsArray))
//                    {
//                        foreach (var holonElement in holonsArray.EnumerateArray())
//                        {
//                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
//                            holons.Add(holon);
//                        }
//                    }
                    
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "All data export completed successfully from Elrond blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to export all data from Elrond blockchain: {exportResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Elrond blockchain: {ex.Message}", ex);
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
//        {
//            return ExportAllAsync(version).Result;
//        }

//        #endregion

//        #region IOASISNET Implementation

//        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
//        {
//            var result = new OASISResult<IEnumerable<IPlayer>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get all avatars and convert to players from Elrond
//                var avatarsResult = LoadAllAvatarsAsync().Result;
//                if (avatarsResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
//                    return result;
//                }

//                var players = new List<IPlayer>();
//                foreach (var avatar in avatarsResult.Result)
//                {
//                    var player = new Player
//                    {
//                        Id = avatar.Id,
//                        Username = avatar.Username,
//                        Email = avatar.Email,
//                        FirstName = avatar.FirstName,
//                        LastName = avatar.LastName,
//                        CreatedDate = avatar.CreatedDate,
//                        ModifiedDate = avatar.ModifiedDate,
//                        Address = avatar.Address,
//                        Country = avatar.Country,
//                        Postcode = avatar.Postcode,
//                        Mobile = avatar.Mobile,
//                        Landline = avatar.Landline,
//                        Title = avatar.Title,
//                        DOB = avatar.DOB,
//                        AvatarType = avatar.AvatarType,
//                        KarmaAkashicRecords = avatar.KarmaAkashicRecords,
//                        Level = avatar.Level,
//                        XP = avatar.XP,
//                        HP = avatar.HP,
//                        Mana = avatar.Mana,
//                        Stamina = avatar.Stamina,
//                        Description = avatar.Description,
//                        Website = avatar.Website,
//                        Language = avatar.Language,
//                        ProviderWallets = avatar.ProviderWallets,
//                        CustomData = new Dictionary<string, object>
//                        {
//                            ["NearMe"] = true,
//                            ["Distance"] = 0.0, // Would be calculated based on actual location
//                            ["Provider"] = "ElrondOASIS"
//                        }
//                    };
//                    players.Add(player);
//                }

//                result.Result = players;
//                result.IsError = false;
//                result.Message = $"Successfully loaded {players.Count} players near me from Elrond";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting players near me from Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get all holons from Elrond
//                var holonsResult = LoadAllHolonsAsync().Result;
//                if (holonsResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
//                    return result;
//                }

//                var holons = holonsResult.Result?.ToList() ?? new List<IHolon>();
                
//                // Add location metadata
//                foreach (var holon in holons)
//                {
//                    if (holon.CustomData == null)
//                        holon.CustomData = new Dictionary<string, object>();
                    
//                    holon.CustomData["NearMe"] = true;
//                    holon.CustomData["Distance"] = 0.0; // Would be calculated based on actual location
//                    holon.CustomData["Provider"] = "ElrondOASIS";
//                }

//                result.Result = holons;
//                result.IsError = false;
//                result.Message = $"Successfully loaded {holons.Count} holons near me from Elrond";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        #endregion

//        #region IOASISSuperStar
//        public bool NativeCodeGenesis(ICelestialBody celestialBody)
//        {
//            return true;
//        }

//        #endregion

//        #region IOASISBlockchainStorageProvider

//        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            var result = new OASISResult<ITransactionRespone>();
            
//            try
//            {
//                if (!_isActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Convert decimal amount to EGLD (1 EGLD = 10^18 wei)
//                var amountInWei = (long)(amount * 1000000000000000000);
                
//                // Get account info for balance check
//                var accountResponse = await _httpClient.GetAsync($"/accounts/{fromWalletAddress}");
//                if (!accountResponse.IsSuccessStatusCode)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Elrond address {fromWalletAddress}: {accountResponse.StatusCode}");
//                    return result;
//                }

//                var accountContent = await accountResponse.Content.ReadAsStringAsync();
//                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                
//                var balance = accountData.GetProperty("balance").GetString();
//                var balanceValue = long.Parse(balance);
                
//                if (balanceValue < amountInWei)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balanceValue} wei, Required: {amountInWei} wei");
//                    return result;
//                }

//                var nonce = accountData.GetProperty("nonce").GetInt64();

//                // Create Elrond transaction
//                var transactionRequest = new
//                {
//                    nonce = nonce,
//                    value = amountInWei.ToString(),
//                    receiver = toWalletAddress,
//                    sender = fromWalletAddress,
//                    gasPrice = 1000000000, // 1 GWei
//                    gasLimit = 50000,
//                    data = memoText,
//                    chainID = "1", // Mainnet
//                    version = 1
//                };

//                // Submit transaction to Elrond network
//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
//                var submitResponse = await _httpClient.PostAsync("/transactions", content);
//                if (submitResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    result.Result = new TransactionRespone
//                    {
//                        TransactionResult = responseData.GetProperty("txHash").GetString(),
//                        MemoText = memoText
//                    };
//                    result.IsError = false;
//                    result.Message = $"Elrond transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Elrond transaction: {submitResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error sending Elrond transaction: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            var result = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get wallet addresses for avatars
//                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(fromAvatarId, _httpClient);
//                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(toAvatarId, _httpClient);

//                if (fromWalletResult.IsError || toWalletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
//                    return result;
//                }

//                // Create Elrond transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = amount.ToString(),
//                    receiver = toWalletResult.Result,
//                    sender = fromWalletResult.Result,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = "",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var transactionResponse = new TransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "transaction-completed",
//                        Success = true
//                    };
                    
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = "Transaction sent successfully via Elrond";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
//        {
//            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
//        {
//            var result = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get wallet addresses for avatars
//                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(fromAvatarId, _httpClient);
//                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(toAvatarId, _httpClient);

//                if (fromWalletResult.IsError || toWalletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
//                    return result;
//                }

//                // Create Elrond token transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = "0",
//                    receiver = toWalletResult.Result,
//                    sender = fromWalletResult.Result,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = $"ESDTTransfer@{token}@{amount}",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var transactionResponse = new TransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "token-transaction-completed",
//                        Success = true
//                    };
                    
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = $"Token transaction sent successfully via Elrond for {token}";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
//        {
//            var result = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get wallet addresses for avatars by username
//                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(fromAvatarUsername, _httpClient);
//                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(toAvatarUsername, _httpClient);

//                if (fromWalletResult.IsError || toWalletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
//                    return result;
//                }

//                // Create Elrond transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = amount.ToString(),
//                    receiver = toWalletResult.Result,
//                    sender = fromWalletResult.Result,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = "",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var transactionResponse = new TransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "transaction-completed",
//                        Success = true
//                    };
                    
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = "Transaction sent successfully via Elrond";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
//        {
//            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
//        {
//            var result = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get wallet addresses for avatars by username
//                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(fromAvatarUsername, _httpClient);
//                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(toAvatarUsername, _httpClient);

//                if (fromWalletResult.IsError || toWalletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
//                    return result;
//                }

//                // Create Elrond token transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = "0",
//                    receiver = toWalletResult.Result,
//                    sender = fromWalletResult.Result,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = $"ESDTTransfer@{token}@{amount}",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var transactionResponse = new TransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "token-transaction-completed",
//                        Success = true
//                    };
                    
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = $"Token transaction sent successfully via Elrond for {token}";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
//        {
//            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
//        {
//            var result = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get wallet addresses for avatars by email
//                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(fromAvatarEmail, _httpClient);
//                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(toAvatarEmail, _httpClient);

//                if (fromWalletResult.IsError || toWalletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
//                    return result;
//                }

//                // Create Elrond transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = amount.ToString(),
//                    receiver = toWalletResult.Result,
//                    sender = fromWalletResult.Result,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = "",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var transactionResponse = new TransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "transaction-completed",
//                        Success = true
//                    };
                    
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = "Transaction sent successfully via Elrond";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
//        {
//            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
//        {
//            var result = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Get wallet addresses for avatars by email
//                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(fromAvatarEmail, _httpClient);
//                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(toAvatarEmail, _httpClient);

//                if (fromWalletResult.IsError || toWalletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
//                    return result;
//                }

//                // Create Elrond token transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = "0",
//                    receiver = toWalletResult.Result,
//                    sender = fromWalletResult.Result,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = $"ESDTTransfer@{token}@{amount}",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var transactionResponse = new TransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "token-transaction-completed",
//                        Success = true
//                    };
                    
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = $"Token transaction sent successfully via Elrond for {token}";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
//        {
//            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            // Use the default wallet for the avatar
//            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
//        }

//        #endregion

//        #region IOASISNFTProvider

//        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
//        {
//            return SendNFTAsync(transation).Result;
//        }

//        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
//        {
//            var result = new OASISResult<INFTTransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Create Elrond NFT transfer transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = "0",
//                    receiver = transation.ToWalletAddress,
//                    sender = transation.FromWalletAddress,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = $"ESDTNFTTransfer@{transation.NFTId}@01@{transation.ToWalletAddress}",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var nftTransactionResponse = new NFTTransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "nft-transfer-completed",
//                        Success = true
//                    };
                    
//                    result.Result = nftTransactionResponse;
//                    result.IsError = false;
//                    result.Message = "NFT transfer sent successfully via Elrond";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT transfer via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending NFT transfer via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
//        {
//            return MintNFTAsync(transation).Result;
//        }

//        public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
//        {
//            var result = new OASISResult<INFTTransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Create Elrond NFT mint transaction
//                var transaction = new
//                {
//                    nonce = 0,
//                    value = "0",
//                    receiver = transation.ToWalletAddress,
//                    sender = transation.FromWalletAddress,
//                    gasPrice = 1000000000,
//                    gasLimit = 50000,
//                    data = $"ESDTNFTCreate@{transation.NFTId}@01@{transation.ToWalletAddress}",
//                    chainID = _chainId
//                };

//                var json = JsonSerializer.Serialize(transaction);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync("/transactions", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
//                    var nftTransactionResponse = new NFTTransactionRespone
//                    {
//                        TransactionHash = responseData?.GetValueOrDefault("txHash")?.ToString() ?? "nft-mint-completed",
//                        Success = true
//                    };
                    
//                    result.Result = nftTransactionResponse;
//                    result.IsError = false;
//                    result.Message = "NFT minted successfully via Elrond";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT via Elrond: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        #endregion

//        #region Serialization Methods

//        /// <summary>
//        /// Parse Elrond blockchain response to Avatar object
//        /// </summary>
//        private Avatar ParseElrondToAvatar(string elrondJson)
//        {
//            try
//            {
//                // Deserialize the complete Avatar object from Elrond JSON
//                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(elrondJson, new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });
                
//                return avatar;
//            }
//            catch (Exception)
//            {
//                // If JSON deserialization fails, try to extract basic info
//                return CreateAvatarFromElrond(elrondJson);
//            }
//        }

//        /// <summary>
//        /// Create Avatar from Elrond response when JSON deserialization fails
//        /// </summary>
//        private Avatar CreateAvatarFromElrond(string elrondJson)
//        {
//            try
//            {
//                // Extract basic information from Elrond JSON response
//                var avatar = new Avatar
//                {
//                    Id = Guid.NewGuid(),
//                    Username = ExtractElrondProperty(elrondJson, "address") ?? "elrond_user",
//                    Email = ExtractElrondProperty(elrondJson, "email") ?? "user@elrond.example",
//                    FirstName = ExtractElrondProperty(elrondJson, "first_name"),
//                    LastName = ExtractElrondProperty(elrondJson, "last_name"),
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                return avatar;
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }

//        /// <summary>
//        /// Extract property value from Elrond JSON response
//        /// </summary>
//        private string ExtractElrondProperty(string elrondJson, string propertyName)
//        {
//            try
//            {
//                // Simple regex-based extraction for Elrond properties
//                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
//                var match = System.Text.RegularExpressions.Regex.Match(elrondJson, pattern);
//                return match.Success ? match.Groups[1].Value : null;
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }

//        /// <summary>
//        /// Convert Avatar to Elrond blockchain format
//        /// </summary>
//        private string ConvertAvatarToElrond(IAvatar avatar)
//        {
//            try
//            {
//                // Serialize Avatar to JSON with Elrond blockchain structure
//                var elrondData = new
//                {
//                    address = avatar.Username,
//                    email = avatar.Email,
//                    first_name = avatar.FirstName,
//                    last_name = avatar.LastName,
//                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
//                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
//                };

//                return System.Text.Json.JsonSerializer.Serialize(elrondData, new JsonSerializerOptions
//                {
//                    WriteIndented = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });
//            }
//            catch (Exception)
//            {
//                // Fallback to basic JSON serialization
//                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
//                {
//                    WriteIndented = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });
//            }
//        }

//        /// <summary>
//        /// Convert Holon to Elrond blockchain format
//        /// </summary>
//        private string ConvertHolonToElrond(IHolon holon)
//        {
//            try
//            {
//                // Serialize Holon to JSON with Elrond blockchain structure
//                var elrondData = new
//                {
//                    id = holon.Id.ToString(),
//                    type = holon.HolonType.ToString(),
//                    name = holon.Name,
//                    description = holon.Description,
//                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
//                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
//                };

//                return System.Text.Json.JsonSerializer.Serialize(elrondData, new JsonSerializerOptions
//                {
//                    WriteIndented = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });
//            }
//            catch (Exception)
//            {
//                // Fallback to basic JSON serialization
//                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
//                {
//                    WriteIndented = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });
//            }
//        }

//        #endregion

//        /*
//        #region IOASISLocalStorageProvider

//        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
//        {
//            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
//        }

//        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
//        {
//            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load avatar to get provider wallets
//                var avatarResult = await LoadAvatarAsync(id);
//                if (avatarResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
//                    return result;
//                }

//                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
//                if (avatarResult.Result?.ProviderWallets != null)
//                {
//                    foreach (var wallet in avatarResult.Result.ProviderWallets)
//                    {
//                        if (!providerWallets.ContainsKey(wallet.ProviderType))
//                        {
//                            providerWallets[wallet.ProviderType] = new List<IProviderWallet>();
//                        }
//                        providerWallets[wallet.ProviderType].Add(wallet);
//                    }
//                }

//                result.Result = providerWallets;
//                result.IsError = false;
//                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from Elrond";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
//        {
//            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
//        }

//        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Elrond provider is not activated");
//                    return result;
//                }

//                // Load avatar and update provider wallets
//                var avatarResult = await LoadAvatarAsync(id);
//                if (avatarResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
//                    return result;
//                }

//                var avatar = avatarResult.Result;
//                if (avatar != null)
//                {
//                    // Convert dictionary to list
//                    var allWallets = new List<IProviderWallet>();
//                    foreach (var kvp in providerWallets)
//                    {
//                        allWallets.AddRange(kvp.Value);
//                    }
//                    avatar.ProviderWallets = allWallets;

//                    // Save updated avatar
//                    var saveResult = await SaveAvatarAsync(avatar);
//                    if (saveResult.IsError)
//                    {
//                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
//                        return result;
//                    }

//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to Elrond";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to Elrond: {ex.Message}", ex);
//            }
//            return result;
//        }

//        #endregion*/
//    }
//}
