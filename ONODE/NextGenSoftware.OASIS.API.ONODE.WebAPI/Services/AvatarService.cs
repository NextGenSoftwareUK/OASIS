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
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    //TODO: Want to phase this out, not needed, moving more and more code into AvatarManager.
    [Obsolete("AvatarService is being phased out. Controllers should call AvatarManager directly.")]
    public class AvatarService : IAvatarService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly OASISDNA _OASISDNA;

        public AvatarService(
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
        }

        private AvatarManager AvatarManager => Program.AvatarManager;
        private SearchManager _searchManager = null;

        public SearchManager SearchManager
        {
            get
            {
                if (_searchManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _searchManager = new SearchManager(result.Result);
                }

                return _searchManager;
            }
        }

        public async Task<OASISResult<string>> GetTerms()
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string>();
                try
                {
                    response.Result = _OASISDNA.OASIS.Terms;
                }
                catch (Exception e)
                {
                    response.Exception = e;
                    response.Message = e.Message;
                    response.IsError = true;
                    OASISErrorHandling.HandleError(ref response, e.Message);
                }

                return response;
            });
        }

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

        //private string GetOrigin(string origin)
        //{
        //    if (string.IsNullOrEmpty(origin))
        //        origin = Program.CURRENT_OASISAPI;

        //    return origin;
        //}

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

        public async Task<OASISResult<bool>> VerifyEmail(string token)
        {
            return await Task.Run(() => AvatarManager.VerifyEmail(token));
        }

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

        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitById(Guid id)
        {
            OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

            if (id == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitById. Guid is empty, please speceify a valid Guid.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailAsync(id);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                if (avatarResult.Result.Portrait == null)
                    OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitById. No image has been uploaded for this avatar. Please upload an image first.");
                else
                {
                    result.Result = new AvatarPortrait
                    {
                        AvatarId = avatarResult.Result.Id,
                        Email = avatarResult.Result.Email,
                        Username = avatarResult.Result.Username,
                        ImageBase64 = avatarResult.Result.Portrait
                    };
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarPortraitById loading the avatar detail. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByUsername(string username)
        {
            OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

            if (string.IsNullOrEmpty(username))
            {
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByUsername. username is empty, please speceify a valid username.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                if (avatarResult.Result.Portrait == null)
                    OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByUsername. No image has been uploaded for this avatar. Please upload an image first.");
                else
                {
                    result.Result = new AvatarPortrait
                    {
                        AvatarId = avatarResult.Result.Id,
                        Email = avatarResult.Result.Email,
                        Username = avatarResult.Result.Username,
                        ImageBase64 = avatarResult.Result.Portrait
                    };
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarPortraitByUsername loading the avatar detail. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByEmail(string email)
        {
            OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

            if (string.IsNullOrEmpty(email))
            {
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByEmail. Email is empty, please speceify a valid username.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailByEmailAsync(email);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                if (avatarResult.Result.Portrait == null)
                    OASISErrorHandling.HandleError(ref result, "Error occured in GetAvatarPortraitByEmail. No image has been uploaded for this avatar. Please upload an image first.", avatarResult.DetailedMessage);
                else
                {
                    result.Result = new AvatarPortrait
                    {
                        AvatarId = avatarResult.Result.Id,
                        Email = avatarResult.Result.Email,
                        Username = avatarResult.Result.Username,
                        ImageBase64 = avatarResult.Result.Portrait
                    };
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in email loading the avatar detail. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public async Task<OASISResult<bool>> UploadAvatarPortrait(AvatarPortrait image)
        {
            var response = new OASISResult<bool>();
            OASISResult<IAvatarDetail> avatarResult = null;

            try
            {
                if (image.AvatarId == Guid.Empty && string.IsNullOrEmpty(image.Username) && string.IsNullOrEmpty(image.Email))
                {
                    OASISErrorHandling.HandleError(ref response, "Error occured in UploadAvatarPortrait, you need to specify either the AvatarId, Username or Email of the avatar you wish to upload an image for.");
                    return response;
                }

                if (image.AvatarId != Guid.Empty)
                    avatarResult = await AvatarManager.LoadAvatarDetailAsync(image.AvatarId);

                else if (!string.IsNullOrEmpty(image.Username))
                    avatarResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(image.Username);

                else if (!string.IsNullOrEmpty(image.Email))
                    avatarResult = await AvatarManager.LoadAvatarDetailByEmailAsync(image.Email);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    avatarResult.Result.Portrait = image.ImageBase64;
                    var saveAvatar = AvatarManager.SaveAvatarDetail(avatarResult.Result);

                    if (saveAvatar.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Error occured in UploadAvatarPortrait saving avatar detail. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
                        return response;
                    }

                    response.Message = "Image Uploaded";
                    response.Result = true;
                }
                else
                {
                    response.Result = false;
                    OASISErrorHandling.HandleError(ref response, $"Error occured in UploadAvatarPortrait uploading image. Avatar failed to load, reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                }
               
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref response, e.Message, e);
            }

            return response;
        }

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

        public async Task<OASISResult<IAvatar>> Create(CreateRequest model)
        {
            var result = new OASISResult<IAvatar>();
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!

            OASISResult<IEnumerable<IAvatar>> avatarsResult = await AvatarManager.LoadAllAvatarsAsync();

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                if (avatarsResult.Result.Any(x => x.Email == model.Email))
                    OASISErrorHandling.HandleError(ref result, $"Email '{model.Email}' is already registered");
                else
                {
                    // map model to new account object
                    var avatar = _mapper.Map<IAvatar>(model);
                    avatar.CreatedDate = DateTime.UtcNow;
                    avatar.Verified = DateTime.UtcNow;

                    // hash password
                    avatar.Password = BC.HashPassword(model.Password);
                    var saveAvatarResult = await AvatarManager.SaveAvatarAsync(avatar);

                    if (saveAvatarResult.IsError || saveAvatarResult.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in Create method on AvatarService saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    else
                    {
                        result.Result = AvatarManager.HideAuthDetails(avatar);
                        result.Message = "Avatar Created Successfully";
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Create method on AvatarService loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);

            return result;
        }

        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in Update method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarAsync(id, false, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByEmail(string email, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in UpdateByEmail method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarByEmailAsync(email, false, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByUsername(string username, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in UpdateByUsername method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarAsync(username, false, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;  
        }

        /*
        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response = await AvatarManager.DeleteAvatarAsync(id);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<bool>> DeleteByUsername(string username)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response = await AvatarManager.DeleteAvatarByUsernameAsync(username);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<bool>> DeleteByEmail(string email)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response = await AvatarManager.DeleteAvatarByEmailAsync(email);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }*/

        public async Task<OASISResult<string>> ValidateAccountToken(string accountToken)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string>();
                try
                {
                    var key = Encoding.ASCII.GetBytes(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Security.SecretKey);
                    var tokenHandler = new JwtSecurityTokenHandler();
                    tokenHandler.ValidateToken(accountToken, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    }, out _);
                    response.IsError = false;
                    response.Result = "Token is Valid!";
                }
                catch (Exception e)
                {
                    response.IsError = true;
                    response.Exception = e;
                    response.Message = e.Message;
                    response.Result = "Token Validating Failed: Invalid Token";
                    OASISErrorHandling.HandleError(ref response, e.Message);
                }

                return response;
            });
        }

        //public async Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id)
        //{
        //    var result = new OASISResult<IAvatarDetail>();
        //    var avatar = await AvatarManager.LoadAvatarDetailAsync(id);

        //    if (avatar != null) return result;
        //    result.Message = "AvatarDetail not found";
        //    result.IsError = true;
        //    OASISErrorHandling.HandleError(ref result, result.Message);
        //    return result;
        //}

        //public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        //{
        //    var response = new OASISResult<IAvatarDetail>();
        //    try
        //    {
        //        var entity = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);
        //        response.Result = entity;
        //    }
        //    catch (Exception e)
        //    {
        //        response.IsError = true;
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        OASISErrorHandling.HandleError(ref response, response.Message);
        //    }

        //    return response;
        //}

        //public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        //{
        //    var response = new OASISResult<IAvatarDetail>();
        //    try
        //    {
        //        var entity = await AvatarManager.LoadAvatarDetailByEmailAsync(email);
        //        response.Result = entity;
        //    }
        //    catch (Exception e)
        //    {
        //        response.IsError = true;
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        OASISErrorHandling.HandleError(ref response, e.Message);
        //    }

        //    return response;
        //}

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

        //TODO: Finish moving everything into AvatarManager.
        private string GenerateJwtToken(IAvatar account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_OASISDNA.OASIS.Security.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {new Claim("id", account.Id.ToString())}),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new()
            {
                Token = AvatarManager.RandomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

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

        // Avatar Session Management Implementation
        public async Task<OASISResult<Models.Avatar.AvatarSessionManagement>> GetAvatarSessionsAsync(Guid avatarId)
        {
            var response = new OASISResult<Models.Avatar.AvatarSessionManagement>();
            
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

                // Map from Core type to WebAPI.Models type (they should have same structure)
                response.Result = sessionsResult.Result as Models.Avatar.AvatarSessionManagement;
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

        public async Task<OASISResult<Models.Avatar.AvatarSession>> CreateAvatarSessionAsync(Guid avatarId, Models.Avatar.CreateSessionRequest request)
        {
            var response = new OASISResult<Models.Avatar.AvatarSession>();
            
            try
            {
                // Convert WebAPI.Models request to Core.Objects request
                var coreRequest = request as NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest 
                    ?? new NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest();
                
                var result = await AvatarManager.CreateAvatarSessionAsync(avatarId, coreRequest);
                
                response.Result = result.Result as Models.Avatar.AvatarSession;
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

        public async Task<OASISResult<Models.Avatar.AvatarSession>> UpdateAvatarSessionAsync(Guid avatarId, string sessionId, Models.Avatar.UpdateSessionRequest request)
        {
            var response = new OASISResult<Models.Avatar.AvatarSession>();
            
            try
            {
                // Convert WebAPI.Models request to Core.Objects request
                var coreRequest = request as NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest
                    ?? new NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest();
                
                var result = await AvatarManager.UpdateAvatarSessionAsync(avatarId, sessionId, coreRequest);
                
                response.Result = result.Result as Models.Avatar.AvatarSession;
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

        public async Task<OASISResult<Models.Avatar.AvatarSessionStats>> GetAvatarSessionStatsAsync(Guid avatarId)
        {
            var response = new OASISResult<Models.Avatar.AvatarSessionStats>();
            
            try
            {
                var result = await AvatarManager.GetAvatarSessionStatsAsync(avatarId);
                
                response.Result = result.Result as Models.Avatar.AvatarSessionStats;
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
    }
}