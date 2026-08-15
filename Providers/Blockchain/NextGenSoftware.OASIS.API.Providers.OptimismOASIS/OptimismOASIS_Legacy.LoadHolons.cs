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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS
{
    public partial class OptimismOASIS_Legacy
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
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Optimism implementation: Search through user avatars to find one with matching email
                var getUserAvatarsFunction = _contract.GetFunction("getUserAvatars");
                var userAvatars = await getUserAvatarsFunction.CallAsync<List<string>>(_account.Address);

                foreach (var avatarId in userAvatars)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction("getAvatar");
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);

                        if (avatarData != null && avatarData.Email == email)
                        {
                            // Found matching avatar - convert to IAvatar
                            var avatar = new Avatar
                            {
                                Id = Guid.Parse(avatarId),
                                Username = avatarData.Username,
                                Email = avatarData.Email,
                                FirstName = avatarData.FirstName,
                                LastName = avatarData.LastName,
                                AvatarType = new EnumValue<AvatarType>(Enum.Parse<AvatarType>(avatarData.AvatarType))
                            };

                            // Parse metadata if available
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
                            response.Message = "Avatar loaded successfully from Optimism by email";
                            return response;
                        }
                    }
                    catch { continue; }
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar with email {email} not found on Optimism");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByEmailAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByEmail is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByEmail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByEmailAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailByEmailAsync: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByUsername is not supported by Optimism provider");
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
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetailByUsernameAsync is not supported by Optimism provider");
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
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatar is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatar: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByEmail is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByEmail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByEmailAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByEmailAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByUsername is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByUsername: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarByUsernameAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarByUsernameAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatar is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatar: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteAvatarAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarAsync: {ex.Message}");
            }
            return response;
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
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Search all user avatars and filter by username
                var getUserAvatarsFunction = _contract.GetFunction("getUserAvatars");
                var userAvatars = await getUserAvatarsFunction.CallAsync<List<string>>(_account.Address);

                foreach (var avatarId in userAvatars)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction("getAvatar");
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);

                        if (avatarData != null && string.Equals(avatarData.Username, avatarUsername, StringComparison.OrdinalIgnoreCase))
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
                                try { avatar.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(avatarData.Metadata); }
                                catch { }
                            }

                            response.Result = avatar;
                            response.IsError = false;
                            response.Message = "Avatar loaded successfully from Optimism by username";
                            return response;
                        }
                    }
                    catch { continue; }
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar with username '{avatarUsername}' not found on Optimism");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByUsernameAsync: {ex.Message}", ex);
            }
            return response;
        }


        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Optimism implementation: Load avatar by provider key (avatarId)
                // Provider key is the avatar ID string
                try
                {
                    var getAvatarFunction = _contract.GetFunction("getAvatar");
                    var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(providerKey);

                    if (avatarData != null)
                    {
                        // Found avatar - convert to IAvatar
                        var avatar = new Avatar
                        {
                            Id = Guid.Parse(providerKey),
                            Username = avatarData.Username,
                            Email = avatarData.Email,
                            FirstName = avatarData.FirstName,
                            LastName = avatarData.LastName,
                            AvatarType = new EnumValue<AvatarType>(Enum.Parse<AvatarType>(avatarData.AvatarType))
                        };

                        // Parse metadata if available
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
                        response.Message = "Avatar loaded successfully from Optimism by provider key";
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    // Avatar might not exist - check if it's a "does not exist" error
                    if (ex.Message.Contains("does not exist") || ex.Message.Contains("Avatar does not exist"))
                    {
                        OASISErrorHandling.HandleError(ref response, $"Avatar with provider key {providerKey} not found on Optimism");
                        return response;
                    }
                    throw;
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar with provider key {providerKey} not found on Optimism");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByProviderKeyAsync: {ex.Message}", ex);
            }
            return response;
        }

    }
}
