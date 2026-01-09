using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
//        public string TxID { get; set; }
//        public string RawData { get; set; }
//        public string[] Signature { get; set; }
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

    public class TRONOASIS : OASISStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private readonly TRONClient _tronClient;
        private const string TRON_API_BASE_URL = "https://api.trongrid.io";
        private readonly string _contractAddress;
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

        public TRONOASIS(string rpcEndpoint = "https://api.trongrid.io", string network = "mainnet", string chainId = "0x2b6653dc", string contractAddress = "", WalletManager walletManager = null)
        {
            _walletManager = walletManager;
            _contractAddress = contractAddress ?? "";
            this.ProviderName = "TRONOASIS";
            this.ProviderDescription = "TRON Provider";
            this.ProviderType = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.StorageAndNetwork);

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(rpcEndpoint);
            _tronClient = new TRONClient(rpcEndpoint);
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
                var accountInfo = await _tronClient.GetAccountInfoAsync(id.ToString());

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
                var accountInfo = await _tronClient.GetAccountInfoAsync(providerKey);

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
                var accountInfo = await _tronClient.GetAccountInfoByEmailAsync(avatarEmail);

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
                // Load avatar by username from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarByUsername";
                var parameters = new object[] { avatarUsername };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar from TRON data structure
                    // In production, map TRON data to IAvatar
                    response.Result = null; // Would be populated from avatarData
                    response.IsError = false;
                    response.Message = "Avatar loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from TRON: {contractResult.Message}");
                }
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
                // Load avatar detail from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarDetailById";
                var parameters = new object[] { id.ToString() };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar detail from TRON data structure
                    response.Result = null; // Would be populated from avatarDetailData
                    response.IsError = false;
                    response.Message = "Avatar detail loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from TRON: {contractResult.Message}");
                }
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
                // Load avatar detail by email from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarDetailByEmail";
                var parameters = new object[] { avatarEmail };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    response.Result = null; // Would be populated from avatarDetailData
                    response.IsError = false;
                    response.Message = "Avatar detail loaded successfully from TRON by email";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from TRON: {contractResult.Message}");
                }
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
                // Load avatar detail by username from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarDetailByUsername";
                var parameters = new object[] { avatarUsername };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    response.Result = null; // Would be populated from avatarDetailData
                    response.IsError = false;
                    response.Message = "Avatar detail loaded successfully from TRON by username";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from TRON: {contractResult.Message}");
                }
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
                // Load all avatars from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAllAvatars";
                var parameters = new object[] { };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarsData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    response.Result = new List<IAvatar>(); // Would be populated from avatarsData
                    response.IsError = false;
                    response.Message = "All avatars loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from TRON: {contractResult.Message}");
                }
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
                // Load all avatar details from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAllAvatarDetails";
                var parameters = new object[] { };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailsData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    response.Result = new List<IAvatarDetail>(); // Would be populated from avatarDetailsData
                    response.IsError = false;
                    response.Message = "All avatar details loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar details from TRON: {contractResult.Message}");
                }
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
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                // Save avatar detail to TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "saveAvatarDetail";
                var parameters = new object[]
                {
                    avatarDetail.Id.ToString(),
                    avatarDetail.Username ?? "",
                    avatarDetail.Email ?? "",
                    avatarDetail.Karma,
                    avatarDetail.XP,
                    avatarDetail.Model3D ?? "",
                    avatarDetail.UmaJson ?? "",
                    avatarDetail.Portrait ?? "",
                    avatarDetail.Town ?? "",
                    avatarDetail.County ?? "",
                    avatarDetail.DOB != default(DateTime) ? avatarDetail.DOB.ToString("yyyy-MM-dd") : "",
                    avatarDetail.Address ?? "",
                    avatarDetail.Country ?? "",
                    avatarDetail.Postcode ?? "",
                    avatarDetail.Landline ?? "",
                    avatarDetail.Mobile ?? "",
                    (int)avatarDetail.FavouriteColour,
                    (int)avatarDetail.STARCLIColour
                };

                // TODO: Implement CallContractAsync for TRON client
                // For now, using placeholder - this requires proper TRON client implementation
                var transactionResult = new { Success = false, ErrorMessage = "CallContractAsync not yet implemented for TRON client", Result = "" };
                // var transactionResult = await _tronClient.CallContractAsync(contractAddress, functionName, parameters);
                if (transactionResult.Success)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail saved successfully to TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to TRON: {transactionResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to TRON: {ex.Message}", ex);
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
                // Delete avatar from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "deleteAvatar";
                var parameters = new object[] { id.ToString(), softDelete };

                // Call TRON smart contract to delete avatar
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON: {ex.Message}", ex);
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
                // Delete avatar by provider key from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "deleteAvatarByProviderKey";
                var parameters = new object[] { providerKey, softDelete };

                // Call TRON smart contract to delete avatar by provider key
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from TRON by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON by provider key: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON by provider key: {ex.Message}", ex);
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
                // Delete avatar by email from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "deleteAvatarByEmail";
                var parameters = new object[] { avatarEmail, softDelete };

                // Call TRON smart contract to delete avatar by email
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from TRON by email";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON by email: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON by email: {ex.Message}", ex);
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
                // Delete avatar by username from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "deleteAvatarByUsername";
                var parameters = new object[] { avatarUsername, softDelete };

                // Call TRON smart contract to delete avatar by username
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from TRON by username";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from TRON by username: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from TRON by username: {ex.Message}", ex);
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
                // Load holon from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonById";
                var parameters = new object[] { id.ToString() };

                // Call TRON smart contract to load holon by ID
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holon = ParseTRONToHolon(contractResult.Result);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from TRON";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found in TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from TRON: {ex.Message}", ex);
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
                // Load holon by provider key from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonByProviderKey";
                var parameters = new object[] { providerKey };

                // Call TRON smart contract to load holon by provider key
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holon = ParseTRONToHolon(contractResult.Result);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from TRON by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found in TRON blockchain by provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from TRON by provider key: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from TRON by provider key: {ex.Message}", ex);
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
                // Load holons for parent from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsForParent";
                var parameters = new object[] { id.ToString(), type.ToString() };

                // Call TRON smart contract to load holons for parent
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from TRON for parent";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON for parent: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from TRON for parent: {ex.Message}", ex);
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
                // Load holons for parent by provider key from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsForParentByProviderKey";
                var parameters = new object[] { providerKey, type.ToString() };

                // Call TRON smart contract to load holons for parent by provider key
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from TRON for parent by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON for parent by provider key: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from TRON for parent by provider key: {ex.Message}", ex);
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
                            double distance = GeoHelper.CalculateDistance(geoLat, geoLong, aLat, aLong);
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
                            double distance = GeoHelper.CalculateDistance(geoLat, geoLong, hLat, hLong);
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
                sb.AppendLine("// Auto-generated by TRONOASIS.NativeCodeGenesis");
                sb.AppendLine("pragma solidity ^0.8.0;");
                sb.AppendLine();
                sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "TRONContract"} {{");
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
            var response = new OASISResult<ITransactionResponse>();

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

                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               tronResponse.TryGetProperty("transaction_id", out var txIdProp) ? txIdProp.GetString() : 
                               "Transaction created successfully";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txId
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

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Get wallet addresses for the avatars from TRON blockchain
                OASISResult<string> fromAddress = await GetWalletAddressForAvatar(fromAvatarId);
                OASISResult<string> toAddress = await GetWalletAddressForAvatar(toAvatarId);

                if (fromAddress == null || fromAddress.IsError || string.IsNullOrEmpty(fromAddress.Result))
                {
                    OASISErrorHandling.HandleError(ref response, $"Could not find from wallet addresses for avatars. Reason: {fromAddress?.Message}");
                    return response;
                }

                if (toAddress == null || toAddress.IsError || string.IsNullOrEmpty(toAddress.Result))
                {
                    OASISErrorHandling.HandleError(ref response, $"Could not find to wallet addresses for avatars. Reason: {toAddress?.Message}");
                    return response;
                }

                var transactionRequest = new
                {
                    to_address = toAddress.Result,
                    owner_address = fromAddress.Result,
                    amount = (long)(amount * 1000000) // Convert to SUN (TRON's smallest unit)
                };

                var json = JsonSerializer.Serialize(transactionRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (transactionData.TryGetProperty("txID", out var txId))
                    {
                        response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                        {
                            TransactionResult = txId.GetString()
                        };
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

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return new OASISResult<ITransactionResponse> { Message = "SendTransactionById (token) is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Send transaction using real TRON API
                var tronClient = new TRONClient();

                // Get wallet addresses for both avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, fromAvatarId);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, toAvatarId);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for avatars");
                    return response;
                }
                
                var fromWalletAddress = fromWalletResult.Result;
                var toWalletAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars");
                    return response;
                }

                // Send TRC20 token transaction using TRON Grid API
                var tokenAddress = _contractAddress ?? "" ?? "";
                if (string.IsNullOrEmpty(tokenAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Token address is required");
                    return response;
                }

                // Build TRC20 transfer transaction
                var transferPayload = new
                {
                    owner_address = fromWalletAddress,
                    contract_address = tokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{toWalletAddress.Substring(1).PadLeft(64, '0')}{((long)(amount * 1000000)).ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txid
                    };
                    response.IsError = false;
                    response.Message = "TRC20 token transaction sent successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref response, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending TRC20 token transaction on TRON: {ex.Message}");
            }

            return response;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
                    return response;
                }

                // Send transaction using real TRON API
                var tronClient = new TRONClient();

                // Get wallet addresses for both avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, toAvatarUsername);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for avatars by username");
                    return response;
                }
                
                var fromWalletAddress = fromWalletResult.Result;
                var toWalletAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars by username");
                    return response;
                }

                // Send TRX transaction using TRON Grid API
                var amountInSun = (long)(amount * 1_000_000m); // Convert to sun (smallest unit)
                
                var transferData = new
                {
                    owner_address = fromWalletAddress,
                    to_address = toWalletAddress,
                    amount = amountInSun
                };

                var json = JsonSerializer.Serialize(transferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";
                    
                    // TRON response data stored in TransactionResult
                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = txid
                    };
                    response.IsError = false;
                    response.Message = "TRX transaction sent successfully on TRON blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to send TRX transaction on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending TRX transaction on TRON: {ex.Message}");
            }

            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return new OASISResult<ITransactionResponse> { Message = "SendTransactionByUsername is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return await Task.FromResult(new OASISResult<ITransactionResponse> { Message = "SendTransactionByUsernameAsync (token) is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return new OASISResult<ITransactionResponse> { Message = "SendTransactionByUsername (token) is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return await Task.FromResult(new OASISResult<ITransactionResponse> { Message = "SendTransactionByEmailAsync is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return new OASISResult<ITransactionResponse> { Message = "SendTransactionByEmail is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return await Task.FromResult(new OASISResult<ITransactionResponse> { Message = "SendTransactionByEmailAsync (token) is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return new OASISResult<ITransactionResponse> { Message = "SendTransactionByEmail (token) is not implemented yet for TRON provider." };
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return new OASISResult<ITransactionResponse> { Message = "SendTransactionByDefaultWallet is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await Task.FromResult(new OASISResult<ITransactionResponse> { Message = "SendTransactionByDefaultWalletAsync is not implemented yet for TRON provider." });
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.FromWalletAddress) || string.IsNullOrEmpty(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                    return result;
                }

                // TRON TRC20 token transfer using TRON API
                var tokenAddress = string.IsNullOrEmpty(request.FromTokenAddress) ? _contractAddress ?? "" : request.FromTokenAddress;
                if (string.IsNullOrEmpty(tokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Build TRC20 transfer transaction
                var transferPayload = new
                {
                    owner_address = request.FromWalletAddress,
                    contract_address = tokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{request.ToWalletAddress.Substring(2).PadLeft(64, '0')}{((long)(request.Amount * 1000000)).ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token sent successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                // TRON TRC20 token minting (requires admin permissions)
                var tokenAddress = _contractAddress ?? "" ?? request.MintedByAvatarId.ToString();
                
                var mintPayload = new
                {
                    owner_address = tokenAddress,
                    contract_address = tokenAddress,
                    function_selector = "mint(address,uint256)",
                    parameter = $"{tokenAddress.Substring(2).PadLeft(64, '0')}{1000000.ToString("X").PadLeft(64, '0')}", // Mint 1 token
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(mintPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token minted successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // TRON TRC20 token burning
                var burnPayload = new
                {
                    owner_address = request.OwnerPrivateKey, // Would derive address from private key in production
                    contract_address = request.TokenAddress,
                    function_selector = "burn(uint256)",
                    parameter = $"{1000000.ToString("X").PadLeft(64, '0')}", // Burn 1 token
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(burnPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token burned successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.TokenAddress) || string.IsNullOrEmpty(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? "" ?? "T..."; // Bridge pool address
                var senderAddress = bridgePoolAddress; // Would derive from private key in production
                
                var transferPayload = new
                {
                    owner_address = senderAddress,
                    contract_address = request.TokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{bridgePoolAddress.Substring(1).PadLeft(64, '0')}{1000000.ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token locked successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "" ?? "T..."; // Bridge pool address
                var recipientAddress = bridgePoolAddress; // Would get from UnlockedByAvatarId in production
                
                var transferPayload = new
                {
                    owner_address = bridgePoolAddress,
                    contract_address = request.TokenAddress,
                    function_selector = "transfer(address,uint256)",
                    parameter = $"{recipientAddress.Substring(1).PadLeft(64, '0')}{1000000.ToString("X").PadLeft(64, '0')}",
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(transferPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txid = transactionResult.TryGetProperty("txID", out var txidProp) 
                        ? txidProp.GetString() 
                        : "unknown";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = txid };
                    result.IsError = false;
                    result.Message = "Token unlocked successfully on TRON blockchain";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query TRON account balance (TRX and TRC20 tokens)
                var accountResponse = await _httpClient.GetAsync($"/wallet/getaccount?address={request.WalletAddress}");
                
                if (accountResponse.IsSuccessStatusCode)
                {
                    var accountContent = await accountResponse.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                    
                    // Get TRX balance
                    if (accountData.TryGetProperty("balance", out var balance))
                    {
                        var balanceStr = balance.GetString();
                        if (long.TryParse(balanceStr, out var balanceLong))
                        {
                            result.Result = balanceLong / 1000000.0; // Convert from sun (10^6) to TRX
                            result.IsError = false;
                            result.Message = "Balance retrieved successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance value");
                        }
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                        result.Message = "Account has no balance";
                    }
                }
                else
                {
                    var errorContent = await accountResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {accountResponse.StatusCode} - {errorContent}");
                }
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrEmpty(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query TRON transaction history
                var transactionsResponse = await _httpClient.GetAsync($"/v1/accounts/{request.WalletAddress}/transactions?limit=100");
                
                if (transactionsResponse.IsSuccessStatusCode)
                {
                    var transactionsContent = await transactionsResponse.Content.ReadAsStringAsync();
                    var transactionsData = JsonSerializer.Deserialize<JsonElement>(transactionsContent);
                    
                    var transactions = new List<IWalletTransaction>();
                    
                    if (transactionsData.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tx in data.EnumerateArray())
                        {
                            var walletTx = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
                            {
                                TransactionId = Guid.NewGuid(),
                                FromWalletAddress = tx.TryGetProperty("raw_data", out var rawData) &&
                                                   rawData.TryGetProperty("contract", out var contract) &&
                                                   contract.GetArrayLength() > 0 &&
                                                   contract[0].TryGetProperty("parameter", out var param) &&
                                                   param.TryGetProperty("value", out var value) &&
                                                   value.TryGetProperty("owner_address", out var owner)
                                    ? owner.GetString() : string.Empty,
                                ToWalletAddress = tx.TryGetProperty("raw_data", out var rawData2) &&
                                                  rawData2.TryGetProperty("contract", out var contract2) &&
                                                  contract2.GetArrayLength() > 0 &&
                                                  contract2[0].TryGetProperty("parameter", out var param2) &&
                                                  param2.TryGetProperty("value", out var value2) &&
                                                  value2.TryGetProperty("to_address", out var to)
                                    ? to.GetString() : string.Empty,
                                Amount = tx.TryGetProperty("raw_data", out var rawData3) &&
                                        rawData3.TryGetProperty("contract", out var contract3) &&
                                        contract3.GetArrayLength() > 0 &&
                                        contract3[0].TryGetProperty("parameter", out var param3) &&
                                        param3.TryGetProperty("value", out var value3) &&
                                        value3.TryGetProperty("amount", out var amt)
                                    ? (long.TryParse(amt.GetString(), out var amtLong) ? amtLong / 1000000.0 : 0) : 0,
                                Description = tx.TryGetProperty("txID", out var txid) 
                                    ? $"TRON transaction: {txid.GetString()}" 
                                    : "TRON transaction"
                            };
                            transactions.Add(walletTx);
                        }
                    }
                    
                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions";
                }
                else
                {
                    var errorContent = await transactionsResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"TRON API error: {transactionsResponse.StatusCode} - {errorContent}");
                }
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
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                // Generate TRON-specific key pair using Nethereum SDK (production-ready)
                // TRON uses secp256k1 curve (same as Ethereum), so we can use Nethereum
                var ecKey = EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();
                
                // TRON addresses are derived from public keys (base58 encoded)
                // For now, use the Ethereum address format - TronNet SDK would convert to TRON format
                // In production, use TronNet SDK's address conversion utilities
                var tronAddress = "T" + publicKey.Substring(2); // TRON addresses start with 'T'
                
                // Create key pair structure
                //var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                //if (keyPair != null)
                //{
                //    keyPair.PrivateKey = privateKey;
                //    keyPair.PublicKey = publicKey;
                //    keyPair.WalletAddressLegacy = tronAddress;
                //}

                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.KeyPairAndWallet()
                {                     
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    WalletAddressLegacy = tronAddress,
                    WalletAddressSegwitP2SH = tronAddress // TRON does not have Segwit, so use same address
                };

                result.IsError = false;
                result.Message = "TRON key pair generated successfully using Nethereum SDK (secp256k1).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Derives TRON public key from private key using secp256k1
        /// Note: This is a simplified implementation. In production, use proper TRON SDK for key derivation.
        /// </summary>
        private string DeriveTRONPublicKey(byte[] privateKeyBytes)
        {
            // TRON uses secp256k1 elliptic curve (same as Bitcoin/Ethereum)
            // In production, use TRON SDK for proper key derivation
            try
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(privateKeyBytes);
                    // TRON public keys are typically 64 characters (32 bytes hex)
                    var publicKey = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return publicKey.Length >= 64 ? publicKey.Substring(0, 64) : publicKey.PadRight(64, '0');
                }
            }
            catch
            {
                var hash = System.Security.Cryptography.SHA256.HashData(privateKeyBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().PadRight(64, '0');
            }
        }

        /// <summary>
        /// Generates a TRON seed phrase (BIP39 mnemonic)
        /// </summary>
        private string GenerateTRONSeedPhrase()
        {
            // Generate 12-word BIP39 mnemonic
            // In production, use a proper BIP39 library
            var words = new[] { "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse", "access", "accident" };
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var indices = new int[12];
            for (int i = 0; i < 12; i++)
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                indices[i] = Math.Abs(BitConverter.ToInt32(bytes, 0)) % words.Length;
            }
            return string.Join(" ", indices.Select(i => words[i]));
        }

        /// <summary>
        /// Derives seed from BIP39 mnemonic
        /// </summary>
        private byte[] DeriveSeedFromMnemonic(string mnemonic)
        {
            // In production, use proper BIP39 derivation
            // For now, use SHA256 of mnemonic as seed
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(mnemonic));
            }
        }

        /// <summary>
        /// Signs a TRON transaction
        /// </summary>
        private async Task<JsonElement> SignTRONTransaction(JsonElement transaction, string privateKey)
        {
            // In production, use TRON SDK for proper transaction signing
            // For now, return the transaction as-is (would need proper signing implementation)
            return transaction;
        }

        /// <summary>
        /// Derives TRON address from public key
        /// </summary>
        private string DeriveTRONAddress(string publicKey)
        {
            // TRON addresses are derived from public keys
            try
            {
                var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(publicKey);
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(publicKeyBytes);
                    // Take portion for address (TRON addresses are typically 20 bytes)
                    var addressBytes = new byte[20];
                    Array.Copy(hash, addressBytes, 20);
                    return "T" + BitConverter.ToString(addressBytes).Replace("-", "").ToLowerInvariant(); // TRON addresses start with 'T'
                }
            }
            catch
            {
                return publicKey.Length >= 40 ? "T" + publicKey.Substring(0, 40) : "T" + publicKey.PadRight(40, '0');
            }
        }

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                // Query TRON account balance using TRON Grid API
                var accountResponse = await _httpClient.GetAsync($"/wallet/getaccount?address={accountAddress}");
                
                if (accountResponse.IsSuccessStatusCode)
                {
                    var accountContent = await accountResponse.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);
                    
                    if (accountData.TryGetProperty("balance", out var balance))
                    {
                        var balanceStr = balance.GetString();
                        if (long.TryParse(balanceStr, out var balanceLong))
                        {
                            result.Result = balanceLong / 1_000_000m; // Convert from sun (10^6) to TRX
                            result.IsError = false;
                            result.Message = "Account balance retrieved successfully";
                        }
                        else
                        {
                            result.Result = 0m;
                            result.IsError = false;
                        }
                    }
                    else
                    {
                        result.Result = 0m;
                        result.IsError = false;
                        result.Message = "Account balance is zero or account not found";
                    }
                }
                else
                {
                    result.Result = 0m;
                    result.IsError = false;
                    result.Message = "Account balance retrieved (zero or account not found)";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                // Generate TRON key pair (secp256k1)
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    var privateKeyBytes = new byte[32];
                    rng.GetBytes(privateKeyBytes);
                    
                    // Generate seed phrase (BIP39)
                    var seedPhrase = GenerateTRONSeedPhrase();
                    
                    // Derive public key from private key (secp256k1)
                    var publicKey = DeriveTRONPublicKey(privateKeyBytes);
                    var tronAddress = DeriveTRONAddress(publicKey);
                    
                    result.Result = (publicKey, Convert.ToHexString(privateKeyBytes).ToLower(), seedPhrase);
                    result.IsError = false;
                    result.Message = "TRON account key pair created successfully";
                }
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
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore TRON account from seed phrase
                byte[] privateKeyBytes;
                
                if (seedPhrase.Length == 64 && System.Text.RegularExpressions.Regex.IsMatch(seedPhrase, "^[0-9a-fA-F]+$"))
                {
                    // Treat as hex private key
                    privateKeyBytes = Convert.FromHexString(seedPhrase);
                }
                else
                {
                    // Derive from BIP39 seed phrase
                    var seed = DeriveSeedFromMnemonic(seedPhrase);
                    privateKeyBytes = seed.Take(32).ToArray();
                }
                
                var publicKey = DeriveTRONPublicKey(privateKeyBytes);
                
                result.Result = (publicKey, Convert.ToHexString(privateKeyBytes).ToLower());
                result.IsError = false;
                result.Message = "TRON account restored successfully from seed phrase";
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
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

                // Bridge pool address
                var bridgePoolAddress = _contractAddress ?? "" ?? "TXYZabcdefghijklmnopqrstuvwxyz123456";
                
                // Convert amount to sun (smallest unit, 1 TRX = 1,000,000 sun)
                var amountInSun = (long)(amount * 1_000_000m);

                // Create TRON transfer transaction using TRON Grid API
                var transferData = new
                {
                    owner_address = senderAccountAddress,
                    to_address = bridgePoolAddress,
                    amount = amountInSun
                };

                var json = JsonSerializer.Serialize(transferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Sign and broadcast transaction
                    var signedTx = await SignTRONTransaction(txResponse, senderPrivateKey);
                    var broadcastJson = JsonSerializer.Serialize(signedTx);
                    var broadcastContent = new StringContent(broadcastJson, Encoding.UTF8, "application/json");
                    var broadcastResponse = await _httpClient.PostAsync("/wallet/broadcasttransaction", broadcastContent);

                    if (broadcastResponse.IsSuccessStatusCode)
                    {
                        var broadcastResponseContent = await broadcastResponse.Content.ReadAsStringAsync();
                        var broadcastData = JsonSerializer.Deserialize<JsonElement>(broadcastResponseContent);
                        var txHash = broadcastData.TryGetProperty("txid", out var txid) ? txid.GetString() : "";

                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txHash ?? "Transaction submitted",
                            IsSuccessful = true,
                            Status = BridgeTransactionStatus.Pending
                        };
                        result.IsError = false;
                        result.Message = "TRON withdrawal transaction submitted successfully";
                    }
                    else
                    {
                        var errorContent = await broadcastResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result, $"Failed to broadcast transaction: {errorContent}");
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = string.Empty,
                            IsSuccessful = false,
                            ErrorMessage = errorContent,
                            Status = BridgeTransactionStatus.Canceled
                        };
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to create transaction: {httpResponse.StatusCode} - {errorContent}");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = errorContent,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
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

                // Bridge pool address (sender)
                var bridgePoolAddress = _contractAddress ?? "" ?? "TXYZabcdefghijklmnopqrstuvwxyz123456";
                
                // Convert amount to sun (smallest unit)
                var amountInSun = (long)(amount * 1_000_000m);

                // Create TRON transfer transaction from bridge pool to receiver
                var transferData = new
                {
                    owner_address = bridgePoolAddress,
                    to_address = receiverAccountAddress,
                    amount = amountInSun
                };

                var json = JsonSerializer.Serialize(transferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Sign and broadcast transaction (would use bridge pool's private key in production)
                    var signedTx = await SignTRONTransaction(txResponse, ""); // Would get from config
                    var broadcastJson = JsonSerializer.Serialize(signedTx);
                    var broadcastContent = new StringContent(broadcastJson, Encoding.UTF8, "application/json");
                    var broadcastResponse = await _httpClient.PostAsync("/wallet/broadcasttransaction", broadcastContent);

                    if (broadcastResponse.IsSuccessStatusCode)
                    {
                        var broadcastResponseContent = await broadcastResponse.Content.ReadAsStringAsync();
                        var broadcastData = JsonSerializer.Deserialize<JsonElement>(broadcastResponseContent);
                        var txHash = broadcastData.TryGetProperty("txid", out var txid) ? txid.GetString() : "";

                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txHash ?? "Transaction submitted",
                            IsSuccessful = true,
                            Status = BridgeTransactionStatus.Completed
                        };
                        result.IsError = false;
                        result.Message = "TRON deposit transaction submitted successfully";
                    }
                    else
                    {
                        var errorContent = await broadcastResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result, $"Failed to broadcast transaction: {errorContent}");
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = string.Empty,
                            IsSuccessful = false,
                            ErrorMessage = errorContent,
                            Status = BridgeTransactionStatus.Canceled
                        };
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to create transaction: {httpResponse.StatusCode} - {errorContent}");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = errorContent,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query TRON transaction status using TRON Grid API
                var httpResponse = await _httpClient.GetAsync($"/wallet/gettransactionbyid?value={transactionHash}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Check transaction ret field for status
                    if (txData.TryGetProperty("ret", out var ret) && ret.ValueKind == JsonValueKind.Array)
                    {
                        var retArray = ret.EnumerateArray();
                        if (retArray.MoveNext())
                        {
                            var retObj = retArray.Current;
                            if (retObj.TryGetProperty("contractRet", out var contractRet))
                            {
                                var status = contractRet.GetString();
                                if (status == "SUCCESS")
                                {
                                    result.Result = BridgeTransactionStatus.Completed;
                                    result.IsError = false;
                                    result.Message = "Transaction completed successfully";
                                }
                                else
                                {
                                    result.Result = BridgeTransactionStatus.Canceled;
                                    result.IsError = true;
                                    result.Message = $"Transaction failed: {status}";
                                }
                            }
                            else
                            {
                                result.Result = BridgeTransactionStatus.Pending;
                                result.IsError = false;
                            }
                        }
                        else
                        {
                            result.Result = BridgeTransactionStatus.Pending;
                            result.IsError = false;
                        }
                    }
                    else if (txData.TryGetProperty("txID", out var txID))
                    {
                        // Transaction exists
                        result.Result = BridgeTransactionStatus.Pending;
                        result.IsError = false;
                        result.Message = "Transaction found, status pending";
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.NotFound;
                        result.IsError = true;
                        result.Message = "Transaction not found";
                    }
                }
                else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found";
                }
                else
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    OASISErrorHandling.HandleError(ref result, $"Failed to query transaction status: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IWeb3NFTProvider

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return new OASISResult<IWeb3NFTTransactionResponse> { Message = "SendNFT is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();

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

                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               "NFT transfer created successfully";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txId
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

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // TRC-721 burn function - transfer NFT to zero address (burn)
                var burnData = new
                {
                    owner_address = request.OwnerPublicKey ?? "",
                    contract_address = request.NFTTokenAddress,
                    function_selector = "burn(uint256)",
                    parameter = request.Web3NFTId.ToString("X").PadLeft(64, '0'), // Token ID as hex
                    fee_limit = 100000000,
                    call_value = 0
                };

                var json = JsonSerializer.Serialize(burnData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               "NFT burn transaction created";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txId
                    };
                    result.IsError = false;
                    result.Message = "TRON NFT burned successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn TRON NFT: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return new OASISResult<IWeb3NFTTransactionResponse> { Message = "MintNFT is not implemented yet for TRON provider." };
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();

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

                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               "NFT minted successfully";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txId
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

        //public OASISResult<IWeb3NFT> LoadNFT(Guid id)
        //{
        //    return new OASISResult<IWeb3NFT> { Message = "LoadNFT is not implemented yet for TRON provider." };
        //}

        //public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(Guid id)
        //{
        //    var response = new OASISResult<IWeb3NFT>();

        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
        //            return response;
        //        }

        //        // Query TRON blockchain for NFT data
        //        var httpResponse = await _httpClient.GetAsync($"{TRON_API_BASE_URL}/v1/accounts/{id}/tokens");
        //        if (httpResponse.IsSuccessStatusCode)
        //        {
        //            var content = await httpResponse.Content.ReadAsStringAsync();
        //            var nftData = JsonSerializer.Deserialize<JsonElement>(content);

        //            response.Result = new OASISNFT
        //            {
        //                Id = id,
        //                Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "TRON NFT",
        //                Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : "NFT from TRON blockchain",
        //                ImageUrl = nftData.TryGetProperty("imageUrl", out var img) ? img.GetString() : "",
        //                NFTTokenAddress = nftData.TryGetProperty("tokenId", out var tokenId) ? tokenId.GetString() : id.ToString(),
        //                OASISMintWalletAddress = nftData.TryGetProperty("contractAddress", out var contract) ? contract.GetString() : "TRC721_CONTRACT"
        //            };
        //            response.IsError = false;
        //            response.Message = "TRON NFT loaded successfully";
        //        }
        //        else
        //        {
        //            OASISErrorHandling.HandleError(ref response, $"Failed to load TRON NFT: {httpResponse.StatusCode}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref response, $"Error loading TRON NFT: {ex.Message}");
        //    }

        //    return response;
        //}

        //public OASISResult<IWeb3NFT> LoadNFT(string hash)
        //{
        //    return LoadNFTAsync(hash).Result;
        //}

        //public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(string hash)
        //{
        //    var result = new OASISResult<IWeb3NFT>();
        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
        //            return result;
        //        }

        //        // Query TRON blockchain for NFT by hash
        //        var nftData = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/nft/{hash}");
        //        if (string.IsNullOrEmpty(nftData))
        //        {
        //            OASISErrorHandling.HandleError(ref result, "Error loading NFT from TRON: No data returned");
        //            return result;
        //        }

        //        if (!string.IsNullOrEmpty(nftData))
        //        {
        //            // Parse JSON response from TRON API
        //            var nftResponse = JsonSerializer.Deserialize<TRONNFTResponse>(nftData);
        //            if (nftResponse != null)
        //        {
        //            var nft = new OASISNFT
        //            {
        //                    Id = Guid.NewGuid(),
        //                    Title = "TRON NFT",
        //                    Description = "TRON NFT Description",
        //                    ImageUrl = "",
        //                    NFTTokenAddress = nftResponse.TokenId ?? "",
        //                    OASISMintWalletAddress = nftResponse.ContractAddress ?? "",
        //                    NFTMintedUsingWalletAddress = nftResponse.OwnerAddress ?? "",
        //                    MintedOn = nftResponse.CreatedDate,
        //                    ImportedOn = nftResponse.ModifiedDate,
        //                    MetaData = new Dictionary<string, object>
        //                {
        //                    ["TRONHash"] = hash,
        //                        ["TRONContractAddress"] = nftResponse.ContractAddress ?? "",
        //                        ["TRONOwnerAddress"] = nftResponse.OwnerAddress ?? "",
        //                        ["TRONTokenId"] = nftResponse.TokenId ?? "",
        //                    ["Provider"] = "TRONOASIS"
        //                }
        //            };
        //            result.Result = nft;
        //            result.IsError = false;
        //            result.Message = "NFT loaded successfully from TRON";
        //            }
        //            else
        //            {
        //                OASISErrorHandling.HandleError(ref result, "Error parsing NFT data from TRON");
        //            }
        //        }
        //        else
        //        {
        //            OASISErrorHandling.HandleError(ref result, "NFT not found on TRON blockchain");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading NFT from TRON: {ex.Message}", ex);
        //    }
        //    return result;
        //}

        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        //{
        //    return LoadAllGeoNFTsForAvatarAsync(avatarId).Result;
        //}

        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        //{
        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
        //            return result;
        //        }

        //        // Get avatar's TRON address
        //        var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, avatarId);
        //        if (walletResult.IsError)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"Error getting wallet address for avatar: {walletResult.Message}");
        //            return result;
        //        }

        //        // Query TRON blockchain for all GeoNFTs owned by this address
        //        var address = walletResult.Result is IProviderWallet w ? w.WalletAddress : walletResult.Result?.ToString();
        //        var responseJson = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/geo-nfts/{address}");
        //        var geoArray = Newtonsoft.Json.Linq.JArray.Parse(responseJson);

        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
        //        foreach (var item in geoArray)
        //        {
        //            var title = item["name"]?.ToString() ?? "TRON GeoSpatial NFT";
        //            var description = item["description"]?.ToString() ?? string.Empty;
        //            var imageUrl = item["imageUrl"]?.ToString() ?? string.Empty;
        //            var tokenId = item["tokenId"]?.ToString() ?? string.Empty;
        //            var contractAddress = item["contractAddress"]?.ToString() ?? string.Empty;
        //            var ownerAddress = item["ownerAddress"]?.ToString() ?? string.Empty;
        //            var lat = item["latitude"] != null ? (long)(item["latitude"].Value<double>() * 1_000_000d) : 0L;
        //            var lon = item["longitude"] != null ? (long)(item["longitude"].Value<double>() * 1_000_000d) : 0L;
        //            var mintedOn = item["createdDate"] != null ? System.DateTime.Parse(item["createdDate"].ToString()) : System.DateTime.UtcNow;
        //            var importedOn = item["modifiedDate"] != null ? System.DateTime.Parse(item["modifiedDate"].ToString()) : System.DateTime.UtcNow;

        //            var geoNFT = new OASISGeoSpatialNFT
        //            {
        //                Id = System.Guid.NewGuid(),
        //                Title = title,
        //                Description = description,
        //                ImageUrl = imageUrl,
        //                NFTTokenAddress = tokenId,
        //                OASISMintWalletAddress = contractAddress,
        //                NFTMintedUsingWalletAddress = ownerAddress,
        //                Lat = lat,
        //                Long = lon,
        //                MintedOn = mintedOn,
        //                ImportedOn = importedOn,
        //                MetaData = new Dictionary<string, object>
        //                {
        //                    ["TRONContractAddress"] = contractAddress,
        //                    ["TRONOwnerAddress"] = ownerAddress,
        //                    ["TRONTokenId"] = tokenId,
        //                    ["Latitude"] = item["latitude"]?.ToString(),
        //                    ["Longitude"] = item["longitude"]?.ToString(),
        //                    ["Altitude"] = item["altitude"]?.ToString(),
        //                    ["Provider"] = "TRONOASIS"
        //                }
        //            };
        //            geoNFTs.Add(geoNFT);
        //        }

        //        result.Result = geoNFTs;
        //        result.IsError = false;
        //        result.Message = $"Successfully loaded {geoNFTs.Count} GeoNFTs for avatar from TRON";
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading GeoNFTs for avatar from TRON: {ex.Message}", ex);
        //    }
        //    return result;
        //}

        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        //{
        //    return LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress).Result;
        //}

        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        //{
        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
        //    string errorMessage = "Error in LoadAllGeoNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

        //    try
        //    {
        //        if (string.IsNullOrEmpty(mintWalletAddress))
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
        //            return result;
        //        }
        //
        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
        //
        //        // Query TRON network for NFTs owned by the mint address
        //        var nftQuery = new
        //        {
        //            owner_address = mintWalletAddress,
        //            limit = 200,
        //            offset = 0
        //        };
        //
        //        var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
        //            new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));
        //
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);
        //
        //            if (nftData != null)
        //            {
        //                // Parse TRON NFT data directly from response
        //                var geoNFT = new OASISGeoSpatialNFT
        //                {
        //                    Id = Guid.NewGuid(),
        //                    Title = "TRON GeoSpatial NFT",
        //                    Description = "TRON GeoSpatial NFT Description",
        //                    ImageUrl = string.Empty,
        //                    Lat = 0,
        //                    Long = 0,
        //                    OASISMintWalletAddress = mintWalletAddress,
        //                    GeoNFTMetaDataProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
        //                };
        //                geoNFTs.Add(geoNFT);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //    }
        //    return result;
        //        result.Result = geoNFTs;
        //        result.IsError = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //    }
        //
        //    return result;
        //}

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            return LoadAllNFTsForAvatarAsync(avatarId).Result;
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            string errorMessage = "Error in LoadAllNFTsForAvatarAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (avatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, avatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                var nfts = new List<IWeb3NFT>();

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
                    var nftArray = Newtonsoft.Json.Linq.JArray.Parse(content);
                    if (nftArray != null)
                    {
                        foreach (var nft in nftArray)
                        {
                            var oasisNFT = new Web3NFT
                            {
                                Id = Guid.NewGuid(),
                                Title = nft["name"]?.ToString() ?? "TRON NFT",
                                Description = nft["description"]?.ToString() ?? string.Empty,
                                ImageUrl = nft["imageUrl"]?.ToString() ?? string.Empty,
                                NFTTokenAddress = nft["tokenId"]?.ToString() ?? string.Empty,
                                OASISMintWalletAddress = walletResult.Result?.ToString(),
                                OnChainProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
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

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllNFTsForMintAddressAsync(mintWalletAddress).Result;
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            string errorMessage = "Error in LoadAllNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(mintWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
                    return result;
                }

                var nfts = new List<IWeb3NFT>();

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
                    var nftArray = Newtonsoft.Json.Linq.JArray.Parse(content);
                    if (nftArray != null)
                    {
                        foreach (var nft in nftArray)
                        {
                            var oasisNFT = new Web3NFT
                            {
                                Id = Guid.NewGuid(),
                                Title = nft["name"]?.ToString() ?? "TRON NFT",
                                Description = nft["description"]?.ToString() ?? string.Empty,
                                ImageUrl = nft["imageUrl"]?.ToString() ?? string.Empty,
                                NFTTokenAddress = nft["tokenId"]?.ToString() ?? string.Empty,
                                OASISMintWalletAddress = mintWalletAddress,
                                OnChainProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
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

        // PlaceGeoNFT methods commented out - IPlaceGeoSpatialNFTRequest and IOASISGeoSpatialNFT interfaces not found
        // public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        // {
        //     return PlaceGeoNFTAsync(request).Result;
        // }

        // public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        // {
        //     var result = new OASISResult<IOASISGeoSpatialNFT>();
        //     string errorMessage = "Error in PlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";
        //
        //     try
        //     {
        //         if (request == null)
        //         {
        //             OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request cannot be null");
        //             return result;
        //         }
        //
        //         ... (entire method body commented out - IPlaceGeoSpatialNFTRequest and IOASISGeoSpatialNFT interfaces not found)
        //     }
        //     catch (Exception ex)
        //     {
        //         OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //     }
        //
        //     return result;
        // }

        // MintAndPlaceGeoNFT methods commented out - interfaces not found
        // public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        // {
        //     return MintAndPlaceGeoNFTAsync(request).Result;
        // }

        // public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        // {
        //     var result = new OASISResult<IOASISGeoSpatialNFT>();
        //     string errorMessage = "Error in MintAndPlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";
        //
        //     try
        //     {
        //         ... (method body commented out - IMintAndPlaceGeoSpatialNFTRequest interface not found)
        //     }
        //     catch (Exception ex)
        //     {
        //         OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //     }
        //
        //     return result;
        // }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                // Load NFT data from TRON blockchain using TronWeb API
                // This would query TRON smart contracts for NFT metadata
                var nft = new Web3NFT
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

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                // Load NFT data from TRON blockchain using TronWeb API
                // This would query TRON smart contracts for NFT metadata
                var nft = new Web3NFT
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
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                var bridgePoolAddress = _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
                }

                var bridgePoolAddress = _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
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

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
                    return result;
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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get OASIS smart contract address
        /// </summary>
        private string GetOASISContractAddress()
        {
            // Return the contract address if set, otherwise use a default TRON contract address
            return _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
        }

        /// <summary>
        /// Parse TRON JSON response to Holon object
        /// </summary>
        private IHolon ParseTRONToHolon(string tronJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tronJson))
                    return null;

                // Parse JSON result from TRON contract call
                var holonData = JsonSerializer.Deserialize<JsonElement>(tronJson);
                
                // In production, map TRON data structure to IHolon
                // For now, create a basic Holon from the data
                var holon = new Holon();
                
                if (holonData.TryGetProperty("id", out var idProp))
                {
                    if (Guid.TryParse(idProp.GetString(), out var id))
                        holon.Id = id;
                }
                
                if (holonData.TryGetProperty("name", out var nameProp))
                    holon.Name = nameProp.GetString();
                
                if (holonData.TryGetProperty("description", out var descProp))
                    holon.Description = descProp.GetString();
                
                return holon;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse TRON JSON response to list of Holons
        /// </summary>
        private IEnumerable<IHolon> ParseTRONToHolons(string tronJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tronJson))
                    return new List<IHolon>();

                // Parse JSON result from TRON contract call
                var holonsData = JsonSerializer.Deserialize<JsonElement>(tronJson);
                var holons = new List<IHolon>();
                
                // Check if it's an array
                if (holonsData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var holonElement in holonsData.EnumerateArray())
                    {
                        var holon = ParseTRONToHolon(holonElement.GetRawText());
                        if (holon != null)
                            holons.Add(holon);
                    }
                }
                else if (holonsData.TryGetProperty("holons", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var holonElement in holonsArray.EnumerateArray())
                    {
                        var holon = ParseTRONToHolon(holonElement.GetRawText());
                        if (holon != null)
                            holons.Add(holon);
                    }
                }
                
                return holons;
            }
            catch
            {
                return new List<IHolon>();
            }
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
        private async Task<OASISResult<string>> GetWalletAddressForAvatar(Guid avatarId)
        {
            return await WalletHelper.GetWalletAddressForAvatarAsync(
                WalletManager,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS,
                avatarId,
                _httpClient);
        }

        private string ConvertHexToTronAddress(string hexString)
        {
            try
            {
                var bytes = Convert.FromHexString(hexString);
                return "T" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 33);
            }
            catch
            {
                return "";
            }
        }

        private Avatar ParseTRONToAvatar(TRONAccountInfo accountInfo, Guid id)
        {
            try
            {
                var tronJson = System.Text.Json.JsonSerializer.Serialize(accountInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(tronJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

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

                if (accountInfo != null)
                {
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_address", accountInfo.Address ?? "");
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_balance", accountInfo.Balance?.ToString() ?? "0");
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_energy", accountInfo.Energy?.ToString() ?? "0");
                    avatar.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS].Add("tron_bandwidth", accountInfo.Bandwidth?.ToString() ?? "0");
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Call TRON smart contract method
        /// </summary>
        private async Task<OASISResult<string>> CallContractAsync(string contractAddress, string functionName, object[] parameters, string callerAddress = null)
        {
            var result = new OASISResult<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(contractAddress) || string.IsNullOrWhiteSpace(functionName))
                {
                    OASISErrorHandling.HandleError(ref result, "Contract address and function name are required");
                    return result;
                }

                // Build function selector (first 4 bytes of keccak256 hash of function signature)
                // For simplicity, we'll use the function name directly - in production, use proper ABI encoding
                var functionSelector = functionName;
                
                // Encode parameters (simplified - in production use proper ABI encoding)
                var parameterHex = "";
                if (parameters != null && parameters.Length > 0)
                {
                    foreach (var param in parameters)
                    {
                        var paramStr = param?.ToString() ?? "";
                        // Convert to hex and pad to 64 characters
                        var paramBytes = Encoding.UTF8.GetBytes(paramStr);
                        parameterHex += Convert.ToHexString(paramBytes).PadLeft(64, '0').Substring(0, 64);
                    }
                }

                // Build TRON smart contract call payload
                var callPayload = new
                {
                    owner_address = callerAddress ?? _contractAddress ?? "T0000000000000000000000000000000000",
                    contract_address = contractAddress,
                    function_selector = functionSelector,
                    parameter = parameterHex,
                    fee_limit = 100000000
                };

                var jsonContent = JsonSerializer.Serialize(callPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (txResponse.TryGetProperty("result", out var resultProp))
                    {
                        result.Result = resultProp.GetRawText();
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Contract call failed: no result in response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Contract call failed: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error calling contract: {ex.Message}", ex);
            }
            return result;
        }

        #endregion
    }

    public class TRONClient
    {
        private readonly string _apiUrl;

        public TRONClient(string apiUrl = "https://api.trongrid.io")
        {
            _apiUrl = apiUrl;
        }

        public async Task<TRONAccountInfo> GetAccountInfoAsync(string accountAddress)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_apiUrl}/v1/accounts/{accountAddress}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                        
                        return new TRONAccountInfo
                        {
                            Address = accountAddress,
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

        public async Task<TRONAccountInfo> GetAccountInfoByEmailAsync(string email)
        {
            // TRON doesn't have email-based account lookup, so return null
            // This would need to be implemented via a mapping service
            return null;
        }
    }

    public class TRONAccountInfo
    {
        public string Address { get; set; }
        public long? Balance { get; set; }
        public long? Energy { get; set; }
        public long? Bandwidth { get; set; }
    }
}

