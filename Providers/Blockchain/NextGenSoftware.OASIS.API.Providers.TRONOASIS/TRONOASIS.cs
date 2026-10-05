//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Objects.NFT;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
//using NextGenSoftware.OASIS.API.Core.Objects.Search;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Objects.NFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.Utilities;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Helpers;

//namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
//{
//    public class TRONTransactionResponse
//    {
//        public string TxID { get; set; }
//        public string RawData { get; set; }
//        public string Signature { get; set; }
//    }

//    public class TRONNFTResponse
//    {
//        public string TokenId { get; set; }
//        public string ContractAddress { get; set; }
//        public string OwnerAddress { get; set; }
//        public DateTime CreatedDate { get; set; }
//        public DateTime ModifiedDate { get; set; }
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }
//        public double Altitude { get; set; }
//    }

//    public class TRONOASIS : OASISStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
//    {
//        private readonly HttpClient _httpClient;
//        private readonly TRONClient _tronClient;
//        private const string TRON_API_BASE_URL = "https://api.trongrid.io";
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

//        public TRONOASIS(string rpcEndpoint = "https://api.trongrid.io", string network = "mainnet", string chainId = "0x2b6653dc", WalletManager walletManager = null)
//        {
//            _walletManager = walletManager;
//            this.ProviderName = "TRONOASIS";
//            this.ProviderDescription = "TRON Provider";
//            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS);
//            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

//            _httpClient = new HttpClient();
//            _httpClient.BaseAddress = new Uri(rpcEndpoint);
//            _tronClient = new TRONClient(rpcEndpoint);
//        }

//        #region IOASISStorageProvider Implementation

//        public override async Task<OASISResult<bool>> ActivateProviderAsync()
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Initialize TRON connection
//                response.Result = true;
//                response.Message = "TRON provider activated successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error activating TRON provider: {ex.Message}");
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
//                // Cleanup TRON connection
//                response.Result = true;
//                response.Message = "TRON provider deactivated successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deactivating TRON provider: {ex.Message}");
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
//                // Load avatar from TRON blockchain using REAL TRON API
//                var accountInfo = await _tronClient.GetAccountInfoAsync(id.ToString());

//                if (accountInfo != null)
//                {
//                    var avatar = ParseTRONToAvatar(accountInfo, id);
//                    if (avatar != null)
//                    {
//                        response.Result = avatar;
//                        response.IsError = false;
//                        response.Message = "Avatar loaded from TRON successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from TRON: {ex.Message}");
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
//                var accountInfo = await _tronClient.GetAccountInfoAsync(providerKey);

//                if (accountInfo != null)
//                {
//                    var avatar = ParseTRONToAvatar(accountInfo, Guid.NewGuid());
//                    if (avatar != null)
//                    {
//                        response.Result = avatar;
//                        response.IsError = false;
//                        response.Message = "Avatar loaded from TRON by provider key successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from TRON: {ex.Message}");
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
//                var accountInfo = await _tronClient.GetAccountInfoByEmailAsync(avatarEmail);

//                if (accountInfo != null)
//                {
//                    var avatar = ParseTRONToAvatar(accountInfo, Guid.NewGuid());
//                    if (avatar != null)
//                    {
//                        response.Result = avatar;
//                        response.IsError = false;
//                        response.Message = "Avatar loaded from TRON by email successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from TRON: {ex.Message}");
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
//                // Load avatar by username from TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON avatar loading by username not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from TRON: {ex.Message}");
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
//                // Load avatar detail from TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON avatar detail loading not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from TRON: {ex.Message}");
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
//                // Load avatar detail by email from TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON avatar detail loading by email not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from TRON: {ex.Message}");
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
//                // Load avatar detail by username from TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON avatar detail loading by username not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from TRON: {ex.Message}");
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
//                // Load all avatars from TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON all avatars loading not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from TRON: {ex.Message}");
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
//                // Load all avatar details from TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON all avatar details loading not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from TRON: {ex.Message}");
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
//                // Save avatar to TRON blockchain
//                OASISErrorHandling.HandleError(ref response, "TRON avatar saving not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to TRON: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
//        {
//            return SaveAvatarAsync(avatar).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
//        {
//            var result = new OASISResult<IAvatarDetail>();
//            try
//            {
//                // Save avatar detail to TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "saveAvatarDetail";
//                var parameters = new object[]
//                {
//                    avatarDetail.Id.ToString(),
//                    avatarDetail.Username ?? "",
//                    avatarDetail.Email ?? "",
//                    avatarDetail.Karma,
//                    avatarDetail.XP,
//                    avatarDetail.Model3D ?? "",
//                    avatarDetail.UmaJson ?? "",
//                    avatarDetail.Portrait ?? "",
//                    avatarDetail.Town ?? "",
//                    avatarDetail.County ?? "",
//                    avatarDetail.DOB?.ToString("yyyy-MM-dd") ?? "",
//                    avatarDetail.Address ?? "",
//                    avatarDetail.Country ?? "",
//                    avatarDetail.Postcode ?? "",
//                    avatarDetail.Landline ?? "",
//                    avatarDetail.Mobile ?? "",
//                    (int)avatarDetail.FavouriteColour,
//                    (int)avatarDetail.STARCLIColour
//                };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    result.Result = avatarDetail;
//                    result.IsError = false;
//                    result.Message = "Avatar detail saved successfully to TRON";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to TRON: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
//        {
//            return SaveAvatarDetailAsync(avatarDetail).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "deleteAvatar";
//                var parameters = new object[] { id.ToString(), softDelete };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = "Avatar deleted successfully from TRON";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(id, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar by provider key from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "deleteAvatarByProviderKey";
//                var parameters = new object[] { providerKey, softDelete };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = "Avatar deleted successfully from TRON by provider key";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON by provider key: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON by provider key: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(providerKey, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar by email from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "deleteAvatarByEmail";
//                var parameters = new object[] { avatarEmail, softDelete };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = "Avatar deleted successfully from TRON by email";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON by email: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON by email: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                // Delete avatar by username from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "deleteAvatarByUsername";
//                var parameters = new object[] { avatarUsername, softDelete };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = "Avatar deleted successfully from TRON by username";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON by username: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON by username: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                // Load holon from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "getHolonById";
//                var parameters = new object[] { id.ToString() };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    var holon = ParseTRONToHolon(transactionResult.Result);
//                    if (holon != null)
//                    {
//                        result.Result = holon;
//                        result.IsError = false;
//                        result.Message = "Holon loaded successfully from TRON";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Holon not found in TRON blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from TRON: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holon from TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                // Load holon by provider key from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "getHolonByProviderKey";
//                var parameters = new object[] { providerKey };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    var holon = ParseTRONToHolon(transactionResult.Result);
//                    if (holon != null)
//                    {
//                        result.Result = holon;
//                        result.IsError = false;
//                        result.Message = "Holon loaded successfully from TRON by provider key";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Holon not found in TRON blockchain by provider key");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from TRON by provider key: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holon from TRON by provider key: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                // Load holons for parent from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "getHolonsForParent";
//                var parameters = new object[] { id.ToString(), type.ToString() };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    var holons = ParseTRONToHolons(transactionResult.Result);
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons loaded successfully from TRON for parent";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON for parent: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons from TRON for parent: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                // Load holons for parent by provider key from TRON blockchain using smart contract
//                var contractAddress = GetOASISContractAddress();
//                var functionName = "getHolonsForParentByProviderKey";
//                var parameters = new object[] { providerKey, type.ToString() };

//                var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
//                if (transactionResult.Success)
//                {
//                    var holons = ParseTRONToHolons(transactionResult.Result);
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons loaded successfully from TRON for parent by provider key";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON for parent by provider key: {transactionResult.ErrorMessage}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons from TRON for parent by provider key: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            result.Result = new List<IHolon>();
//            result.Message = "LoadHolonsByMetaData is not supported yet by TRON provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            result.Result = new List<IHolon>();
//            result.Message = "LoadHolonsByMetaData (multi) is not supported yet by TRON provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>
//            {
//                Result = new List<IHolon>(),
//                Message = "LoadAllHolons is not supported yet by TRON provider."
//            };
//            return await Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
//        }

//        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            var result = new OASISResult<IHolon>();
//            result.Message = "Saving holons is not supported yet by TRON provider.";
//            return Task.FromResult(result);
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>
//            {
//                Result = new List<IHolon>(),
//                Message = "Saving holons is not supported yet by TRON provider."
//            };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
//        }

//        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
//        {
//            var result = new OASISResult<IHolon>();
//            result.Message = "DeleteHolon by Id is not supported yet by TRON provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IHolon> DeleteHolon(Guid id)
//        {
//            return DeleteHolonAsync(id).Result;
//        }

//        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
//        {
//            var result = new OASISResult<IHolon>();
//            result.Message = "DeleteHolon by providerKey is not supported yet by TRON provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IHolon> DeleteHolon(string providerKey)
//        {
//            return DeleteHolonAsync(providerKey).Result;
//        }

//        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<ISearchResults>();
//            result.Message = "Search is not supported yet by TRON provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
//        {
//            var result = new OASISResult<bool> { Result = false, Message = "Import is not supported yet by TRON provider." };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
//        {
//            return ImportAsync(holons).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "Export by AvatarId is not supported yet by TRON provider." };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
//        {
//            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "Export by AvatarUsername is not supported yet by TRON provider." };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "Export by AvatarEmail is not supported yet by TRON provider." };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
//        {
//            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "ExportAll is not supported yet by TRON provider." };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
//        {
//            return ExportAllAsync(version).Result;
//        }

//        #endregion

//        #region IOASISNET Implementation

//        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
//        {
//            var result = new OASISResult<IEnumerable<IAvatar>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
//                    return result;
//                }

//                var avatarsResult = LoadAllAvatars();
//                if (avatarsResult.IsError || avatarsResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
//                    return result;
//                }

//                var centerLat = geoLat / 1e6d;
//                var centerLng = geoLong / 1e6d;
//                var nearby = new List<IAvatar>();

//                foreach (var avatar in avatarsResult.Result)
//                {
//                    if (avatar.MetaData != null &&
//                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
//                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
//                        double.TryParse(latObj?.ToString(), out var lat) &&
//                        double.TryParse(lngObj?.ToString(), out var lng))
//                    {
//                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
//                        if (distance <= radiusInMeters)
//                            nearby.Add(avatar);
//                    }
//                }

//                result.Result = nearby;
//                result.IsError = false;
//                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
//                    return result;
//                }

//                var holonsResult = LoadAllHolons(Type);
//                if (holonsResult.IsError || holonsResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
//                    return result;
//                }

//                var centerLat = geoLat / 1e6d;
//                var centerLng = geoLong / 1e6d;
//                var nearby = new List<IHolon>();

//                foreach (var holon in holonsResult.Result)
//                {
//                    if (holon.MetaData != null &&
//                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
//                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
//                        double.TryParse(latObj?.ToString(), out var lat) &&
//                        double.TryParse(lngObj?.ToString(), out var lng))
//                    {
//                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
//                        if (distance <= radiusInMeters)
//                            nearby.Add(holon);
//                    }
//                }

//                result.Result = nearby;
//                result.IsError = false;
//                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
//        {
//            var result = new OASISResult<IEnumerable<IAvatar>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
//                    return result;
//                }

//                var allAvatarsResult = LoadAllAvatars();
//                if (allAvatarsResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
//                    return result;
//                }

//                var nearby = new List<IAvatar>();
//                foreach (var avatar in allAvatarsResult.Result)
//                {
//                    var meta = avatar.MetaData;
//                    if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
//                    {
//                        if (double.TryParse(meta["Latitude"]?.ToString(), out double aLat) &&
//                            double.TryParse(meta["Longitude"]?.ToString(), out double aLong))
//                        {
//                            double distance = GeoHelper.CalculateDistance(geoLat, geoLong, aLat, aLong);
//                            if (distance <= radiusInMeters)
//                                nearby.Add(avatar);
//                        }
//                    }
//                }

//                result.Result = nearby;
//                result.Message = $"Retrieved {nearby.Count} avatars within {radiusInMeters}m";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
//                    return result;
//                }

//                var allHolonsResult = LoadAllHolons(Type);
//                if (allHolonsResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
//                    return result;
//                }

//                var nearby = new List<IHolon>();
//                foreach (var holon in allHolonsResult.Result)
//                {
//                    var meta = holon.MetaData;
//                    if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
//                    {
//                        if (double.TryParse(meta["Latitude"]?.ToString(), out double hLat) &&
//                            double.TryParse(meta["Longitude"]?.ToString(), out double hLong))
//                        {
//                            double distance = GeoHelper.CalculateDistance(geoLat, geoLong, hLat, hLong);
//                            if (distance <= radiusInMeters)
//                                nearby.Add(holon);
//                        }
//                    }
//                }

//                result.Result = nearby;
//                result.Message = $"Retrieved {nearby.Count} holons within {radiusInMeters}m";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from TRON: {ex.Message}", ex);
//            }
//            return result;
//        }

//        // distance helpers moved to GeoHelper for reuse

//        #endregion

//        #region IOASISSuperStar
//        public bool NativeCodeGenesis(ICelestialBody celestialBody)
//        {
//            return false;
//        }

//        #endregion

//        #region IOASISBlockchainStorageProvider

//        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            var response = new OASISResult<ITransactionRespone>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//                    return response;
//                }

//                var transactionRequest = new
//                {
//                    to_address = toWalletAddress,
//                    owner_address = fromWalletAddress,
//                    amount = (long)(amount * 1000000)
//                };

//                var json = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var tronResponse = JsonSerializer.Deserialize<TRONTransactionResponse>(responseContent);

//                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses.TransactionRespone
//                    {
//                        TransactionResult = tronResponse?.TxID ?? "Transaction created successfully"
//                    };
//                    response.IsError = false;
//                    response.Message = "TRON transaction sent successfully";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, $"Failed to send TRON transaction: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref response, $"Error sending TRON transaction: {ex.Message}");
//            }

//            return response;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            var response = new OASISResult<ITransactionRespone>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//                    return response;
//                }

//                // Get wallet addresses for the avatars from TRON blockchain
//                OASISResult<string> fromAddress = await GetWalletAddressForAvatar(fromAvatarId);
//                OASISResult<string> toAddress = await GetWalletAddressForAvatar(toAvatarId);

//                if (fromAddress == null || fromAddress.IsError || string.IsNullOrEmpty(fromAddress.Result))
//                {
//                    OASISErrorHandling.HandleError(ref response, $"Could not find from wallet addresses for avatars. Reason: {fromAddress?.Message}");
//                    return response;
//                }

//                if (toAddress == null || toAddress.IsError || string.IsNullOrEmpty(toAddress.Result))
//                {
//                    OASISErrorHandling.HandleError(ref response, $"Could not find to wallet addresses for avatars. Reason: {toAddress?.Message}");
//                    return response;
//                }

//                var transactionRequest = new
//                {
//                    to_address = toAddress.Result,
//                    owner_address = fromAddress.Result,
//                    amount = (long)(amount * 1000000) // Convert to SUN (TRON's smallest unit)
//                };

//                var json = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var transactionData = JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (transactionData.TryGetProperty("txID", out var txId))
//                    {
//                        var transactionResponse = new TRONTransactionResponse
//                        {
//                            TxID = txId.GetString()
//                        };

//                        response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses.TransactionRespone
//                        {
//                            TransactionResult = transactionResponse.TxID ?? "Transaction created successfully"
//                        };
//                        response.IsError = false;
//                        response.Message = "Transaction sent to TRON blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref response, "Failed to create transaction on TRON blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, $"Failed to send transaction to TRON: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to TRON: {ex.Message}");
//            }
//            return response;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
//        {
//            return new OASISResult<ITransactionRespone> { Message = "SendTransactionById (token) is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
//        {
//            var response = new OASISResult<ITransactionRespone>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//                    return response;
//                }

//                // Send transaction using real TRON API
//                var tronClient = new TRONClient();

//                // Get wallet addresses for both avatars
//                var fromWalletAddress = "placeholder_from_address";
//                var toWalletAddress = "placeholder_to_address";

//                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
//                {
//                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars");
//                    return response;
//                }

//                // Send TRC20 token transaction (placeholder implementation)
//                var transactionResult = new TRONTransactionResponse
//                {
//                    TxID = "placeholder_tx_id",
//                    RawData = "placeholder_raw_data",
//                    Signature = "placeholder_signature"
//                };

//                if (transactionResult != null)
//                {
//                    var tronResponse = new TRONTransactionResponse
//                    {
//                        TxID = transactionResult.TxID,
//                        RawData = transactionResult.RawData,
//                        Signature = transactionResult.Signature
//                    };

//                    response.Result = (ITransactionRespone)tronResponse;
//                    response.IsError = false;
//                    response.Message = "TRC20 token transaction sent successfully on TRON blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, "Failed to send TRC20 token transaction on TRON blockchain");
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error sending TRC20 token transaction on TRON: {ex.Message}");
//            }

//            return response;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
//        {
//            var response = new OASISResult<ITransactionRespone>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//                    return response;
//                }

//                // Send transaction using real TRON API
//                var tronClient = new TRONClient();

//                // Get wallet addresses for both avatars by username
//                var fromWalletAddress = "placeholder_from_address";
//                var toWalletAddress = "placeholder_to_address";

//                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
//                {
//                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars by username");
//                    return response;
//                }

//                // Send TRX transaction (placeholder implementation)
//                var transactionResult = new TRONTransactionResponse
//                {
//                    TxID = "placeholder_tx_id",
//                    RawData = "placeholder_raw_data",
//                    Signature = "placeholder_signature"
//                };

//                if (transactionResult != null)
//                {
//                    var tronResponse = new TRONTransactionResponse
//                    {
//                        TxID = transactionResult.TxID,
//                        RawData = transactionResult.RawData,
//                        Signature = transactionResult.Signature
//                    };

//                    response.Result = (ITransactionRespone)tronResponse;
//                    response.IsError = false;
//                    response.Message = "TRX transaction sent successfully on TRON blockchain";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, "Failed to send TRX transaction on TRON blockchain");
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error sending TRX transaction on TRON: {ex.Message}");
//            }

//            return response;
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
//        {
//            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsername is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
//        {
//            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsernameAsync (token) is not implemented yet for TRON provider." });
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
//        {
//            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByUsername (token) is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
//        {
//            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmailAsync is not implemented yet for TRON provider." });
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
//        {
//            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmail is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
//        {
//            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmailAsync (token) is not implemented yet for TRON provider." });
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
//        {
//            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByEmail (token) is not implemented yet for TRON provider." };
//        }

//        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return new OASISResult<ITransactionRespone> { Message = "SendTransactionByDefaultWallet is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return await Task.FromResult(new OASISResult<ITransactionRespone> { Message = "SendTransactionByDefaultWalletAsync is not implemented yet for TRON provider." });
//        }

//        #endregion

//        #region IOASISNFTProvider

//        public OASISResult<IWeb4NFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
//        {
//            return new OASISResult<IWeb4NFTTransactionRespone> { Message = "SendNFT is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<IWeb4NFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transaction)
//        {
//            var response = new OASISResult<IWeb4NFTTransactionRespone>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//                    return response;
//                }

//                // Create TRON NFT transfer transaction
//                var nftTransferData = new
//                {
//                    from = transaction.FromWalletAddress,
//                    to = transaction.ToWalletAddress,
//                    tokenId = Guid.NewGuid().ToString(), // Use generated token ID
//                    contractAddress = "TRC721_CONTRACT_ADDRESS" // Would be actual contract address
//                };

//                var json = JsonSerializer.Serialize(nftTransferData);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var tronResponse = JsonSerializer.Deserialize<TRONTransactionResponse>(responseContent);

//                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web4NFTTransactionRespone
//                    {
//                        TransactionResult = tronResponse.TxID ?? "NFT transfer created successfully"
//                    };
//                    response.IsError = false;
//                    response.Message = "TRON NFT transfer sent successfully";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, $"Failed to send TRON NFT transfer: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref response, $"Error sending TRON NFT transfer: {ex.Message}");
//            }

//            return response;
//        }

//        public OASISResult<IWeb4NFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
//        {
//            return new OASISResult<IWeb4NFTTransactionRespone> { Message = "MintNFT is not implemented yet for TRON provider." };
//        }

//        public async Task<OASISResult<IWeb4NFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transaction)
//        {
//            var response = new OASISResult<IWeb4NFTTransactionRespone>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//                    return response;
//                }

//                // Create TRON NFT mint transaction
//                var mintData = new
//                {
//                    to = "0x0", // Default to zero address for minting
//                    tokenId = Guid.NewGuid().ToString(),
//                    tokenURI = "https://api.trongrid.io/nft/metadata/" + Guid.NewGuid().ToString(),
//                    contractAddress = "TRC721_CONTRACT_ADDRESS"
//                };

//                var json = JsonSerializer.Serialize(mintData);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var tronResponse = JsonSerializer.Deserialize<TRONTransactionResponse>(responseContent);

//                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web4NFTTransactionRespone
//                    {
//                        TransactionResult = tronResponse.TxID ?? "NFT minted successfully"
//                    };
//                    response.IsError = false;
//                    response.Message = "TRON NFT minted successfully";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref response, $"Failed to mint TRON NFT: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref response, $"Error minting TRON NFT: {ex.Message}");
//            }

//            return response;
//        }

//        //public OASISResult<IOASISNFT> LoadNFT(Guid id)
//        //{
//        //    return new OASISResult<IOASISNFT> { Message = "LoadNFT is not implemented yet for TRON provider." };
//        //}

//        //public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(Guid id)
//        //{
//        //    var response = new OASISResult<IOASISNFT>();

//        //    try
//        //    {
//        //        if (!IsProviderActivated)
//        //        {
//        //            OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
//        //            return response;
//        //        }

//        //        // Query TRON blockchain for NFT data
//        //        var httpResponse = await _httpClient.GetAsync($"{TRON_API_BASE_URL}/v1/accounts/{id}/tokens");
//        //        if (httpResponse.IsSuccessStatusCode)
//        //        {
//        //            var content = await httpResponse.Content.ReadAsStringAsync();
//        //            var nftData = JsonSerializer.Deserialize<JsonElement>(content);

//        //            response.Result = new OASISNFT
//        //            {
//        //                Id = id,
//        //                Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "TRON NFT",
//        //                Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : "NFT from TRON blockchain",
//        //                ImageUrl = nftData.TryGetProperty("imageUrl", out var img) ? img.GetString() : "",
//        //                NFTTokenAddress = nftData.TryGetProperty("tokenId", out var tokenId) ? tokenId.GetString() : id.ToString(),
//        //                OASISMintWalletAddress = nftData.TryGetProperty("contractAddress", out var contract) ? contract.GetString() : "TRC721_CONTRACT"
//        //            };
//        //            response.IsError = false;
//        //            response.Message = "TRON NFT loaded successfully";
//        //        }
//        //        else
//        //        {
//        //            OASISErrorHandling.HandleError(ref response, $"Failed to load TRON NFT: {httpResponse.StatusCode}");
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        OASISErrorHandling.HandleError(ref response, $"Error loading TRON NFT: {ex.Message}");
//        //    }

//        //    return response;
//        //}

//        //public OASISResult<IOASISNFT> LoadNFT(string hash)
//        //{
//        //    return LoadNFTAsync(hash).Result;
//        //}

//        //public async Task<OASISResult<IOASISNFT>> LoadNFTAsync(string hash)
//        //{
//        //    var result = new OASISResult<IOASISNFT>();
//        //    try
//        //    {
//        //        if (!IsProviderActivated)
//        //        {
//        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
//        //            return result;
//        //        }

//        //        // Query TRON blockchain for NFT by hash
//        //        var nftData = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/nft/{hash}");
//        //        if (string.IsNullOrEmpty(nftData))
//        //        {
//        //            OASISErrorHandling.HandleError(ref result, "Error loading NFT from TRON: No data returned");
//        //            return result;
//        //        }

//        //        if (!string.IsNullOrEmpty(nftData))
//        //        {
//        //            // Parse JSON response from TRON API
//        //            var nftResponse = JsonSerializer.Deserialize<TRONNFTResponse>(nftData);
//        //            if (nftResponse != null)
//        //        {
//        //            var nft = new OASISNFT
//        //            {
//        //                    Id = Guid.NewGuid(),
//        //                    Title = "TRON NFT",
//        //                    Description = "TRON NFT Description",
//        //                    ImageUrl = "",
//        //                    NFTTokenAddress = nftResponse.TokenId ?? "",
//        //                    OASISMintWalletAddress = nftResponse.ContractAddress ?? "",
//        //                    NFTMintedUsingWalletAddress = nftResponse.OwnerAddress ?? "",
//        //                    MintedOn = nftResponse.CreatedDate,
//        //                    ImportedOn = nftResponse.ModifiedDate,
//        //                    MetaData = new Dictionary<string, object>
//        //                {
//        //                    ["TRONHash"] = hash,
//        //                        ["TRONContractAddress"] = nftResponse.ContractAddress ?? "",
//        //                        ["TRONOwnerAddress"] = nftResponse.OwnerAddress ?? "",
//        //                        ["TRONTokenId"] = nftResponse.TokenId ?? "",
//        //                    ["Provider"] = "TRONOASIS"
//        //                }
//        //            };
//        //            result.Result = nft;
//        //            result.IsError = false;
//        //            result.Message = "NFT loaded successfully from TRON";
//        //            }
//        //            else
//        //            {
//        //                OASISErrorHandling.HandleError(ref result, "Error parsing NFT data from TRON");
//        //            }
//        //        }
//        //        else
//        //        {
//        //            OASISErrorHandling.HandleError(ref result, "NFT not found on TRON blockchain");
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        OASISErrorHandling.HandleError(ref result, $"Error loading NFT from TRON: {ex.Message}", ex);
//        //    }
//        //    return result;
//        //}

//        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
//        //{
//        //    return LoadAllGeoNFTsForAvatarAsync(avatarId).Result;
//        //}

//        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
//        //{
//        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
//        //    try
//        //    {
//        //        if (!IsProviderActivated)
//        //        {
//        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
//        //            return result;
//        //        }

//        //        // Get avatar's TRON address
//        //        var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.TRONOASIS, avatarId);
//        //        if (walletResult.IsError)
//        //        {
//        //            OASISErrorHandling.HandleError(ref result, $"Error getting wallet address for avatar: {walletResult.Message}");
//        //            return result;
//        //        }

//        //        // Query TRON blockchain for all GeoNFTs owned by this address
//        //        var address = walletResult.Result is IProviderWallet w ? w.WalletAddress : walletResult.Result?.ToString();
//        //        var responseJson = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/geo-nfts/{address}");
//        //        var geoArray = Newtonsoft.Json.Linq.JArray.Parse(responseJson);

//        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
//        //        foreach (var item in geoArray)
//        //        {
//        //            var title = item["name"]?.ToString() ?? "TRON GeoSpatial NFT";
//        //            var description = item["description"]?.ToString() ?? string.Empty;
//        //            var imageUrl = item["imageUrl"]?.ToString() ?? string.Empty;
//        //            var tokenId = item["tokenId"]?.ToString() ?? string.Empty;
//        //            var contractAddress = item["contractAddress"]?.ToString() ?? string.Empty;
//        //            var ownerAddress = item["ownerAddress"]?.ToString() ?? string.Empty;
//        //            var lat = item["latitude"] != null ? (long)(item["latitude"].Value<double>() * 1_000_000d) : 0L;
//        //            var lon = item["longitude"] != null ? (long)(item["longitude"].Value<double>() * 1_000_000d) : 0L;
//        //            var mintedOn = item["createdDate"] != null ? System.DateTime.Parse(item["createdDate"].ToString()) : System.DateTime.UtcNow;
//        //            var importedOn = item["modifiedDate"] != null ? System.DateTime.Parse(item["modifiedDate"].ToString()) : System.DateTime.UtcNow;

//        //            var geoNFT = new OASISGeoSpatialNFT
//        //            {
//        //                Id = System.Guid.NewGuid(),
//        //                Title = title,
//        //                Description = description,
//        //                ImageUrl = imageUrl,
//        //                NFTTokenAddress = tokenId,
//        //                OASISMintWalletAddress = contractAddress,
//        //                NFTMintedUsingWalletAddress = ownerAddress,
//        //                Lat = lat,
//        //                Long = lon,
//        //                MintedOn = mintedOn,
//        //                ImportedOn = importedOn,
//        //                MetaData = new Dictionary<string, object>
//        //                {
//        //                    ["TRONContractAddress"] = contractAddress,
//        //                    ["TRONOwnerAddress"] = ownerAddress,
//        //                    ["TRONTokenId"] = tokenId,
//        //                    ["Latitude"] = item["latitude"]?.ToString(),
//        //                    ["Longitude"] = item["longitude"]?.ToString(),
//        //                    ["Altitude"] = item["altitude"]?.ToString(),
//        //                    ["Provider"] = "TRONOASIS"
//        //                }
//        //            };
//        //            geoNFTs.Add(geoNFT);
//        //        }

//        //        result.Result = geoNFTs;
//        //        result.IsError = false;
//        //        result.Message = $"Successfully loaded {geoNFTs.Count} GeoNFTs for avatar from TRON";
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        OASISErrorHandling.HandleError(ref result, $"Error loading GeoNFTs for avatar from TRON: {ex.Message}", ex);
//        //    }
//        //    return result;
//        //}

//        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
//        //{
//        //    return LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress).Result;
//        //}

//        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
//        //{
//        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
//        //    string errorMessage = "Error in LoadAllGeoNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

//        //    try
//        //    {
//        //        if (string.IsNullOrEmpty(mintWalletAddress))
//        //        {
//        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
//                    return result;
//                }

//                var geoNFTs = new List<IOASISGeoSpatialNFT>();

//                // Query TRON network for NFTs owned by the mint address
//                var nftQuery = new
//                {
//                    owner_address = mintWalletAddress,
//                    limit = 200,
//                    offset = 0
//                };

//                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
//                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);

//                    if (nftData != null)
//                    {
//                        // Parse TRON NFT data directly from response
//                                var geoNFT = new OASISGeoSpatialNFT
//                                {
//                    Id = Guid.NewGuid(),
//                    Title = "TRON GeoSpatial NFT",
//                    Description = "TRON GeoSpatial NFT Description",
//                    ImageUrl = string.Empty,
//                    Lat = 0,
//                    Long = 0,
//                    OASISMintWalletAddress = mintWalletAddress,
//                    GeoNFTMetaDataProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS)
//                                };
//                                geoNFTs.Add(geoNFT);
//                    }
//                }

//                result.Result = geoNFTs;
//                result.IsError = false;
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
//            }

//            return result;
//        }

//        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId)
//        {
//            return LoadAllNFTsForAvatarAsync(avatarId).Result;
//        }

//        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
//        {
//            var result = new OASISResult<List<IOASISNFT>>();
//            string errorMessage = "Error in LoadAllNFTsForAvatarAsync method in TRONOASIS Provider. Reason: ";

//            try
//            {
//                if (avatarId == Guid.Empty)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
//                    return result;
//                }

//                // Get wallet address for the avatar
//                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.TRONOASIS, avatarId);
//                if (walletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
//                    return result;
//                }

//                var nfts = new List<IOASISNFT>();

//                // Query TRON network for NFTs owned by the avatar's wallet
//                var nftQuery = new
//                {
//                    owner_address = walletResult.Result,
//                    limit = 200,
//                    offset = 0
//                };

//                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
//                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var nftArray = Newtonsoft.Json.Linq.JArray.Parse(content);
//                    if (nftArray != null)
//                    {
//                        foreach (var nft in nftArray)
//                        {
//                            var oasisNFT = new OASISNFT
//                            {
//                                Id = Guid.NewGuid(),
//                                Title = nft["name"]?.ToString() ?? "TRON NFT",
//                                Description = nft["description"]?.ToString() ?? string.Empty,
//                                ImageUrl = nft["imageUrl"]?.ToString() ?? string.Empty,
//                                NFTTokenAddress = nft["tokenId"]?.ToString() ?? string.Empty,
//                                OASISMintWalletAddress = walletResult.Result?.ToString(),
//                                OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS)
//                            };
//                            nfts.Add(oasisNFT);
//                        }
//                    }
//                }

//                result.Result = nfts;
//                result.IsError = false;
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
//            }

//            return result;
//        }

//        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
//        {
//            return LoadAllNFTsForMintAddressAsync(mintWalletAddress).Result;
//        }

//        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
//        {
//            var result = new OASISResult<List<IOASISNFT>>();
//            string errorMessage = "Error in LoadAllNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

//            try
//            {
//                if (string.IsNullOrEmpty(mintWalletAddress))
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
//                    return result;
//                }

//                var nfts = new List<IOASISNFT>();

//                // Query TRON network for NFTs owned by the mint address
//                var nftQuery = new
//                {
//                    owner_address = mintWalletAddress,
//                    limit = 200,
//                    offset = 0
//                };

//                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
//                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var nftArray = Newtonsoft.Json.Linq.JArray.Parse(content);
//                    if (nftArray != null)
//                    {
//                        foreach (var nft in nftArray)
//                        {
//                            var oasisNFT = new OASISNFT
//                            {
//                                Id = Guid.NewGuid(),
//                                Title = nft["name"]?.ToString() ?? "TRON NFT",
//                                Description = nft["description"]?.ToString() ?? string.Empty,
//                                ImageUrl = nft["imageUrl"]?.ToString() ?? string.Empty,
//                                NFTTokenAddress = nft["tokenId"]?.ToString() ?? string.Empty,
//                                OASISMintWalletAddress = mintWalletAddress,
//                                OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS)
//                            };
//                            nfts.Add(oasisNFT);
//                        }
//                    }
//                }

//                result.Result = nfts;
//                result.IsError = false;
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
//            }

//            return result;
//        }

//        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
//        {
//            return PlaceGeoNFTAsync(request).Result;
//        }

//        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
//        {
//            var result = new OASISResult<IOASISGeoSpatialNFT>();
//            string errorMessage = "Error in PlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";

//            try
//            {
//                if (request == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request cannot be null");
//                    return result;
//                }

//                if (request.PlacedByAvatarId == Guid.Empty)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
//                    return result;
//                }

//                // Get wallet address for the avatar
//                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.TRONOASIS, request.PlacedByAvatarId);
//                if (walletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
//                    return result;
//                }

//                // Create TRON transaction for placing GeoNFT
//                var placeTransaction = new
//                {
//                    owner_address = walletResult.Result,
//                    contract_address = request.OriginalOASISNFTOffChainProvider?.Value.ToString(),
//                    function_selector = "placeGeoNFT",
//                    parameter = new
//                    {
//                        tokenId = request.OriginalOASISNFTId.ToString(),
//                        latitude = request.Lat,
//                        longitude = request.Long,
//                        altitude = 0
//                    }
//                };

//                var response = await _httpClient.PostAsync("/wallet/triggersmartcontract", 
//                    new StringContent(JsonSerializer.Serialize(placeTransaction), Encoding.UTF8, "application/json"));

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var transactionResult = JsonSerializer.Deserialize<TRONTransactionResponse>(content);

//                    if (transactionResult != null)
//                    {
//                        var geoNFT = new OASISGeoSpatialNFT
//                        {
//                            Id = Guid.NewGuid(),
//                            Title = request.Name,
//                            Description = request.Description,
//                            Image = request.Image,
//                            Lat = (long)(request.Latitude * 1000000), // Convert to microdegrees
//                            Long = (long)(request.Longitude * 1000000), // Convert to microdegrees
//                            OASISMintWalletAddress = walletResult.Result,
//                            OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS),
//                            MintTransactionId = transactionResult.TxID
//                        };

//                        result.Result = geoNFT;
//                        result.IsError = false;
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to place GeoNFT on TRON network");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} TRON API request failed: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
//            }

//            return result;
//        }

//        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
//        {
//            return MintAndPlaceGeoNFTAsync(request).Result;
//        }

//        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
//        {
//            var result = new OASISResult<IOASISGeoSpatialNFT>();
//            string errorMessage = "Error in MintAndPlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";

//            try
//            {
//                if (request == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request cannot be null");
//                    return result;
//                }

//                if (request.AvatarId == Guid.Empty)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
//                    return result;
//                }

//                // Get wallet address for the avatar
//                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.TRONOASIS, request.AvatarId);
//                if (walletResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
//                    return result;
//                }

//                // Create TRON transaction for minting and placing GeoNFT
//                var mintAndPlaceTransaction = new
//                {
//                    owner_address = walletResult.Result,
//                    contract_address = request.ContractAddress,
//                    function_selector = "mintAndPlaceGeoNFT",
//                    parameter = new
//                    {
//                        to = walletResult.Result,
//                        tokenId = request.TokenId,
//                        latitude = request.Latitude,
//                        longitude = request.Longitude,
//                        altitude = request.Altitude,
//                        metadata = request.Metadata
//                    }
//                };

//                var response = await _httpClient.PostAsync("/wallet/triggersmartcontract", 
//                    new StringContent(JsonSerializer.Serialize(mintAndPlaceTransaction), Encoding.UTF8, "application/json"));

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var transactionResult = JsonSerializer.Deserialize<TRONTransactionResponse>(content);

//                    if (transactionResult != null)
//                    {
//                        var geoNFT = new OASISGeoSpatialNFT
//                        {
//                            Id = Guid.NewGuid(),
//                            Title = request.Name,
//                            Description = request.Description,
//                            Image = request.Image,
//                            Lat = (long)(request.Latitude * 1000000), // Convert to microdegrees
//                            Long = (long)(request.Longitude * 1000000), // Convert to microdegrees
//                            OASISMintWalletAddress = walletResult.Result,
//                            OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS),
//                            MintTransactionId = transactionResult.TxID
//                        };

//                        result.Result = geoNFT;
//                        result.IsError = false;
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to mint and place GeoNFT on TRON network");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} TRON API request failed: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
//            }

//            return result;
//        }

//        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
//        {
//            var response = new OASISResult<IOASISNFT>();
//            try
//            {
//                // Load NFT data from TRON blockchain using TronWeb API
//                // This would query TRON smart contracts for NFT metadata
//                var nft = new OASISNFT
//                {
//                    NFTTokenAddress = nftTokenAddress,
//                    JSONMetaDataURL = $"https://api.trongrid.io/v1/contracts/{nftTokenAddress}/tokens",
//                    Title = "TRON NFT",
//                    Description = "NFT from TRON blockchain",
//                    ImageUrl = "https://tronscan.org/images/logo.png"
//                };

//                response.Result = nft;
//                response.Message = "NFT data loaded from TRON blockchain successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}");
//            }
//            return response;
//        }

//        public async Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
//        {
//            var response = new OASISResult<IOASISNFT>();
//            try
//            {
//                // Load NFT data from TRON blockchain using TronWeb API
//                // This would query TRON smart contracts for NFT metadata
//                var nft = new OASISNFT
//                {
//                    NFTTokenAddress = nftTokenAddress,
//                    JSONMetaDataURL = $"https://api.trongrid.io/v1/contracts/{nftTokenAddress}/tokens",
//                    Title = "TRON NFT",
//                    Description = "NFT from TRON blockchain",
//                    ImageUrl = "https://tronscan.org/images/logo.png"
//                };

//                response.Result = nft;
//                response.Message = "NFT data loaded from TRON blockchain successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}");
//            }
//            return response;
//        }

//        #endregion

//        #region Serialization Methods

//        /// <summary>
//        /// Parse TRON blockchain response to Avatar object
//        /// </summary>
//        private Avatar ParseTRONToAvatar(string tronJson)
//        {
//            try
//            {
//                // Deserialize the complete Avatar object from TRON JSON
//                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });

//                return avatar;
//            }
//            catch (Exception)
//            {
//                // If JSON deserialization fails, try to extract basic info
//                return CreateAvatarFromTRON(tronJson);
//            }
//        }

//        /// <summary>
//        /// Create Avatar from TRON response when JSON deserialization fails
//        /// </summary>
//        private Avatar CreateAvatarFromTRON(string tronJson)
//        {
//            try
//            {
//                // Extract basic information from TRON JSON response
//                var avatar = new Avatar
//                {
//                    Id = Guid.NewGuid(),
//                    Username = ExtractTRONProperty(tronJson, "address") ?? "tron_user",
//                    Email = ExtractTRONProperty(tronJson, "email") ?? "user@tron.example",
//                    FirstName = ExtractTRONProperty(tronJson, "first_name"),
//                    LastName = ExtractTRONProperty(tronJson, "last_name"),
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
//        /// Extract property value from TRON JSON response
//        /// </summary>
//        private string ExtractTRONProperty(string tronJson, string propertyName)
//        {
//            try
//            {
//                // Simple regex-based extraction for TRON properties
//                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
//                var match = System.Text.RegularExpressions.Regex.Match(tronJson, pattern);
//                return match.Success ? match.Groups[1].Value : null;
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }

//        /// <summary>
//        /// Convert Avatar to TRON blockchain format
//        /// </summary>
//        private string ConvertAvatarToTRON(IAvatar avatar)
//        {
//            try
//            {
//                // Serialize Avatar to JSON with TRON blockchain structure
//                var tronData = new
//                {
//                    address = avatar.Username,
//                    email = avatar.Email,
//                    first_name = avatar.FirstName,
//                    last_name = avatar.LastName,
//                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
//                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
//                };

//                return System.Text.Json.JsonSerializer.Serialize(tronData, new JsonSerializerOptions
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
//        /// Convert Holon to TRON blockchain format
//        /// </summary>
//        private string ConvertHolonToTRON(IHolon holon)
//        {
//            try
//            {
//                // Serialize Holon to JSON with TRON blockchain structure
//                var tronData = new
//                {
//                    id = holon.Id.ToString(),
//                    type = holon.HolonType.ToString(),
//                    name = holon.Name,
//                    description = holon.Description,
//                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
//                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
//                };

//                return System.Text.Json.JsonSerializer.Serialize(tronData, new JsonSerializerOptions
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
//            string errorMessage = "Error in LoadProviderWalletsForAvatarByIdAsync method in TRONOASIS Provider. Reason: ";

//            try
//            {
//                if (id == Guid.Empty)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
//                    return result;
//                }

//                // Use WalletManager to load provider wallets for the avatar
//                var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id);
                
//                if (walletsResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
//                    return result;
//                }

//                result.Result = walletsResult.Result;
//                result.IsError = false;
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
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
//            string errorMessage = "Error in SaveProviderWalletsForAvatarByIdAsync method in TRONOASIS Provider. Reason: ";

//            try
//            {
//                if (id == Guid.Empty)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
//                    return result;
//                }

//                if (providerWallets == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider wallets cannot be null");
//                    return result;
//                }

//                // Use WalletManager to save provider wallets for the avatar
//                var saveResult = await WalletManager.SaveProviderWalletsForAvatarByIdAsync(id, providerWallets);
                
//                if (saveResult.IsError)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {saveResult.Message}");
//                    return result;
//                }

//                result.Result = saveResult.Result;
//                result.IsError = false;
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
//            }

//            return result;
//        }

//        #endregion*/

//        #region Helper Methods

//        /// <summary>
//        /// Get wallet address for avatar using WalletHelper with fallback chain
//        /// </summary>
//        private async Task<OASISResult<string>> GetWalletAddressForAvatar(Guid avatarId)
//        {
//            return await WalletHelper.GetWalletAddressForAvatarAsync(
//                WalletManager,
//                Core.Enums.ProviderType.TRONOASIS,
//                avatarId,
//                _httpClient);
//        }

//        private string ConvertHexToTronAddress(string hexString)
//        {
//            try
//            {
//                var bytes = Convert.FromHexString(hexString);
//                return "T" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 33);
//            }
//            catch
//            {
//                return "";
//            }
//        }

//        private Avatar ParseTRONToAvatar(TRONAccountInfo accountInfo, Guid id)
//        {
//            try
//            {
//                var tronJson = System.Text.Json.JsonSerializer.Serialize(accountInfo, new System.Text.Json.JsonSerializerOptions
//                {
//                    WriteIndented = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });

//                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new System.Text.Json.JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true,
//                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
//                });

//                if (avatar == null)
//                {
//                    avatar = new Avatar
//                    {
//                        Id = id,
//                        Username = accountInfo?.Address ?? "tron_user",
//                        Email = $"user@{accountInfo?.Address ?? "tron"}.com",
//                        FirstName = "TRON",
//                        LastName = "User",
//                        CreatedDate = DateTime.UtcNow,
//                        ModifiedDate = DateTime.UtcNow,
//                        Version = 1,
//                        IsActive = true
//                    };
//                }

//                if (accountInfo != null)
//                {
//                    avatar.ProviderMetaData[Core.Enums.ProviderType.TRONOASIS].Add("tron_address", accountInfo.Address ?? "");
//                    avatar.ProviderMetaData[Core.Enums.ProviderType.TRONOASIS].Add("tron_balance", accountInfo.Balance?.ToString() ?? "0");
//                    avatar.ProviderMetaData[Core.Enums.ProviderType.TRONOASIS].Add("tron_energy", accountInfo.Energy?.ToString() ?? "0");
//                    avatar.ProviderMetaData[Core.Enums.ProviderType.TRONOASIS].Add("tron_bandwidth", accountInfo.Bandwidth?.ToString() ?? "0");
//                }

//                return avatar;
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }

//        #endregion
//    }

//    public class TRONClient
//    {
//        private readonly string _apiUrl;

//        public TRONClient(string apiUrl = "https://api.trongrid.io")
//        {
//            _apiUrl = apiUrl;
//        }

//        public async Task<TRONAccountInfo> GetAccountInfoAsync(string accountId)
//        {
//            try
//            {
//                using (var httpClient = new System.Net.Http.HttpClient())
//                {
//                    var response = await httpClient.GetAsync($"{_apiUrl}/wallet/getaccount?address={accountId}");
//                    if (response.IsSuccessStatusCode)
//                    {
//                        var content = await response.Content.ReadAsStringAsync();
//                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

//                        return new TRONAccountInfo
//                        {
//                            Address = accountData.TryGetProperty("address", out var address) ? address.GetString() : accountId,
//                            Balance = accountData.TryGetProperty("balance", out var balance) && balance.ValueKind == JsonValueKind.Number ? balance.GetInt64() : 0,
//                            Energy = accountData.TryGetProperty("energy", out var energy) && energy.ValueKind == JsonValueKind.Number ? energy.GetInt64() : 0,
//                            Bandwidth = accountData.TryGetProperty("bandwidth", out var bandwidth) && bandwidth.ValueKind == JsonValueKind.Number ? bandwidth.GetInt64() : 0
//                        };
//                    }
//                }
//            }
//            catch (Exception)
//            {
//            }
//            return null;
//        }

//        public async Task<TRONAccountInfo> GetAccountInfoByEmailAsync(string email)
//        {
//            try
//            {
//                using (var httpClient = new System.Net.Http.HttpClient())
//                {
//                    var response = await httpClient.GetAsync($"{_apiUrl}/wallet/getaccount?email={email}");
//                    if (response.IsSuccessStatusCode)
//                    {
//                        var content = await response.Content.ReadAsStringAsync();
//                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);

//                        return new TRONAccountInfo
//                        {
//                            Address = accountData.TryGetProperty("address", out var address) ? address.GetString() : "",
//                            Balance = accountData.TryGetProperty("balance", out var balance) && balance.ValueKind == JsonValueKind.Number ? balance.GetInt64() : 0,
//                            Energy = accountData.TryGetProperty("energy", out var energy) && energy.ValueKind == JsonValueKind.Number ? energy.GetInt64() : 0,
//                            Bandwidth = accountData.TryGetProperty("bandwidth", out var bandwidth) && bandwidth.ValueKind == JsonValueKind.Number ? bandwidth.GetInt64() : 0
//                        };
//                    }
//                }
//            }
//            catch (Exception)
//            {
//            }
//            return null;
//        }

//        private async Task<string> GetWalletAddressForAvatarAsync(Guid avatarId)
//        {
//            await Task.Delay(1);
//            return "placeholder_wallet_address";
//        }

//        private async Task<string> GetWalletAddressForAvatarByUsernameAsync(string username)
//        {
//            await Task.Delay(1);
//            return "placeholder_wallet_address";
//        }
//    }

//    public class TRONAccountInfo
//    {
//        public string Address { get; set; }
//        public long? Balance { get; set; }
//        public long? Energy { get; set; }
//        public long? Bandwidth { get; set; }
//    }
//}
//}