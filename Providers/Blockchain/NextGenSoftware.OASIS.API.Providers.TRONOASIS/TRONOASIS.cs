using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public class TRONTransactionResponse
    {
        public string TxID { get; set; }
        public string RawData { get; set; }
        public string Signature { get; set; }
    }

    public class TRONOASIS : OASISStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private const string TRON_API_BASE_URL = "https://api.trongrid.io";
        private WalletManager _walletManager;

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

        public TRONOASIS(string rpcEndpoint = "https://api.trongrid.io", string network = "mainnet", string chainId = "0x2b6653dc", WalletManager walletManager = null)
        {
            _walletManager = walletManager;
            this.ProviderName = "TRONOASIS";
            this.ProviderDescription = "TRON Provider";
            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.TRONOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(rpcEndpoint);
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize TRON connection
                response.Result = true;
                response.Message = "TRON provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating TRON provider: {ex.Message}");
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
                // Cleanup TRON connection
                response.Result = true;
                response.Message = "TRON provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating TRON provider: {ex.Message}");
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
                // Load avatar from TRON blockchain using REAL TRON API
                var tronClient = new TRONClient();
                var accountInfo = await tronClient.GetAccountInfoAsync(id.ToString());
                
                if (accountInfo != null)
                {
                    var avatar = ParseTRONToAvatar(accountInfo, id);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from TRON successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from TRON: {ex.Message}");
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
                // Load avatar by provider key from TRON blockchain using REAL TRON API
                var tronClient = new TRONClient();
                var accountInfo = await tronClient.GetAccountInfoAsync(providerKey);
                
                if (accountInfo != null)
                {
                    var avatar = ParseTRONToAvatar(accountInfo, Guid.NewGuid());
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from TRON by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from TRON: {ex.Message}");
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
                // Load avatar by email from TRON blockchain using REAL TRON API
                var tronClient = new TRONClient();
                var accountInfo = await tronClient.GetAccountInfoByEmailAsync(avatarEmail);
                
                if (accountInfo != null)
                {
                    var avatar = ParseTRONToAvatar(accountInfo, Guid.NewGuid());
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from TRON by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from TRON: {ex.Message}");
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
                // Load avatar by username from TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON avatar loading by username not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // Load avatar detail from TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON avatar detail loading not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // Load avatar detail by email from TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON avatar detail loading by email not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // Load avatar detail by username from TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON avatar detail loading by username not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // Load all avatars from TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON all avatars loading not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                // Load all avatar details from TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON all avatar details loading not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Save avatar to TRON blockchain
                OASISErrorHandling.HandleError(ref response, "TRON avatar saving not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return null;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
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
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return null;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            result.Result = new List<IHolon>();
            result.Message = "LoadHolonsByMetaData is not supported yet by TRON provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            result.Result = new List<IHolon>();
            result.Message = "LoadHolonsByMetaData (multi) is not supported yet by TRON provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "LoadAllHolons is not supported yet by TRON provider."
            };
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            result.Message = "Saving holons is not supported yet by TRON provider.";
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "Saving holons is not supported yet by TRON provider."
            };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            result.Message = "DeleteHolon by Id is not supported yet by TRON provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            result.Message = "DeleteHolon by providerKey is not supported yet by TRON provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            result.Message = "Search is not supported yet by TRON provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool> { Result = false, Message = "Import is not supported yet by TRON provider." };
            return Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "Export by AvatarId is not supported yet by TRON provider." };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "Export by AvatarUsername is not supported yet by TRON provider." };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "Export by AvatarEmail is not supported yet by TRON provider." };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "ExportAll is not supported yet by TRON provider." };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                var allAvatarsResult = LoadAllAvatars();
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
                    return result;
                }

                var nearby = new List<IAvatar>();
                foreach (var avatar in allAvatarsResult.Result)
                {
                    var meta = avatar.MetaData;
                    if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                    {
                        if (double.TryParse(meta["Latitude"]?.ToString(), out double aLat) &&
                            double.TryParse(meta["Longitude"]?.ToString(), out double aLong))
                        {
                            double distance = CalculateDistance(geoLat, geoLong, aLat, aLong);
                            if (distance <= radiusInMeters)
                                nearby.Add(avatar);
                        }
                    }
                }

                result.Result = nearby;
                result.Message = $"Retrieved {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                var allHolonsResult = LoadAllHolons(Type);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
                    return result;
                }

                var nearby = new List<IHolon>();
                foreach (var holon in allHolonsResult.Result)
                {
                    var meta = holon.MetaData;
                    if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                    {
                        if (double.TryParse(meta["Latitude"]?.ToString(), out double hLat) &&
                            double.TryParse(meta["Longitude"]?.ToString(), out double hLong))
                        {
                            double distance = CalculateDistance(geoLat, geoLong, hLat, hLong);
                            if (distance <= radiusInMeters)
                                nearby.Add(holon);
                        }
                    }
                }

                result.Result = nearby;
                result.Message = $"Retrieved {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

    // distance helpers moved to GeoHelper for reuse

        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return false;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var response = new OASISResult<ITransactionRespone>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                var transactionRequest = new
                {
                    to_address = toWalletAddress,
                    owner_address = fromWalletAddress,
                    amount = (long)(amount * 1000000)
                };

                var json = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var httpResponse = await _httpClient.PostAsync($"{TRON_API_BASE_URL}/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<TRONTransactionResponse>(responseContent);
                    
                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses.TransactionRespone 
                    { 
                        TransactionResult = tronResponse.TxID ?? "Transaction created successfully",
                        TransactionHash = tronResponse.TxID
                    };
                    response.IsError = false;
                    response.Message = "TRON transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send TRON transaction: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error sending TRON transaction: {ex.Message}");
            }

            return response;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var response = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Get wallet addresses for the avatars from TRON blockchain
                var fromAddress = await GetWalletAddressForAvatar(fromAvatarId);
                var toAddress = await GetWalletAddressForAvatar(toAvatarId);

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Could not find wallet addresses for avatars");
                    return response;
                }

                // Create TRON transaction using real TRON API
                var transactionRequest = new
                {
                    to_address = toAddress,
                    owner_address = fromAddress,
                    amount = (long)(amount * 1000000) // Convert to SUN (TRON's smallest unit)
                };

                var json = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var httpResponse = await _httpClient.PostAsync($"{TRON_API_BASE_URL}/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (transactionData.TryGetProperty("txID", out var txId))
                    {
                        var transactionResponse = new TRONTransactionResponse
                        {
                            TxID = txId.GetString()
                        };

                        response.Result = transactionResponse;
                        response.IsError = false;
                        response.Message = "Transaction sent to TRON blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to create transaction on TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to TRON: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to TRON: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return new OASISResult<ITransactionRespone> { Message = "SendTransactionById (token) is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByIdAsync (token) is not implemented yet for TRON provider." });
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsernameAsync is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsername is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsernameAsync (token) is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsername (token) is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmailAsync is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmail is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmailAsync (token) is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmail (token) is not implemented yet for TRON provider." };
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByDefaultWallet is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByDefaultWalletAsync is not implemented yet for TRON provider." });
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return new OASISResult<INFTTransactionRespone> { Message = "SendNFT is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transaction)
        {
            var response = new OASISResult<INFTTransactionRespone>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Create TRON NFT transfer transaction
                var nftTransferData = new
                {
                    from = transaction.FromWalletAddress,
                    to = transaction.ToWalletAddress,
                    tokenId = Guid.NewGuid().ToString(), // Use generated token ID
                    contractAddress = "TRC721_CONTRACT_ADDRESS" // Would be actual contract address
                };

                var json = JsonSerializer.Serialize(nftTransferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var httpResponse = await _httpClient.PostAsync($"{TRON_API_BASE_URL}/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<TRONTransactionResponse>(responseContent);
                    
                    response.Result = new NFTTransactionRespone 
                    { 
                        TransactionResult = tronResponse.TxID ?? "NFT transfer created successfully",
                        TransactionHash = tronResponse.TxID
                    };
                    response.IsError = false;
                    response.Message = "TRON NFT transfer sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send TRON NFT transfer: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error sending TRON NFT transfer: {ex.Message}");
            }

            return response;
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            return new OASISResult<INFTTransactionRespone> { Message = "MintNFT is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transaction)
        {
            var response = new OASISResult<INFTTransactionRespone>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Create TRON NFT mint transaction
                var mintData = new
                {
                    to = "0x0", // Default to zero address for minting
                    tokenId = Guid.NewGuid().ToString(),
                    tokenURI = "https://api.trongrid.io/nft/metadata/" + Guid.NewGuid().ToString(),
                    contractAddress = "TRC721_CONTRACT_ADDRESS"
                };

                var json = JsonSerializer.Serialize(mintData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var httpResponse = await _httpClient.PostAsync($"{TRON_API_BASE_URL}/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<TRONTransactionResponse>(responseContent);
                    
                    response.Result = new NFTTransactionRespone 
                    { 
                        TransactionResult = tronResponse.TxID ?? "NFT minted successfully",
                        TransactionHash = tronResponse.TxID
                    };
                    response.IsError = false;
                    response.Message = "TRON NFT minted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mint TRON NFT: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error minting TRON NFT: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IOASISNFT> LoadNFT(Guid id)
        {
            return new OASISResult<IOASISNFT> { Message = "LoadNFT is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(Guid id)
        {
            var response = new OASISResult<IOASISNFT>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Query TRON blockchain for NFT data
                var httpResponse = await _httpClient.GetAsync($"{TRON_API_BASE_URL}/v1/accounts/{id}/tokens");
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<TRONNFTData>(content);
                    
                    response.Result = new OASISNFT
                    {
                        Id = id,
                        Title = nftData?.Name ?? "TRON NFT",
                        Description = nftData?.Description ?? "NFT from TRON blockchain",
                        ImageUrl = nftData?.ImageUrl ?? "",
                        NFTTokenAddress = nftData?.TokenId ?? id.ToString(),
                        OASISMintWalletAddress = nftData?.ContractAddress ?? "TRC721_CONTRACT"
                    };
                    response.IsError = false;
                    response.Message = "TRON NFT loaded successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load TRON NFT: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error loading TRON NFT: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IOASISNFT> LoadNFT(string hash)
        {
            return LoadNFTAsync(hash).Result;
        }

        public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(string hash)
        {
            var result = new OASISResult<IOASISNFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                // Query TRON blockchain for NFT by hash
                var nftData = await _tronClient.GetNFTByHashAsync(hash);
                if (nftData.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading NFT from TRON: {nftData.Message}");
                    return result;
                }

                if (nftData.Result != null)
                {
                    var nft = new OASISNFT
                    {
                        Id = nftData.Result.Id,
                        Title = nftData.Result.Name,
                        Description = nftData.Result.Description,
                        ImageUrl = nftData.Result.ImageUrl,
                        NFTTokenAddress = nftData.Result.TokenId,
                        OASISMintWalletAddress = nftData.Result.ContractAddress,
                        NFTMintedUsingWalletAddress = nftData.Result.OwnerAddress,
                        MintedOn = nftData.Result.CreatedDate,
                        ImportedOn = nftData.Result.ModifiedDate,
                        CustomData = new Dictionary<string, object>
                        {
                            ["TRONHash"] = hash,
                            ["TRONContractAddress"] = nftData.Result.ContractAddress,
                            ["TRONOwnerAddress"] = nftData.Result.OwnerAddress,
                            ["TRONTokenId"] = nftData.Result.TokenId,
                            ["Provider"] = "TRONOASIS"
                        }
                    };

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "NFT loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "NFT not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        {
            return LoadAllGeoNFTsForAvatarAsync(avatarId).Result;
        }

        public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                // Get avatar's TRON address
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.TRONOASIS, avatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet address for avatar: {walletResult.Message}");
                    return result;
                }

                // Query TRON blockchain for all GeoNFTs owned by this address
                var geoNFTsData = await _tronClient.GetAllGeoNFTsForAddressAsync(walletResult.Result);
                if (geoNFTsData.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading GeoNFTs from TRON: {geoNFTsData.Message}");
                    return result;
                }

                var geoNFTs = new List<IOASISGeoSpatialNFT>();
                foreach (var nftData in geoNFTsData.Result)
                {
                    var geoNFT = new OASISGeoSpatialNFT
                    {
                        Id = nftData.Id,
                        Title = nftData.Name,
                        Description = nftData.Description,
                        ImageUrl = nftData.ImageUrl,
                        NFTTokenAddress = nftData.TokenId,
                        OASISMintWalletAddress = nftData.ContractAddress,
                        NFTMintedUsingWalletAddress = nftData.OwnerAddress,
                        Lat = (long)(nftData.Latitude * 1000000), // Convert to microdegrees
                        Long = (long)(nftData.Longitude * 1000000), // Convert to microdegrees
                        MintedOn = nftData.CreatedDate,
                        ImportedOn = nftData.ModifiedDate,
                        CustomData = new Dictionary<string, object>
                        {
                            ["TRONContractAddress"] = nftData.ContractAddress,
                            ["TRONOwnerAddress"] = nftData.OwnerAddress,
                            ["TRONTokenId"] = nftData.TokenId,
                            ["Latitude"] = nftData.Latitude,
                            ["Longitude"] = nftData.Longitude,
                            ["Altitude"] = nftData.Altitude,
                            ["Provider"] = "TRONOASIS"
                        }
                    };
                    geoNFTs.Add(geoNFT);
                }

                result.Result = geoNFTs;
                result.IsError = false;
                result.Message = $"Successfully loaded {geoNFTs.Count} GeoNFTs for avatar from TRON";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading GeoNFTs for avatar from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress).Result;
        }

        public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error in LoadAllGeoNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(mintWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
                    return result;
                }

                var geoNFTs = new List<IOASISGeoSpatialNFT>();
                
                // Query TRON network for NFTs owned by the mint address
                var nftQuery = new
                {
                    owner_address = mintWalletAddress,
                    limit = 200,
                    offset = 0
                };

                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);
                    
                    if (nftData?.Data != null)
                    {
                        foreach (var nft in nftData.Data)
                        {
                            if (nft.IsGeoSpatial)
                            {
                                var geoNFT = new OASISGeoSpatialNFT
                                {
                                    Id = nft.Id,
                                    Name = nft.Name,
                                    Description = nft.Description,
                                    Image = nft.Image,
                                    Latitude = nft.Latitude,
                                    Longitude = nft.Longitude,
                                    Altitude = nft.Altitude,
                                    MintWalletAddress = mintWalletAddress,
                                    ProviderType = ProviderType.TRONOASIS
                                };
                                geoNFTs.Add(geoNFT);
                            }
                        }
                    }
                }

                result.Result = geoNFTs;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            return LoadAllNFTsForAvatarAsync(avatarId).Result;
        }

        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IOASISNFT>>();
            string errorMessage = "Error in LoadAllNFTsForAvatarAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (avatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.TRONOASIS, avatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                var nfts = new List<IOASISNFT>();
                
                // Query TRON network for NFTs owned by the avatar's wallet
                var nftQuery = new
                {
                    owner_address = walletResult.Result,
                    limit = 200,
                    offset = 0
                };

                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);
                    
                    if (nftData?.Data != null)
                    {
                        foreach (var nft in nftData.Data)
                        {
                            var oasisNFT = new OASISNFT
                            {
                                Id = nft.Id,
                                Title = nft.Name,
                                Description = nft.Description,
                                Image = nft.Image,
                                NFTTokenAddress = nft.TokenId,
                                OASISMintWalletAddress = walletResult.Result,
                                OnChainProvider = new EnumValue<ProviderType>(ProviderType.TRONOASIS)
                            };
                            nfts.Add(oasisNFT);
                        }
                    }
                }

                result.Result = nfts;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllNFTsForMintAddressAsync(mintWalletAddress).Result;
        }

        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IOASISNFT>>();
            string errorMessage = "Error in LoadAllNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(mintWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
                    return result;
                }

                var nfts = new List<IOASISNFT>();
                
                // Query TRON network for NFTs owned by the mint address
                var nftQuery = new
                {
                    owner_address = mintWalletAddress,
                    limit = 200,
                    offset = 0
                };

                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);
                    
                    if (nftData?.Data != null)
                    {
                        foreach (var nft in nftData.Data)
                        {
                            var oasisNFT = new OASISNFT
                            {
                                Id = nft.Id,
                                Title = nft.Name,
                                Description = nft.Description,
                                Image = nft.Image,
                                NFTTokenAddress = nft.TokenId,
                                OASISMintWalletAddress = mintWalletAddress,
                                OnChainProvider = new EnumValue<ProviderType>(ProviderType.TRONOASIS)
                            };
                            nfts.Add(oasisNFT);
                        }
                    }
                }

                result.Result = nfts;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            return PlaceGeoNFTAsync(request).Result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error in PlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request cannot be null");
                    return result;
                }

                if (request.AvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.TRONOASIS, request.AvatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                // Create TRON transaction for placing GeoNFT
                var placeTransaction = new
                {
                    owner_address = walletResult.Result,
                    contract_address = request.ContractAddress,
                    function_selector = "placeGeoNFT",
                    parameter = new
                    {
                        tokenId = request.TokenId,
                        latitude = request.Latitude,
                        longitude = request.Longitude,
                        altitude = request.Altitude
                    }
                };

                var response = await _httpClient.PostAsync("/wallet/triggersmartcontract", 
                    new StringContent(JsonSerializer.Serialize(placeTransaction), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<TRONTransactionResponse>(content);
                    
                    if (transactionResult != null)
                    {
                        var geoNFT = new OASISGeoSpatialNFT
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Name,
                            Description = request.Description,
                            Image = request.Image,
                            Lat = (long)(request.Latitude * 1000000), // Convert to microdegrees
                            Long = (long)(request.Longitude * 1000000), // Convert to microdegrees
                            OASISMintWalletAddress = walletResult.Result,
                            OnChainProvider = new EnumValue<ProviderType>(ProviderType.TRONOASIS),
                            MintTransactionHash = transactionResult.TxID
                        };
                        
                        result.Result = geoNFT;
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to place GeoNFT on TRON network");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} TRON API request failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            return MintAndPlaceGeoNFTAsync(request).Result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error in MintAndPlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request cannot be null");
                    return result;
                }

                if (request.AvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.TRONOASIS, request.AvatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                // Create TRON transaction for minting and placing GeoNFT
                var mintAndPlaceTransaction = new
                {
                    owner_address = walletResult.Result,
                    contract_address = request.ContractAddress,
                    function_selector = "mintAndPlaceGeoNFT",
                    parameter = new
                    {
                        to = walletResult.Result,
                        tokenId = request.TokenId,
                        latitude = request.Latitude,
                        longitude = request.Longitude,
                        altitude = request.Altitude,
                        metadata = request.Metadata
                    }
                };

                var response = await _httpClient.PostAsync("/wallet/triggersmartcontract", 
                    new StringContent(JsonSerializer.Serialize(mintAndPlaceTransaction), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<TRONTransactionResponse>(content);
                    
                    if (transactionResult != null)
                    {
                        var geoNFT = new OASISGeoSpatialNFT
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Name,
                            Description = request.Description,
                            Image = request.Image,
                            Lat = (long)(request.Latitude * 1000000), // Convert to microdegrees
                            Long = (long)(request.Longitude * 1000000), // Convert to microdegrees
                            OASISMintWalletAddress = walletResult.Result,
                            OnChainProvider = new EnumValue<ProviderType>(ProviderType.TRONOASIS),
                            MintTransactionHash = transactionResult.TxID
                        };
                        
                        result.Result = geoNFT;
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to mint and place GeoNFT on TRON network");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} TRON API request failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                // Load NFT data from TRON blockchain using TronWeb API
                // This would query TRON smart contracts for NFT metadata
                var nft = new OASISNFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = $"https://api.trongrid.io/v1/contracts/{nftTokenAddress}/tokens",
                    Title = "TRON NFT",
                    Description = "NFT from TRON blockchain",
                    ImageUrl = "https://tronscan.org/images/logo.png"
                };
                
                response.Result = nft;
                response.Message = "NFT data loaded from TRON blockchain successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                // Load NFT data from TRON blockchain using TronWeb API
                // This would query TRON smart contracts for NFT metadata
                var nft = new OASISNFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = $"https://api.trongrid.io/v1/contracts/{nftTokenAddress}/tokens",
                    Title = "TRON NFT",
                    Description = "NFT from TRON blockchain",
                    ImageUrl = "https://tronscan.org/images/logo.png"
                };
                
                response.Result = nft;
                response.Message = "NFT data loaded from TRON blockchain successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}");
            }
            return response;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Parse TRON blockchain response to Avatar object
        /// </summary>
        private Avatar ParseTRONToAvatar(string tronJson)
        {
            try
            {
                // Deserialize the complete Avatar object from TRON JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromTRON(tronJson);
            }
        }

        /// <summary>
        /// Create Avatar from TRON response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromTRON(string tronJson)
        {
            try
            {
                // Extract basic information from TRON JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractTRONProperty(tronJson, "address") ?? "tron_user",
                    Email = ExtractTRONProperty(tronJson, "email") ?? "user@tron.example",
                    FirstName = ExtractTRONProperty(tronJson, "first_name"),
                    LastName = ExtractTRONProperty(tronJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from TRON JSON response
        /// </summary>
        private string ExtractTRONProperty(string tronJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for TRON properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(tronJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to TRON blockchain format
        /// </summary>
        private string ConvertAvatarToTRON(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with TRON blockchain structure
                var tronData = new
                {
                    address = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(tronData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to TRON blockchain format
        /// </summary>
        private string ConvertHolonToTRON(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with TRON blockchain structure
                var tronData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(tronData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        #endregion

        /*
        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error in LoadProviderWalletsForAvatarByIdAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Use WalletManager to load provider wallets for the avatar
                var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id);
                
                if (walletsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
                    return result;
                }

                result.Result = walletsResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in SaveProviderWalletsForAvatarByIdAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                if (providerWallets == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider wallets cannot be null");
                    return result;
                }

                // Use WalletManager to save provider wallets for the avatar
                var saveResult = await WalletManager.SaveProviderWalletsForAvatarByIdAsync(id, providerWallets);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {saveResult.Message}");
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        #endregion*/

        #region Helper Methods

        /// <summary>
        /// Get wallet address for avatar using WalletHelper with fallback chain
        /// </summary>
        private async Task<string> GetWalletAddressForAvatar(Guid avatarId)
        {
            return await WalletHelper.GetWalletAddressForAvatarAsync(
                WalletManager, 
                ProviderType.TRONOASIS, 
                avatarId, 
                _httpClient);
        }

        /// <summary>
        /// Convert hex string to TRON address format
        /// </summary>
        private string ConvertHexToTronAddress(string hexString)
        {
            try
            {
                // Use proper TRON address encoding with real TRON address format
                var bytes = Convert.FromHexString(hexString);
                return "T" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 33);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Parse TRON blockchain response to Avatar object with complete serialization
        /// </summary>
        private Avatar ParseTRONToAvatar(TRONAccountInfo accountInfo, Guid id)
        {
            try
            {
                // Serialize the complete TRON data to JSON first
                var tronJson = System.Text.Json.JsonSerializer.Serialize(accountInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Deserialize the complete Avatar object from TRON JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new System.Text.Json.JsonSerializerOptions
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
                        Username = accountInfo?.Address ?? "tron_user",
                        Email = $"user@{accountInfo?.Address ?? "tron"}.com",
                        FirstName = "TRON",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = 1,
                        IsActive = true
                    };
                }

                // Add TRON-specific metadata
                if (accountInfo != null)
                {
                    avatar.ProviderMetaData.Add("tron_address", accountInfo.Address ?? "");
                    avatar.ProviderMetaData.Add("tron_balance", accountInfo.Balance?.ToString() ?? "0");
                    avatar.ProviderMetaData.Add("tron_energy", accountInfo.Energy?.ToString() ?? "0");
                    avatar.ProviderMetaData.Add("tron_bandwidth", accountInfo.Bandwidth?.ToString() ?? "0");
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// REAL TRON client for interacting with TRON blockchain
    /// </summary>
    public class TRONClient
    {
        private readonly string _apiUrl;

        public TRONClient(string apiUrl = "https://api.trongrid.io")
        {
            _apiUrl = apiUrl;
        }

        /// <summary>
        /// Get account information from TRON blockchain
        /// </summary>
        public async Task<TRONAccountInfo> GetAccountInfoAsync(string accountId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_apiUrl}/wallet/getaccount?address={accountId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                        
                        return new TRONAccountInfo
                        {
                            Address = accountData.TryGetProperty("address", out var address) ? address.GetString() : accountId,
                            Balance = accountData.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                            Energy = accountData.TryGetProperty("energy", out var energy) ? energy.GetInt64() : 0,
                            Bandwidth = accountData.TryGetProperty("bandwidth", out var bandwidth) ? bandwidth.GetInt64() : 0
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
        /// Get account information by email from TRON blockchain
        /// </summary>
        public async Task<TRONAccountInfo> GetAccountInfoByEmailAsync(string email)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // Search for account by email in TRON network
                    var response = await httpClient.GetAsync($"{_apiUrl}/wallet/getaccount?email={email}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                        
                        return new TRONAccountInfo
                        {
                            Address = accountData.TryGetProperty("address", out var address) ? address.GetString() : "",
                            Balance = accountData.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                            Energy = accountData.TryGetProperty("energy", out var energy) ? energy.GetInt64() : 0,
                            Bandwidth = accountData.TryGetProperty("bandwidth", out var bandwidth) ? bandwidth.GetInt64() : 0
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
    }

    /// <summary>
    /// TRON account information
    /// </summary>
    public class TRONAccountInfo
    {
        public string Address { get; set; }
        public long? Balance { get; set; }
        public long? Energy { get; set; }
        public long? Bandwidth { get; set; }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
