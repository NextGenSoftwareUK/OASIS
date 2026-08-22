using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public partial class AvatarService
    {
        /*
        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();

            try
            {
                response = await AvatarManager.LoadAllAvatarDetailsAsync();
            }
            catch (Exception e)
            {
                response.Message = e.Message;
                response.Exception = e;
                response.IsError = true;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }*/

        // MIGRATED TO AvatarManager.GetAvatarUmaJsonByIdAsync/ByUsernameAsync/ByEmailAsync — see AvatarManager-Portrait.cs
        // AvatarController calls AvatarManager directly for all UMA JSON endpoints
        /*
        public async Task<OASISResult<string>> GetAvatarUmaJsonById(Guid id)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (id == Guid.Empty)
                    OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarUmaJsonById. AvatarId is empty");

                OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailAsync(id);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUmaJsonById loading avatar detail. Reason:{avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in GetAvatarUmaJsonById. Details: {e}", e);
            }

            return result;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonByUsername(string username)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrEmpty(username))
                    OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarUmaJsonByUsername. username is empty");

                OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUmaJsonByUsername loading avatar detail. Reason:{avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in GetAvatarUmaJsonByUsername. Details: {e}", e);
            }

            return result;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonByEmail(string email)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrEmpty(email))
                    OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarUmaJsonByEmail. email is empty");

                OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailByEmailAsync(email);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUmaJsonByEmail loading avatar detail. Reason:{avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in GetAvatarUmaJsonByEmail. Details: {e}", e);
            }

            return result;
        }
        */

        //TODO: Check this works?!
        public async Task<OASISResult<IAvatar>> GetLoggedInAvatar()
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<IAvatar>();
                try
                {
                    if (_httpContextAccessor.HttpContext == null)
                    {
                        response.Result = null;
                        OASISErrorHandling.HandleError(ref response, "Avatar not found.");
                        return response;
                    }

                    var avatar = (IAvatar) _httpContextAccessor.HttpContext.Items["Avatar"];

                    if (avatar == null)
                    {
                        response.Result = null;
                        OASISErrorHandling.HandleError(ref response, "Avatar not found");
                        return response;
                    }

                    response.Result = avatar;
                }
                catch (Exception e)
                {
                    response.Result = null;
                    OASISErrorHandling.HandleError(ref response, $"An unknown error occured in GetAvatarByJwt. Reason: {e.Message}");
                }

                return response;
            });
        }

        //public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams)
        //{
        //    var response = new OASISResult<ISearchResults>();

        //    try
        //    {
        //        searchParams.SearchAvatarsOnly = true;

        //        if (string.IsNullOrEmpty(searchParams.SearchQuery))
        //            OASISErrorHandling.HandleError(ref response, "SearchQuery field is empty");
        //        else
        //            response = await SearchManager.SearchAsync(searchParams);
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref response, $"Unknown error occured in Search method in AvatarService. Reason: {e}", e);
        //    }

        //    return response;
        //}

        //public async Task<OASISResult<bool>> LinkProviderKeyToAvatar(Guid avatarId, ProviderType providerType, string key)
        //{
        //    return await Task.Run(() =>
        //    {
        //        var response = new OASISResult<bool>();

        //        try
        //        {
        //            response = AvatarManager.LinkProviderKeyToAvatar(avatarId, providerType, key);
        //        }
        //        catch (Exception e)
        //        {
        //            OASISErrorHandling.HandleError(ref response, $"Unknown error occured in LinkProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerType)} and key {key}: {e.Message}");
        //        }
        //        return response;
        //    });
        //}

        //public async Task<OASISResult<bool>> LinkPrivateProviderKeyToAvatar(Guid avatarId, ProviderType providerType, string key)
        //{
        //    return await Task.Run(() =>
        //    {
        //        var response = new OASISResult<bool>();

        //        try
        //        {
        //            response = AvatarManager.LinkPrivateProviderKeyToAvatar(avatarId, providerType, key);
        //        }
        //        catch (Exception e)
        //        {
        //            OASISErrorHandling.HandleError(ref response, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerType)} and key {key}: {e.Message}");
        //        }
        //        return response;
        //    });
        //}

        /*
        public async Task<OASISResult<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string>();

                try
                {
                    response.Result = AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref response, $"Unknown error occured in GetProviderKeyForAvatar for avatar {avatarUsername} and providerType {Enum.GetName(typeof(ProviderType), providerType)}: {e.Message}");
                }

                return response;
            });
        }

        public async Task<OASISResult<string>> GetPrivateProviderKeyForAvatar(Guid avatarId, ProviderType providerType)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string>();

                try
                {
                    response.Result = AvatarManager.GetPrivateProviderKeyForAvatar(avatarId, providerType);
                }
                catch (Exception e)
                {
                    OASISErrorHandling.HandleError(ref response, $"Unknown error occured in GetPrivateProviderKeyForAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerType)}: {e.Message}");
                }

                return response;
            });
        }
        */

        // MIGRATED — AvatarController calls AvatarManager.AddKarmaToAvatarAsync/RemoveKarmaFromAvatarAsync directly (AvatarManager-Karma.cs)
        /*
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addRemoveKarmaToAvatarRequest)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<KarmaAkashicRecord>();
                try
                {
                    object karmaTypePositiveObject = null;
                    object karmaSourceTypeObject = null;

                    if (!Enum.TryParse(typeof(KarmaTypePositive), addRemoveKarmaToAvatarRequest.KarmaType,
                        out karmaTypePositiveObject))
                    {
                        response.IsError = true;
                        response.IsSaved = false;
                        response.Message = string.Concat(
                            "ERROR: KarmaType needs to be one of the values found in KarmaTypePositive enumeration. Possible value can be:\n\n",
                            EnumHelper.GetEnumValues(typeof(KarmaTypePositive)));
                        OASISErrorHandling.HandleError(ref response, response.Message);
                    }

                    if (!Enum.TryParse(typeof(KarmaSourceType), addRemoveKarmaToAvatarRequest.karmaSourceType,
                        out karmaSourceTypeObject))
                    {
                        response.IsError = true;
                        response.IsSaved = false;
                        response.Message = string.Concat(
                            "ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n",
                            EnumHelper.GetEnumValues(typeof(KarmaSourceType)));
                        OASISErrorHandling.HandleError(ref response, response.Message);
                    }

                    //response.Result = AvatarManager.AddKarmaToAvatar(avatarId,
                    //    (KarmaTypePositive) karmaTypePositiveObject,
                    //    (KarmaSourceType) karmaSourceTypeObject, addRemoveKarmaToAvatarRequest.KaramSourceTitle,
                    //    addRemoveKarmaToAvatarRequest.KarmaSourceDesc).Result;

                    response = AvatarManager.AddKarmaToAvatar(avatarId,
                       (KarmaTypePositive)karmaTypePositiveObject,
                       (KarmaSourceType)karmaSourceTypeObject, addRemoveKarmaToAvatarRequest.KaramSourceTitle,
                       addRemoveKarmaToAvatarRequest.KarmaSourceDesc);
                }
                catch (Exception e)
                {
                    response.Exception = e;
                    response.Message = e.Message;
                    response.IsError = true;
                    response.IsSaved = false;
                    OASISErrorHandling.HandleError(ref response, e.Message);
                }

                return response;
            });
        }

        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            var response = new OASISResult<KarmaAkashicRecord>();
            try
            {
                object karmaTypeNegativeObject = null;
                object karmaSourceTypeObject = null;

                if (!Enum.TryParse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType, out karmaTypeNegativeObject))
                {
                    response.Message = string.Concat(
                        "ERROR: KarmaType needs to be one of the values found in KarmaTypeNegative enumeration. Possible value can be:\n\n",
                        EnumHelper.GetEnumValues(typeof(KarmaTypeNegative)));
                    response.IsError = true;
                    response.IsSaved = false;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                {
                    response.Message = string.Concat(
                        "ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n",
                        EnumHelper.GetEnumValues(typeof(KarmaSourceType)));
                    response.IsError = true;
                    response.IsSaved = false;
                    OASISErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                //response.Result = AvatarManager.RemoveKarmaFromAvatar(avatarId, (KarmaTypeNegative) karmaTypeNegativeObject,
                //    (KarmaSourceType) karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle,
                //    addKarmaToAvatarRequest.KarmaSourceDesc).Result;

                response = AvatarManager.RemoveKarmaFromAvatar(avatarId, (KarmaTypeNegative)karmaTypeNegativeObject,
                    (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle,
                    addKarmaToAvatarRequest.KarmaSourceDesc);
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }
        */

        //private async Task<OASISResult<IAvatar>> GetAvatar(Guid id, bool internalUse = false)
        //{
        //    var result = await AvatarManager.LoadAvatarAsync(id, internalUse);

        //    if (!internalUse)
        //        avatar = AvatarManager.RemoveAuthDetails(avatar);

        //    return result;
        //}

        //private (RefreshToken, IAvatar) GetRefreshToken(string token)
        //{
        //    //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
        //    var avatar = AvatarManager.LoadAllAvatarsWithPasswords()
        //        .FirstOrDefault(x => x.RefreshTokens.Any(t => t.Token == token));

        //    if (avatar == null)
        //        throw new AppException("Invalid token");

        //    var refreshToken = avatar.RefreshTokens.Single(x => x.Token == token);

        //    if (!refreshToken.IsActive)
        //        throw new AppException("Invalid token");

        //    return (refreshToken, avatar);
        //}

        // MIGRATED — private helper for Update/UpdateByEmail/UpdateByUsername (all now commented out above)
        /*
        private async Task<OASISResult<IAvatar>> Update(IAvatar originalAvatar, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in Update method in Avatar Service. Reason: ";

            try
            {
                // only admins can update role
                if (avatar.AvatarType != "Wizard")
                    avatar.AvatarType = null;

                if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != originalAvatar.Email &&
                    (await AvatarManager.LoadAvatarByEmailAsync(avatar.Email, false, false)).Result != null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Email '{avatar.Email}' is already taken");
                    return response;
                }

                // hash password if it was entered
                if (!string.IsNullOrEmpty(avatar.Password))
                    avatar.Password = BC.HashPassword(avatar.Password);

                //TODO: Fix this. Can't remember what needs fixing? But we need to be able to update ANY avatar property....
                _mapper.Map(avatar, originalAvatar);
                originalAvatar.ModifiedDate = DateTime.UtcNow;

                var saveAvatarResult = AvatarManager.SaveAvatar(originalAvatar);

                if (!saveAvatarResult.IsError && saveAvatarResult.Result != null)
                {
                    OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailAsync(originalAvatar.Id);

                    if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    {
                        avatarDetailResult.Result.Username = originalAvatar.Username;
                        avatarDetailResult.Result.Email = originalAvatar.Email;

                        OASISResult<IAvatarDetail> saveAvatarDetailResult = await avatarDetailResult.Result.SaveAsync();

                        if (!saveAvatarDetailResult.IsError && saveAvatarDetailResult.Result != null)
                        {
                            response.IsSaved = true;
                            response.SavedCount = 1;
                            response.Message = "Avatar Successfully Updated";
                            response.Result = AvatarManager.HideAuthDetails(saveAvatarResult.Result);
                        }
                        else
                            OASISErrorHandling.HandleError(ref response, $"{errorMessage}{saveAvatarDetailResult.Message}", saveAvatarDetailResult.DetailedMessage);
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, $"{errorMessage}{avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }
        */

        // MIGRATED — private helper for RefreshToken (now commented out above); AvatarManager has its own equivalent
        /*
        private (OASISResult<RefreshToken>, IAvatar) GetRefreshToken(string token)
        {
            OASISResult<RefreshToken> result = new OASISResult<RefreshToken>();

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list
            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(false, false);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.RefreshTokens.Any(t => t.Token == token));

                if (avatar == null)
                {
                    result.Message = "Invalid Token";
                    return (result, avatar);
                }

                var refreshToken = avatar.RefreshTokens.Single(x => x.Token == token);

                if (!refreshToken.IsActive)
                {
                    result.Message = "Invalid Token";
                    return (result, avatar);
                }


                result.Result = refreshToken;
                return (result, avatar);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error in GetRefreshToken loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);

            return (result, null);
        }

        // MIGRATED — private helper for RefreshToken; AvatarManager.GenerateJwtToken handles this
        private string GenerateJwtToken(IAvatar account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_OASISDNA.OASIS.Security.SecretKey);
            var jwtMinutes = _OASISDNA.OASIS.Security.JwtTokenExpirationMinutes;
            if (jwtMinutes <= 0) jwtMinutes = 15;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {new Claim("id", account.Id.ToString())}),
                Expires = DateTime.UtcNow.AddMinutes(jwtMinutes),
                Issuer = "OASIS",
                Audience = "OASIS",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // MIGRATED — private helper for RefreshToken; AvatarManager.GenerateRefreshToken handles this
        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var refreshDays = _OASISDNA.OASIS.Security.RefreshTokenExpirationDays;
            if (refreshDays <= 0) refreshDays = 7;
            return new()
            {
                Token = AvatarManager.RandomTokenString(),
                Expires = DateTime.UtcNow.AddDays(refreshDays),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }
        */

        //private IAvatar RemoveAuthDetails(IAvatar avatar)
        //{
        //    avatar.VerificationToken = null; //TODO: Put back in when LIVE!
        //    avatar.Password = null;
        //    return avatar;
        //}

        //private string RandomTokenString()
        //{
        //    using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
        //    var randomBytes = new byte[40];
        //    rngCryptoServiceProvider.GetBytes(randomBytes);
        //    // convert random bytes to hex string
        //    return BitConverter.ToString(randomBytes).Replace("-", "");
        //}

        // MIGRATED — Avatar Session Management — AvatarController calls AvatarManager session methods directly
        /*
        public async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>> GetAvatarSessionsAsync(Guid avatarId)
        {
            var response = new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>();

            try
            {
                var avatarResult = await AvatarManager.LoadAvatarAsync(avatarId);

                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    response.IsError = true;
                    response.Message = $"Error loading avatar: {avatarResult.Message}";
                    return response;
                }

                // Use AvatarManager session methods - returns Core type, we use WebAPI.Models type in service
                var sessionsResult = await AvatarManager.GetAvatarSessionsAsync(avatarId);

                if (sessionsResult.IsError)
                {
                    response.IsError = true;
                    response.Message = sessionsResult.Message;
                    return response;
                }

                // AvatarService is being phased out; result mapping handled by callers.
                // response.Result = sessionsResult.Result as NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement;
                response.IsSaved = true;
                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"Error getting avatar sessions: {ex.Message}";
                response.Exception = ex;
                return response;
            }
        }

        public async Task<OASISResult<bool>> LogoutAvatarSessionsAsync(Guid avatarId, System.Collections.Generic.List<string> sessionIds)
        {
            var response = new OASISResult<bool>();

            try
            {
                var result = await AvatarManager.LogoutAvatarSessionsAsync(avatarId, sessionIds);

                response.Result = !result.IsError;
                response.IsError = result.IsError;
                response.Message = result.Message;
                response.IsSaved = !result.IsError;

                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"Error logging out sessions: {ex.Message}";
                response.Exception = ex;
                return response;
            }
        }

        public async Task<OASISResult<bool>> LogoutAllAvatarSessionsAsync(Guid avatarId)
        {
            var response = new OASISResult<bool>();

            try
            {
                var result = await AvatarManager.LogoutAllAvatarSessionsAsync(avatarId);

                response.Result = !result.IsError;
                response.IsError = result.IsError;
                response.Message = result.Message;
                response.IsSaved = !result.IsError;

                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"Error logging out all sessions: {ex.Message}";
                response.Exception = ex;
                return response;
            }
        }

        public async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> CreateAvatarSessionAsync(Guid avatarId, NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest request)
        {
            var response = new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>();

            try
            {
                // Request is already Core.Objects type

                var result = await AvatarManager.CreateAvatarSessionAsync(avatarId, request);

                // AvatarService is being phased out; result mapping handled by callers.
                // response.Result = result.Result as NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession;
                response.IsError = result.IsError;
                response.Message = result.Message;
                response.IsSaved = !result.IsError;

                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"Error creating session: {ex.Message}";
                response.Exception = ex;
                return response;
            }
        }

        public async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> UpdateAvatarSessionAsync(Guid avatarId, string sessionId, NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest request)
        {
            var response = new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>();

            try
            {
                // Request is already Core.Objects type

                var result = await AvatarManager.UpdateAvatarSessionAsync(avatarId, sessionId, request);

                // AvatarService is being phased out; result mapping handled by callers.
                // response.Result = result.Result as NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession;
                response.IsError = result.IsError;
                response.Message = result.Message;
                response.IsSaved = !result.IsError;

                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"Error updating session: {ex.Message}";
                response.Exception = ex;
                return response;
            }
        }

        public async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>> GetAvatarSessionStatsAsync(Guid avatarId)
        {
            var response = new OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>();

            try
            {
                var result = await AvatarManager.GetAvatarSessionStatsAsync(avatarId);

                // AvatarService is being phased out; result mapping handled by callers.
                // response.Result = result.Result as NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats;
                response.IsError = result.IsError;
                response.Message = result.Message;
                response.IsSaved = !result.IsError;

                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"Error getting session stats: {ex.Message}";
                response.Exception = ex;
                return response;
            }
        }
        */
    }
}
