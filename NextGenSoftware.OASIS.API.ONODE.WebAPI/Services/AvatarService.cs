using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using BC = BCrypt.Net.BCrypt;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    //TODO: Want to phase this out, now needed, moving more and more code into AvatarManager.
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
                    ErrorHandling.HandleError(ref response, e.Message);
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
                    ErrorHandling.HandleError(ref response, result.Message);
                    return response;
                }

                response.Message = "Avatar Successfully Authenticated.";
                response.Result = new AuthenticateResponse { Message = response.Message, Avatar = result.Result };
            }
            catch (Exception e)
            {
                response.Exception = e;
                ErrorHandling.HandleError(ref response, e.Message);
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
        //            ErrorHandling.HandleError(ref response, response.Message);
        //            return response;
        //        }

        //        response.Message = "Avatar Successfully Authenticated.";
        //    }
        //    catch (Exception e)
        //    {
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        response.IsError = true;
        //        ErrorHandling.HandleError(ref response, e.Message);
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
                        ErrorHandling.HandleError(ref response, "Avatar not found");
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
                            ErrorHandling.HandleError(ref response, $"Error occured in RefreshToken method in AvatarService saving avatar. Reason: {saveAvatarResult.Result}");
                    }
                    else
                        ErrorHandling.HandleError(ref response, $"Error occured in RefreshToken method in AvatarService getting refresh token. Reason: {refreshTokenResult.Result}");
                }
                catch (Exception ex)
                {
                    response.Exception = ex;
                    ErrorHandling.HandleError(ref response, $"An unknown error occured in RefreshToken method in AvatarService. Reason: {ex.Message}");
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
                    ErrorHandling.HandleError(ref response, "Avatar not found");
                    return response;
                }

                // revoke token and save
                refreshTokenResult.Result.Revoked = DateTime.UtcNow;
                refreshTokenResult.Result.RevokedByIp = ipAddress;
                avatar.IsBeamedIn = false;
                avatar.LastBeamedOut = DateTime.Now;

                var saveAvatar = AvatarManager.SaveAvatar(avatar);

                if (saveAvatar != null && !saveAvatar.IsError && saveAvatar.Result != null)
                {
                    response.Message = "Token Revoked.";
                    return response;
                }

                ErrorHandling.HandleError(ref response, $"An error in Revoke Token method in AvatarService saving the avatar. Reason: {saveAvatar.Message}");
                return response;
            });
        }

        public async Task<OASISResult<IAvatar>> RegisterAsync(RegisterRequest model, string origin)
        {
            var result = PrepareToRegister(model);

            if (!result.IsError)
            {
                origin = GetOrigin(origin);

                result = await AvatarManager.RegisterAsync(model.Title, model.FirstName, model.LastName, model.Email, model.Password,
                    (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType), origin, model.CreatedOASISType);
            }

            return result;
        }

        public OASISResult<IAvatar> Register(RegisterRequest model, string origin)
        {
            var result = PrepareToRegister(model);

            if (!result.IsError)
            {
                origin = GetOrigin(origin);

                result = AvatarManager.Register(model.Title, model.FirstName, model.LastName, model.Email, model.Password,
                    (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType), origin, model.CreatedOASISType);
            }

            return result;
        }

        private string GetOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin))
                origin = Program.CURRENT_OASISAPI;

            return origin;
        }

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
                ErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            return result;
        }

        public async Task<OASISResult<bool>> VerifyEmail(string token)
        {
            return await Task.Run(() => AvatarManager.VerifyEmail(token));
        }

        public async Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.LoadAvatarByEmailAsync(model.Email, false);

                // always return ok response to prevent email enumeration
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    ErrorHandling.HandleError(ref response, $"Error occured loading avatar in ForgotPassword, avatar not found. Reason: {avatarResult.Message}");
                    return response;
                }

                // create reset token that expires after 1 day
                avatarResult.Result.ResetToken = RandomTokenString();
                avatarResult.Result.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

                var saveAvatar = AvatarManager.SaveAvatar(avatarResult.Result);

                if (saveAvatar.IsError)
                {
                    ErrorHandling.HandleError(ref response, $"An error occured saving the avatar in ForgotPassword method in AvatarService. Reason: {saveAvatar.Message}");
                    return response;
                }

                // send email
                SendPasswordResetEmail(avatarResult.Result, origin);
                response.Message = "Please check your email for password reset instructions";
            }
            catch (Exception e)
            {
                response.Exception = e;
                ErrorHandling.HandleError(ref response, $"An error occured in ForgotPassword method in AvatarService. Reason: {e.Message}");
            }

            return response;
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
                        ErrorHandling.HandleError(ref result, "Invalid token");
                }
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in ValidateResetToken loading all avatars. Reason: {avatarsResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"An unknown error occured in ValidateResetToken. Reason: {e}", true, false, false, false, true, e);
            }

            return result;
        }

        public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IEnumerable<IAvatar>> avatarsResult = await AvatarManager.LoadAllAvatarsAsync(false);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                    var avatar = avatarsResult.Result.FirstOrDefault(x =>
                        x.ResetToken == model.Token &&
                        x.ResetTokenExpires > DateTime.UtcNow);

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref response, "Avatar Not Found");
                        return response;
                    }

                    // update password and remove reset token
                    avatar.Password = BC.HashPassword(model.Password);
                    avatar.PasswordReset = DateTime.UtcNow;
                    avatar.ResetToken = null;
                    avatar.ResetTokenExpires = null;

                    var saveAvatarResult = AvatarManager.SaveAvatar(avatar);

                    if (saveAvatarResult.IsError)
                    {
                        ErrorHandling.HandleError(ref saveAvatarResult, $"Error occured in ResetPassword saving the avatar. Reason: {saveAvatarResult.Message}");
                        return response;
                    }

                    response.Result = "Password reset successful, you can now login";
                }
                else
                    ErrorHandling.HandleError(ref response, $"Error occured in ResetPassword loading all avatars. Reason: {avatarsResult.Message}");
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                response.IsSaved = false;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        //public async Task<OASISResult<IEnumerable<IAvatar>>> GetAll()
        //{
        //    var response = new OASISResult<IEnumerable<IAvatar>>();

        //    try
        //    {
        //        response = await AvatarManager.LoadAllAvatarsAsync();
        //        //OASISResult<IEnumerable<IAvatar await AvatarManager.LoadAllAvatarsAsync();

        //        //foreach (IAvatar avatar in response.Result)
        //        //    AvatarManager.RemoveAuthDetails()
        //    }
        //    catch (Exception e)
        //    {
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        response.IsError = true;
        //        ErrorHandling.HandleError(ref response, e.Message);
        //    }

        //    return response;
        //}

        public async Task<OASISResult<AvatarImage>> GetAvatarImageById(Guid id)
        {
            OASISResult<AvatarImage> result = new OASISResult<AvatarImage>();

            if (id == Guid.Empty)
            {
                ErrorHandling.HandleError(ref result, "Error occured in GetAvatarImageById. Guid is empty, please speceify a valid Guid.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailAsync(id);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                if (avatarResult.Result.Image2D == null)
                    ErrorHandling.HandleError(ref result, "Error occured in GetAvatarImageById. No image has been uploaded for this avatar. Please upload an image first.");
                else
                {
                    result.Result = new AvatarImage
                    {
                        AvatarId = avatarResult.Result.Id,
                        ImageBase64 = avatarResult.Result.Image2D
                    };
                }
            }
            else
                ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarImageById loading the avatar detail. Reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<AvatarImage>> GetAvatarImageByUsername(string username)
        {
            OASISResult<AvatarImage> result = new OASISResult<AvatarImage>();

            if (string.IsNullOrEmpty(username))
            {
                ErrorHandling.HandleError(ref result, "Error occured in GetAvatarImageByUsername. username is empty, please speceify a valid username.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                if (avatarResult.Result.Image2D == null)
                    ErrorHandling.HandleError(ref result, "Error occured in GetAvatarImageByUsername. No image has been uploaded for this avatar. Please upload an image first.");
                else
                {
                    result.Result = new AvatarImage
                    {
                        AvatarId = avatarResult.Result.Id,
                        ImageBase64 = avatarResult.Result.Image2D
                    };
                }
            }
            else
                ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarImageByUsername loading the avatar detail. Reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<AvatarImage>> GetAvatarImageByEmail(string email)
        {
            OASISResult<AvatarImage> result = new OASISResult<AvatarImage>();

            if (string.IsNullOrEmpty(email))
            {
                ErrorHandling.HandleError(ref result, "Error occured in GetAvatarImageByEmail. Email is empty, please speceify a valid username.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(email);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                if (avatarResult.Result.Image2D == null)
                    ErrorHandling.HandleError(ref result, "Error occured in GetAvatarImageByEmail. No image has been uploaded for this avatar. Please upload an image first.");
                else
                {
                    result.Result = new AvatarImage
                    {
                        AvatarId = avatarResult.Result.Id,
                        ImageBase64 = avatarResult.Result.Image2D
                    };
                }
            }
            else
                ErrorHandling.HandleError(ref result, $"Error occured in email loading the avatar detail. Reason: {avatarResult.Message}");

            return result;
        }

        public async Task<OASISResult<bool>> Upload2DAvatarImage(AvatarImage image)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (image.AvatarId == Guid.Empty)
                {
                    ErrorHandling.HandleError(ref response, "Error occured in Upload2DAvatarImage, AvatarId property not specified");
                    return response;
                }

                OASISResult<IAvatarDetail> avatarResult = await AvatarManager.LoadAvatarDetailAsync(image.AvatarId);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    avatarResult.Result.Image2D = image.ImageBase64;
                    var saveAvatar = AvatarManager.SaveAvatarDetail(avatarResult.Result);

                    if (saveAvatar.IsError)
                    {
                        ErrorHandling.HandleError(ref response, $"Error occured in Upload2DAvatarImage saving avatar detail. Reason: {saveAvatar.Message}");
                        return response;
                    }

                    response.Message = "Image Uploaded";
                    response.Result = true;
                }
                else
                {
                    response.Result = false;
                    ErrorHandling.HandleError(ref response, $"Error uploading image. Avatar failed to load, reason: {avatarResult.Message}");
                }
               
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref response, e.Message, true, false, false, false, true, e);
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
        //            ErrorHandling.HandleError(ref response, "Error in GetByUsername, UserName property is empty");
        //            return response;
        //        }

        //        response = await AvatarManager.LoadAvatarAsync(userName);
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorHandling.HandleError(ref response, e.Message, true, false, false, false, true, e);
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
        //            ErrorHandling.HandleError(ref response, "Error in GetByEmail, Email property is empty");
        //            return response;
        //        }

        //        response = await AvatarManager.LoadAvatarByEmailAsync(email);
        //    }
        //    catch (Exception e)
        //    {
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        response.IsError = true;
        //        ErrorHandling.HandleError(ref response, response.Message);
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
                    ErrorHandling.HandleError(ref result, $"Email '{model.Email}' is already registered");
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
                        ErrorHandling.HandleError(ref result, $"Error occured in Create method on AvatarService saving the avatar. Reason: {saveAvatarResult.Message} ");
                    else
                    {
                        result.Result = AvatarManager.HideAuthDetails(avatar);
                        result.Message = "Avatar Created Successfully";
                    }
                }
            }
            else
                ErrorHandling.HandleError(ref result, $"Error occured in Create method on AvatarService loading all avatars. Reason: {avatarsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // only admins can update role
                if (avatar.AvatarType != "Wizard")
                    avatar.AvatarType = null;

                //var oasisResult = await GetAvatar(id, true);
                var oasisResult = await AvatarManager.LoadAvatarAsync(id, false);

                if (oasisResult.IsError)
                {
                    ErrorHandling.HandleError(ref response, "Avatar not found");
                    return response;
                }

                var origAvatar = oasisResult.Result;

                if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email &&
                    await AvatarManager.LoadAvatarByEmailAsync(avatar.Email) != null)
                {
                    ErrorHandling.HandleError(ref response, $"Email '{avatar.Email}' is already taken");
                    return response;
                }

                // hash password if it was entered
                if (!string.IsNullOrEmpty(avatar.Password))
                    avatar.Password = BC.HashPassword(avatar.Password);

                //TODO: Fix this.
                _mapper.Map(avatar, origAvatar);
                origAvatar.ModifiedDate = DateTime.UtcNow;

                var saveResult = AvatarManager.SaveAvatar(origAvatar);
                if (saveResult.IsError)
                {
                    ErrorHandling.HandleError(ref response, saveResult.Message);
                    return response;
                }

                response.Result = AvatarManager.HideAuthDetails(saveResult.Result);
            }
            catch (Exception e)
            {
                response.Exception = e;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByEmail(string email, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            
            // only admins can update role
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            
            OASISResult<IAvatar> originalAvatarResult = await AvatarManager.LoadAvatarByEmailAsync(email);

            if (!originalAvatarResult.IsError && originalAvatarResult.Result != null)
            {
                if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != originalAvatarResult.Result.Email)
                {
                    ErrorHandling.HandleError(ref response, $"Email '{avatar.Email}' is already taken");
                    return response;
                }
            }

            // hash password if it was entered
            if (!string.IsNullOrEmpty(avatar.Password))
                avatar.Password = BC.HashPassword(avatar.Password);

            //TODO: Fix this.
            _mapper.Map(avatar, originalAvatarResult.Result);
            originalAvatarResult.Result.ModifiedDate = DateTime.UtcNow;
            var saveResult = AvatarManager.SaveAvatar(originalAvatarResult.Result);

            if (saveResult.IsError)
            {
                ErrorHandling.HandleError(ref response, $"Error in UpdateByEmail saving avatar. Reason: {saveResult.Message}");
                return response;
            }

            response.Result = AvatarManager.HideAuthDetails(saveResult.Result);
            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByUsername(string username, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();

            // only admins can update role
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            
            OASISResult<IAvatar> originalAvatarResult = await AvatarManager.LoadAvatarAsync(username);

            if (!originalAvatarResult.IsError && originalAvatarResult.Result != null)
            {
                if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != originalAvatarResult.Result.Email &&
                    await AvatarManager.LoadAvatarByEmailAsync(avatar.Email) != null)
                {
                    ErrorHandling.HandleError(ref response, $"Email '{avatar.Email}' is already taken");
                    return response;
                }
            }
            else
            {
                ErrorHandling.HandleError(ref response, $"Error loading avatar in UpdateByUsername method. Reason: {originalAvatarResult.Message}");
                return response;
            }

            // hash password if it was entered
            if (!string.IsNullOrEmpty(avatar.Password))
                avatar.Password = BC.HashPassword(avatar.Password);

            //TODO: Fix this.
            _mapper.Map(avatar, originalAvatarResult.Result);
            originalAvatarResult.Result.ModifiedDate = DateTime.UtcNow;

            var saveAvatar = AvatarManager.SaveAvatar(originalAvatarResult.Result);
            if (saveAvatar.IsError)
            {
                ErrorHandling.HandleError(ref response, $"Error in UpdateByUsername saving avatar. Reason: {saveAvatar.Message}");
                return response;
            }

            response.Result = AvatarManager.HideAuthDetails(saveAvatar.Result);
            return response;
        }

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
                ErrorHandling.HandleError(ref response, e.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

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
                    response.Result = "Token Validating Failed!";
                    ErrorHandling.HandleError(ref response, e.Message);
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
        //    ErrorHandling.HandleError(ref result, result.Message);
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
        //        ErrorHandling.HandleError(ref response, response.Message);
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
        //        ErrorHandling.HandleError(ref response, e.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }*/

        public async Task<OASISResult<string>> GetAvatarUmaJsonById(Guid id)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (id == Guid.Empty)
                    ErrorHandling.HandleError(ref result, "Error occured in GetAvatarUmaJsonById. AvatarId is empty");

                OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailAsync(id);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUmaJsonById loading avatar detail. Reason:{avatarDetailResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in GetAvatarUmaJsonById. Details: {e}", true, false, false, false, true, e);
            }

            return result;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonByUsername(string username)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrEmpty(username))
                    ErrorHandling.HandleError(ref result, "Error occured in GetAvatarUmaJsonByUsername. username is empty");

                OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUmaJsonByUsername loading avatar detail. Reason:{avatarDetailResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in GetAvatarUmaJsonByUsername. Details: {e}", true, false, false, false, true, e);
            }

            return result;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonByEmail(string email)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrEmpty(email))
                    ErrorHandling.HandleError(ref result, "Error occured in GetAvatarUmaJsonByEmail. email is empty");

                OASISResult<IAvatarDetail> avatarDetailResult = await AvatarManager.LoadAvatarDetailByUsernameAsync(email);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUmaJsonByEmail loading avatar detail. Reason:{avatarDetailResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in GetAvatarUmaJsonByEmail. Details: {e}", true, false, false, false, true, e);
            }

            return result;
        }

        //TODO: Check this works?!
        public async Task<OASISResult<IAvatar>> GetAvatarByJwt()
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<IAvatar>();
                try
                {
                    if (_httpContextAccessor.HttpContext == null)
                    {
                        response.Result = null;
                        ErrorHandling.HandleError(ref response, "Avatar not found.");
                        return response;
                    }

                    var avatar = (IAvatar) _httpContextAccessor.HttpContext.Items["Avatar"];

                    if (avatar == null)
                    {
                        response.Result = null;
                        ErrorHandling.HandleError(ref response, "Avatar not found");
                        return response;
                    }

                    response.Result = avatar;
                }
                catch (Exception e)
                {
                    response.Result = null;
                    ErrorHandling.HandleError(ref response, $"An unknown error occured in GetAvatarByJwt. Reason: {e.Message}");
                }

                return response;
            });
        }

        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (string.IsNullOrEmpty(searchParams.SearchQuery))
                {
                    response.Message = "SearchQuery field is empty";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                }

                response.Result = await AvatarManager.SearchAsync(searchParams);
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

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
        //            ErrorHandling.HandleError(ref response, $"Unknown error occured in LinkProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerType)} and key {key}: {e.Message}");
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
        //            ErrorHandling.HandleError(ref response, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerType)} and key {key}: {e.Message}");
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
                    ErrorHandling.HandleError(ref response, $"Unknown error occured in GetProviderKeyForAvatar for avatar {avatarUsername} and providerType {Enum.GetName(typeof(ProviderType), providerType)}: {e.Message}");
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
                    ErrorHandling.HandleError(ref response, $"Unknown error occured in GetPrivateProviderKeyForAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerType)}: {e.Message}");
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
                        ErrorHandling.HandleError(ref response, response.Message);
                    }

                    if (!Enum.TryParse(typeof(KarmaSourceType), addRemoveKarmaToAvatarRequest.karmaSourceType,
                        out karmaSourceTypeObject))
                    {
                        response.IsError = true;
                        response.IsSaved = false;
                        response.Message = string.Concat(
                            "ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n",
                            EnumHelper.GetEnumValues(typeof(KarmaSourceType)));
                        ErrorHandling.HandleError(ref response, response.Message);
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
                    ErrorHandling.HandleError(ref response, e.Message);
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
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                
                if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                {
                    response.Message = string.Concat(
                        "ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n",
                        EnumHelper.GetEnumValues(typeof(KarmaSourceType)));
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, response.Message);
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
                ErrorHandling.HandleError(ref response, e.Message);
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

        private (OASISResult<RefreshToken>, IAvatar) GetRefreshToken(string token)
        {
            OASISResult<RefreshToken> result = new OASISResult<RefreshToken>();

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list
            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars();

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
                ErrorHandling.HandleError(ref result, $"Error in GetRefreshToken loading all avatars. Reason: {avatarsResult.Message}");

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
                Token = RandomTokenString(),
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

        private string RandomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        private void SendPasswordResetEmail(IAvatar avatar, string origin)
        {
            if (string.IsNullOrEmpty(origin))
                origin = Program.CURRENT_OASISAPI;

            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/avatar/reset-password?token={avatar.ResetToken}";
                message =
                    $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message =
                    $@"<p>Please use the below token to reset your password with the <code>/avatar/reset-password</code> api route:</p>
                             <p><code>{avatar.ResetToken}</code></p>";
            }

            if (!EmailManager.IsInitialized)
                EmailManager.Initialize(_OASISDNA);

            EmailManager.Send(
                avatar.Email,
                "OASIS - Reset Password",
                $@"<h4>Reset Password</h4>
                         {message}"
            );
        }
    }
}