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
        public async Task<OASISResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var response = new OASISResult<AuthenticateResponse>();

            try
            {
                var result = await AvatarManager.AuthenticateAsync(model.Email, model.Password, ipAddress);

                if (result.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, result.Message);
                    return response;
                }

                response.Message = "Avatar Successfully Authenticated.";
                response.Result = new AuthenticateResponse { Message = response.Message, Avatar = result.Result };
            }
            catch (Exception e)
            {
                response.Exception = e;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }*/

        //public async Task<OASISResult<IAvatar>> Authenticate(AuthenticateRequest model, string ipAddress)
        //{
        //    var response = new OASISResult<IAvatar>();

        //    try
        //    {
        //        response = await AvatarManager.AuthenticateAsync(model.Email, model.Password, ipAddress);

        //        if (response.IsError)
        //        {
        //            OASISErrorHandling.HandleError(ref response, response.Message);
        //            return response;
        //        }

        //        response.Message = "Avatar Successfully Authenticated.";
        //    }
        //    catch (Exception e)
        //    {
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        response.IsError = true;
        //        OASISErrorHandling.HandleError(ref response, e.Message);
        //    }
        //    return response;
        //}

        // MIGRATED — AvatarManager.RefreshToken — AvatarController calls AvatarManager.RefreshToken directly
        /*
        public async Task<OASISResult<IAvatar>> RefreshToken(string token, string ipAddress)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<IAvatar>();

                try
                {
                    var (refreshTokenResult, avatar) = GetRefreshToken(token);

                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found");
                        return response;
                    }

                    if (refreshTokenResult != null && !refreshTokenResult.IsError && refreshTokenResult.Result != null)
                    {
                        var newRefreshToken = GenerateRefreshToken(ipAddress);
                        refreshTokenResult.Result.Revoked = DateTime.UtcNow;
                        refreshTokenResult.Result.RevokedByIp = ipAddress;
                        refreshTokenResult.Result.ReplacedByToken = newRefreshToken.Token;
                        avatar.RefreshTokens.Add(newRefreshToken);

                        avatar.RefreshToken = newRefreshToken.Token;
                        avatar.JwtToken = GenerateJwtToken(avatar);

                        OASISResult<IAvatar> saveAvatarResult = AvatarManager.SaveAvatar(avatar);

                        if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                        {
                            avatar = AvatarManager.HideAuthDetails(saveAvatarResult.Result);
                            response.Result = avatar;
                        }
                        else
                            OASISErrorHandling.HandleError(ref response, $"Error occured in RefreshToken method in AvatarService saving avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, $"Error occured in RefreshToken method in AvatarService getting refresh token. Reason: {refreshTokenResult.Message}", refreshTokenResult.DetailedMessage);
                }
                catch (Exception ex)
                {
                    response.Exception = ex;
                    OASISErrorHandling.HandleError(ref response, $"An unknown error occured in RefreshToken method in AvatarService. Reason: {ex.Message}");
                }

                return response;
            });
        }
        */

        // MIGRATED — AvatarManager.RevokeToken — AvatarController calls AvatarManager.RevokeToken directly
        /*
        public async Task<OASISResult<string>> RevokeToken(string token, string ipAddress)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string>();
                var (refreshTokenResult, avatar) = GetRefreshToken(token);

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found");
                    return response;
                }

                // revoke token and save
                if (!refreshTokenResult.IsError && refreshTokenResult.Result != null)
                {
                    refreshTokenResult.Result.Revoked = DateTime.UtcNow;
                    refreshTokenResult.Result.RevokedByIp = ipAddress;
                    avatar.IsBeamedIn = false;
                    avatar.LastBeamedOut = DateTime.Now;

                    var saveAvatar = AvatarManager.SaveAvatar(avatar);

                    if (saveAvatar != null && !saveAvatar.IsError && saveAvatar.Result != null)
                    {
                        response.Message = "Token Revoked.";
                        response.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, $"An error in RevokeToken method in AvatarService saving the avatar. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref response, $"An error occured in RevokeToken method in AvatarService. Reason: {refreshTokenResult.Message}", refreshTokenResult.DetailedMessage);

                return response;
            });
        }
        */

        // MIGRATED — AvatarController calls AvatarManager.RegisterAsync/Register directly
        /*
        public async Task<OASISResult<IAvatar>> RegisterAsync(RegisterRequest model, string origin)
        {
            var result = PrepareToRegister(model);

            if (!result.IsError)
            {
                //origin = GetOrigin(origin);

                result = await AvatarManager.RegisterAsync(model.Title, model.FirstName, model.LastName, model.Email, model.Password, model.Username,
                    (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType), model.CreatedOASISType);
            }

            return result;
        }

        public OASISResult<IAvatar> Register(RegisterRequest model, string origin)
        {
            var result = PrepareToRegister(model);

            if (!result.IsError)
            {
                //origin = GetOrigin(origin);

                result = AvatarManager.Register(model.Title, model.FirstName, model.LastName, model.Email, model.Password, model.Username,
                    (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType), model.CreatedOASISType);
            }

            return result;
        }
        */

        //private string GetOrigin(string origin)
        //{
        //    if (string.IsNullOrEmpty(origin))
        //        origin = Program.CURRENT_OASISAPI;

        //    return origin;
        //}

        // MIGRATED — helper for RegisterAsync/Register (both now commented out above)
        /*
        private OASISResult<IAvatar> PrepareToRegister(RegisterRequest model)
        {
            var result = new OASISResult<IAvatar>();

            if (!Enum.TryParse(typeof(AvatarType), model.AvatarType, out _))
            {
                result.Message = string.Concat(
                    "ERROR: AvatarType needs to be one of the values found in AvatarType enumeration. Possible value can be:\n\n",
                    EnumHelper.GetEnumValues(typeof(AvatarType)));

                result.IsError = true;
                result.IsSaved = false;
                OASISErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            return result;
        }
        */

        // MIGRATED — AvatarController calls AvatarManager.VerifyEmail directly
        //public async Task<OASISResult<bool>> VerifyEmail(string token)
        //{
        //    return await Task.Run(() => AvatarManager.VerifyEmail(token));
        //}

        // MIGRATED — AvatarController calls AvatarManager.ValidateResetToken directly
        /*
        public async Task<OASISResult<string>> ValidateResetToken(ValidateResetTokenRequest model)
        {
            var result = new OASISResult<string>();
            try
            {
                //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                OASISResult<IEnumerable<IAvatar>> avatarsResult = await AvatarManager.LoadAllAvatarsAsync();

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    var avatar = avatarsResult.Result.FirstOrDefault(x =>
                        x.ResetToken == model.Token &&
                        x.ResetTokenExpires > DateTime.UtcNow);

                    if (avatar == null)
                        OASISErrorHandling.HandleError(ref result, "Invalid token");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in ValidateResetToken loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in ValidateResetToken. Reason: {e}", e);
            }

            return result;
        }
        */

        //public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model)
        //{
        //    var response = new OASISResult<string>();

        //    try
        //    {
        //        OASISResult<IEnumerable<IAvatar>> avatarsResult = await AvatarManager.LoadAllAvatarsAsync(false, false);

        //        if (!avatarsResult.IsError && avatarsResult.Result != null)
        //        {
        //            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
        //            var avatar = avatarsResult.Result.FirstOrDefault(x =>
        //                x.ResetToken == model.Token &&
        //                x.ResetTokenExpires > DateTime.UtcNow);

        //            if (avatar == null)
        //            {
        //                OASISErrorHandling.HandleError(ref response, "Avatar Not Found");
        //                return response;
        //            }

        //            //int salt = 12;
        //            //string passwordHash = BC.HashPassword(model.OldPassword, salt);

        //            //if (!BC.Verify(avatar.Password, passwordHash))
        //            //{
        //            //    OASISErrorHandling.HandleError(ref response, "Old Password Is Not Correct");
        //            //    return response;
        //            //}

        //            // update password and remove reset token
        //            avatar.Password = BC.HashPassword(model.NewPassword);
        //            avatar.PasswordReset = DateTime.UtcNow;
        //            avatar.ResetToken = null;
        //            avatar.ResetTokenExpires = null;

        //            var saveAvatarResult = AvatarManager.SaveAvatar(avatar);

        //            if (saveAvatarResult.IsError)
        //            {
        //                OASISErrorHandling.HandleError(ref saveAvatarResult, $"Error occured in ResetPassword saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
        //                return response;
        //            }

        //            response.Message = "Password reset successful, you can now login";
        //            response.Result = response.Message;
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref response, $"Error occured in ResetPassword loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
        //    }
        //    catch (Exception e)
        //    {
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        response.IsError = true;
        //        response.IsSaved = false;
        //        OASISErrorHandling.HandleError(ref response, e.Message);
        //    }

        //    return response;
        //}

        // MIGRATED TO AvatarManager.GetAvatarPortraitByIdAsync — see AvatarManager-Portrait.cs
        //public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitById(Guid id)
        //{
        //    OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

        //    if (id == Guid.Empty)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitById. Guid is empty, please speceify a valid Guid.");
        //        return result;
        //    }

        //    OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailAsync(id);

        //    if (!avatarResult.IsError && avatarResult.Result != null)
        //    {
        //        if (avatarResult.Result.Portrait == null)
        //            OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitById. No image has been uploaded for this avatar. Please upload an image first.");
        //        else
        //        {
        //            result.Result = new AvatarPortrait
        //            {
        //                AvatarId = avatarResult.Result.Id,
        //                Email = avatarResult.Result.Email,
        //                Username = avatarResult.Result.Username,
        //                ImageBase64 = avatarResult.Result.Portrait
        //            };
        //        }
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarPortraitById loading the avatar detail. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

        //    return result;
        //}

        // MIGRATED TO AvatarManager.GetAvatarPortraitByUsernameAsync — see AvatarManager-Portrait.cs
        //public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByUsername(string username)
        //{
        //    OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

        //    if (string.IsNullOrEmpty(username))
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByUsername. username is empty, please speceify a valid username.");
        //        return result;
        //    }

        //    OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);

        //    if (!avatarResult.IsError && avatarResult.Result != null)
        //    {
        //        if (avatarResult.Result.Portrait == null)
        //            OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByUsername. No image has been uploaded for this avatar. Please upload an image first.");
        //        else
        //        {
        //            result.Result = new AvatarPortrait
        //            {
        //                AvatarId = avatarResult.Result.Id,
        //                Email = avatarResult.Result.Email,
        //                Username = avatarResult.Result.Username,
        //                ImageBase64 = avatarResult.Result.Portrait
        //            };
        //        }
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarPortraitByUsername loading the avatar detail. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

        //    return result;
        //}

        // MIGRATED TO AvatarManager.GetAvatarPortraitByEmailAsync — see AvatarManager-Portrait.cs
        //public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByEmail(string email)
        //{
        //    OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

        //    if (string.IsNullOrEmpty(email))
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByEmail. Email is empty, please speceify a valid username.");
        //        return result;
        //    }

        //    OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailByEmailAsync(email);

        //    if (!avatarResult.IsError && avatarResult.Result != null)
        //    {
        //        if (avatarResult.Result.Portrait == null)
        //            OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByEmail. No image has been uploaded for this avatar. Please upload an image first.", avatarResult.DetailedMessage);
        //        else
        //        {
        //            result.Result = new AvatarPortrait
        //            {
        //                AvatarId = avatarResult.Result.Id,
        //                Email = avatarResult.Result.Email,
        //                Username = avatarResult.Result.Username,
        //                ImageBase64 = avatarResult.Result.Portrait
        //            };
        //        }
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in email loading the avatar detail. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

        //    return result;
        //}

        // MIGRATED TO AvatarManager.UploadAvatarPortraitAsync — see AvatarManager-Portrait.cs
        //public async Task<OASISResult<bool>> UploadAvatarPortrait(AvatarPortrait image)
        //{
        //    var response = new OASISResult<bool>();
        //    OASISResult<IAvatarDetail> avatarResult = null;

        //    try
        //    {
        //        if (image.AvatarId == Guid.Empty && string.IsNullOrEmpty(image.Username) && string.IsNullOrEmpty(image.Email))
        //        {
        //            OASISErrorHandling.HandleError(ref response, "Error occured in UploadAvatarPortrait, you need to specify either the AvatarId, Username or Email of the avatar you wish to upload an image for.");
        //            return response;
        //        }

        //        if (image.AvatarId != Guid.Empty)
        //            avatarResult = await AvatarManager.LoadAvatarDetailAsync(image.AvatarId);

        //        else if (!string.IsNullOrEmpty(image.Username))
        //            avatarResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(image.Username);

        //        else if (!string.IsNullOrEmpty(image.Email))
        //            avatarResult = await AvatarManager.LoadAvatarDetailByEmailAsync(image.Email);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            avatarResult.Result.Portrait = image.ImageBase64;
        //            var saveAvatar = AvatarManager.SaveAvatarDetail(avatarResult.Result);

        //            if (saveAvatar.IsError)
        //            {
        //                OASISErrorHandling.HandleError(ref response, $"Error occured in UploadAvatarPortrait saving avatar detail. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
        //                return response;
        //            }

        //            response.Message = "Image Uploaded";
        //            response.Result = true;
        //        }
        //        else
        //        {
        //            response.Result = false;
        //            OASISErrorHandling.HandleError(ref response, $"Error occured in UploadAvatarPortrait uploading image. Avatar failed to load, reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref response, e.Message, e);
        //    }

        //    return response;
        //}

        //public async Task<OASISResult<IAvatar>> GetById(Guid id)
        //{
        //    return await GetAvatar(id);
        //}

        //public async Task<OASISResult<IAvatar>> GetByUsername(string userName)
        //{
        //    var response = new OASISResult<IAvatar>();

        //    try
        //    {
        //        if (string.IsNullOrEmpty(userName))
        //        {
        //            OASISErrorHandling.HandleError(ref response, "Error in GetByUsername, UserName property is empty");
        //            return response;
        //        }

        //        response = await AvatarManager.LoadAvatarAsync(userName);
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref response, e.Message, true, false, false, false, true, e);
        //    }

        //    return response;
        //}

        //public async Task<OASISResult<IAvatar>> GetByEmail(string email)
        //{
        //    var response = new OASISResult<IAvatar>();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(email))
        //        {
        //            OASISErrorHandling.HandleError(ref response, "Error in GetByEmail, Email property is empty");
        //            return response;
        //        }

        //        response = await AvatarManager.LoadAvatarByEmailAsync(email);
        //    }
        //    catch (Exception e)
        //    {
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        response.IsError = true;
        //        OASISErrorHandling.HandleError(ref response, response.Message);
        //    }

        //    return response;
        //}

    }
}
