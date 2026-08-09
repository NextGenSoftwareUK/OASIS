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
    public partial class TRONOASIS
    {
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Save avatar to TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "saveAvatar";
                var parameters = new object[]
                {
                    avatar.Id.ToString(),
                    avatar.Username ?? "",
                    avatar.Email ?? "",
                    avatar.FirstName ?? "",
                    avatar.LastName ?? "",
                    avatar.Title ?? "",
                    avatar.Password ?? "",
                    (int)avatar.AvatarType.Value,
                    avatar.AcceptTerms,
                    avatar.JwtToken ?? "",
                    avatar.PasswordReset.HasValue ? ((DateTimeOffset)avatar.PasswordReset.Value).ToUnixTimeSeconds() : 0,
                    avatar.RefreshToken ?? "",
                    avatar.ResetToken ?? "",
                    avatar.ResetTokenExpires.HasValue ? ((DateTimeOffset)avatar.ResetTokenExpires.Value).ToUnixTimeSeconds() : 0,
                    avatar.VerificationToken ?? "",
                    avatar.Verified.HasValue ? ((DateTimeOffset)avatar.Verified.Value).ToUnixTimeSeconds() : 0,
                    avatar.LastBeamedIn.HasValue ? ((DateTimeOffset)avatar.LastBeamedIn.Value).ToUnixTimeSeconds() : 0,
                    avatar.LastBeamedOut.HasValue ? ((DateTimeOffset)avatar.LastBeamedOut.Value).ToUnixTimeSeconds() : 0,
                    avatar.IsBeamedIn,
                    ((DateTimeOffset)avatar.CreatedDate).ToUnixTimeSeconds(),
                    ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                    avatar.Description ?? "",
                    avatar.IsActive
                };

                // Call TRON smart contract to save avatar
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar saved successfully to TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to TRON: {ex.Message}", ex);
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

                // Call TRON smart contract to save avatar detail
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail saved successfully to TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to TRON: {contractResult.Message}");
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

        public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // Load holon by custom key from TRON smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonByCustomKey";
                var parameters = new object[] { customKey };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holon = ParseTRONToHolon(contractResult.Result);
                    if (holon != null)
                    {
                        // Load children if requested
                        if (loadChildren && (recursive || maxChildDepth > 0))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from TRON by custom key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse holon from TRON");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by custom key from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (string.IsNullOrWhiteSpace(metaKey) || string.IsNullOrWhiteSpace(metaValue))
                {
                    OASISErrorHandling.HandleError(ref result, "Metadata key and value are required");
                    return result;
                }

                // Load holons by metadata and return first match
                var holonsResult = await LoadHolonsByMetaDataAsync(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                
                if (!holonsResult.IsError && holonsResult.Result != null && holonsResult.Result.Any())
                {
                    result.Result = holonsResult.Result.First();
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from TRON by metadata";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No holon found matching the metadata criteria");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by metadata from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByMetaDataAsync(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}