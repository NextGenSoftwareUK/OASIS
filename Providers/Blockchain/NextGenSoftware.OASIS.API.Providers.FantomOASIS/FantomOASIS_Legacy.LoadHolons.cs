using System;
using Nethereum.Hex.HexConvertors.Extensions;
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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public partial class FantomOASIS_Legacy
    {
        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return LoadAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Query smart contract for avatar by email
                // Search through all avatars to find one with matching email
                var getAvatarsCountFunction = _contract.GetFunction("getAvatarsCount");
                var avatarsCount = await getAvatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction("getAvatarById");
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        // Check if this avatar matches the email (stored in Info field as JSON)
                        if (!string.IsNullOrEmpty(avatarData.Info) && avatarData.Info.Contains(email))
                        {
                            var avatar = JsonSerializer.Deserialize<Avatar>(avatarData.Info, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            if (avatar != null && avatar.Email == email)
                            {
                                response.Result = avatar;
                                response.IsError = false;
                                response.Message = "Avatar loaded from Fantom by email successfully";
                                return response;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar not found with email: {email}");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Fantom: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Query smart contract for avatar detail by email
                var getAvatarDetailsCountFunction = _contract.GetFunction("getAvatarDetailsCount");
                var avatarDetailsCount = await getAvatarDetailsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarDetailsCount; i++)
                {
                    try
                    {
                        var getAvatarDetailFunction = _contract.GetFunction("getAvatarDetailById");
                        var avatarDetailData = await getAvatarDetailFunction.CallDeserializingToObjectAsync<AvatarDetailStruct>(i);
                        
                        if (!string.IsNullOrEmpty(avatarDetailData.Info) && avatarDetailData.Info.Contains(email))
                        {
                            var avatarDetail = JsonSerializer.Deserialize<AvatarDetail>(avatarDetailData.Info, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            if (avatarDetail != null && avatarDetail.Email == email)
                            {
                                response.Result = avatarDetail;
                                response.IsError = false;
                                response.Message = "Avatar detail loaded from Fantom by email successfully";
                                return response;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar detail not found with email: {email}");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from Fantom: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByUsername is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByUsername: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByUsernameAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByUsernameAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }


        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaData is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaData: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Load all holons for the current user
                var getUserHolonsFunction = _contract.GetFunction("getUserHolons");
                var holonIds = await getUserHolonsFunction.CallAsync<List<string>>(_account.Address);

                var holons = new List<IHolon>();
                foreach (var holonId in holonIds)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction("getHolon");
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);

                        if (holonData != null)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(holonId),
                                Name = holonData.Name,
                                Description = holonData.Description,
                                HolonType = Enum.Parse<HolonType>(holonData.HolonType)
                            };

                            if (!string.IsNullOrEmpty(holonData.Metadata))
                            {
                                try
                                {
                                    holon.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.Metadata);
                                }
                                catch { }
                            }

                            if (!string.IsNullOrEmpty(holonData.ParentId) && Guid.TryParse(holonData.ParentId, out var parentId))
                            {
                                holon.ParentHolonId = parentId;
                            }

                            // Filter by type if specified
                            if (type == HolonType.All || holon.HolonType == type)
                            {
                                holons.Add(holon);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error loading holon {holonId}: {ex.Message}", ex);
                            return response;
                        }
                    }
                }

                response.Result = holons;
                response.IsError = false;
                response.Message = $"Loaded {holons.Count} holons from Fantom";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolonsAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarDetails is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetails: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarDetailsAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetailsAsync: {ex.Message}");
            }
            return response;
        }

        // Additional missing abstract methods
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Delete avatar from smart contract
                var avatarId = id.ToString();
                var deleteAvatarFunction = _contract.GetFunction("deleteAvatar");
                var gasEstimate = await deleteAvatarFunction.EstimateGasAsync(avatarId);

                var transactionReceipt = await deleteAvatarFunction.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    avatarId
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = $"Avatar deleted from Fantom successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Fantom");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Fantom: {ex.Message}", ex);
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmail} not found");
                return response;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                return response;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with provider key {providerKey} not found");
                return response;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Query smart contract for avatar by username
                // Use getUserAvatars for efficiency
                var getUserAvatarsFunction = _contract.GetFunction("getUserAvatars");
                var avatarIds = await getUserAvatarsFunction.CallAsync<List<string>>(_account.Address);

                foreach (var avatarId in avatarIds)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction("getAvatar");
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);

                        if (avatarData != null && avatarData.Username == avatarUsername)
                        {
                            var avatar = new Avatar
                            {
                                Id = Guid.Parse(avatarId),
                                Username = avatarData.Username,
                                Email = avatarData.Email,
                                FirstName = avatarData.FirstName,
                                LastName = avatarData.LastName,
                                AvatarType = new EnumValue<AvatarType>(Enum.Parse<AvatarType>(avatarData.AvatarType))
                            };

                            if (!string.IsNullOrEmpty(avatarData.Metadata))
                            {
                                try
                                {
                                    avatar.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(avatarData.Metadata);
                                }
                                catch { }
                            }

                            response.Result = avatar;
                            response.IsError = false;
                            response.Message = "Avatar loaded from Fantom by username successfully";
                            return response;
                        }
                    }
                    catch { continue; }
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar not found with username: {avatarUsername}");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Fantom: {ex.Message}", ex);
            }
            return response;
        }


    }
}
