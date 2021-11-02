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
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using BC = BCrypt.Net.BCrypt;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
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

        public async Task<OASISResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var response = new OASISResult<AuthenticateResponse>();
            try
            {
                var result = await AvatarManager.AuthenticateAsync(model.Email, model.Password, ipAddress);
                if (result.IsError)
                {
                    response.IsError = true;
                    response.Message = response.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                response.Result = new AuthenticateResponse
                    {Message = "Avatar Successfully Authenticated.", Avatar = result.Result};
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

        public async Task<OASISResult<IAvatar>> RefreshToken(string token, string ipAddress)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<IAvatar>();
                try
                {
                    var (refreshToken, avatar) = GetRefreshToken(token);
                    if (avatar == null)
                    {
                        response.IsError = true;
                        response.Message = "Avatar not found";
                        ErrorHandling.HandleError(ref response, response.Message);
                        return response;
                    }

                    var newRefreshToken = GenerateRefreshToken(ipAddress);
                    refreshToken.Revoked = DateTime.UtcNow;
                    refreshToken.RevokedByIp = ipAddress;
                    refreshToken.ReplacedByToken = newRefreshToken.Token;
                    avatar.RefreshTokens.Add(newRefreshToken);

                    avatar.RefreshToken = newRefreshToken.Token;
                    avatar.JwtToken = GenerateJwtToken(avatar);
                    avatar = RemoveAuthDetails(AvatarManager.SaveAvatar(avatar).Result);
                    response.Result = avatar;
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

        public async Task<OASISResult<string>> RevokeToken(string token, string ipAddress)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string> {Result = "Token Revoked"};
                var (refreshToken, avatar) = GetRefreshToken(token);

                if (avatar == null)
                {
                    response.IsError = true;
                    response.Message = "Avatar not found";
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                // revoke token and save
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                avatar.IsBeamedIn = false;
                avatar.LastBeamedOut = DateTime.Now;

                var saveAvatar = AvatarManager.SaveAvatar(avatar);
                if (!saveAvatar.IsError) return response;
                response.Exception = saveAvatar.Exception;
                response.IsError = true;
                response.IsSaved = false;
                response.Message = saveAvatar.Message;
                ErrorHandling.HandleError(ref response, response.Message);
                return response;
            });
        }

        public async Task<OASISResult<IAvatar>> Register(RegisterRequest model, string origin)
        {
            return await Task.Run(() =>
            {
                var result = new OASISResult<IAvatar>();
                if (string.IsNullOrEmpty(origin))
                    origin = Program.CURRENT_OASISAPI;
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

                result = AvatarManager.Register(model.Title, model.FirstName, model.LastName, model.Email, model.Password,
                    (AvatarType) Enum.Parse(typeof(AvatarType), model.AvatarType), origin, model.CreatedOASISType);
                return result;
            });
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
                //TODO: {PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                var avatar =
                    (await AvatarManager.LoadAllAvatarsAsync()).FirstOrDefault(x => x.Email == model.Email);

                // always return ok response to prevent email enumeration
                if (avatar == null)
                {
                    response.IsError = true;
                    response.Message = "Avatar not found";
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                // create reset token that expires after 1 day
                avatar.ResetToken = RandomTokenString();
                avatar.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

                var saveAvatar = AvatarManager.SaveAvatar(avatar);
                if (saveAvatar.IsError)
                {
                    response.IsSaved = false;
                    response.IsError = true;
                    response.Message = saveAvatar.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                // send email
                SendPasswordResetEmail(avatar, origin);
                response.Result = "Please check your email for password reset instructions";
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

        public async Task<OASISResult<string>> ValidateResetToken(ValidateResetTokenRequest model)
        {
            var result = new OASISResult<string>();
            try
            {
                //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                var avatar = (await AvatarManager.LoadAllAvatarsAsync()).FirstOrDefault(x =>
                    x.ResetToken == model.Token &&
                    x.ResetTokenExpires > DateTime.UtcNow);

                if (avatar == null)
                {
                    result.Message = "Invalid token";
                    result.IsError = true;
                    ErrorHandling.HandleError(ref result, result.Message);
                    return result;
                }
            }
            catch (Exception e)
            {
                result.Exception = e;
                result.Message = e.Message;
                result.IsError = true;
                ErrorHandling.HandleError(ref result, result.Message);
            }

            return result;
        }

        public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model)
        {
            var response = new OASISResult<string>();
            try
            {
                //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
                var avatar = (await AvatarManager.LoadAllAvatarsAsync()).FirstOrDefault(x =>
                    x.ResetToken == model.Token &&
                    x.ResetTokenExpires > DateTime.UtcNow);

                if (avatar == null)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = "Avatar Not Found";
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                // update password and remove reset token
                avatar.Password = BC.HashPassword(model.Password);
                avatar.PasswordReset = DateTime.UtcNow;
                avatar.ResetToken = null;
                avatar.ResetTokenExpires = null;

                var saveAvatar = AvatarManager.SaveAvatar(avatar);
                if (saveAvatar.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = saveAvatar.Message;
                    ErrorHandling.HandleError(ref saveAvatar, saveAvatar.Message);
                    return response;
                }

                response.Result = "Password reset successful, you can now login";
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

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAll()
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                response.Result = await AvatarManager.LoadAllAvatarsAsync();
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

        public async Task<OASISResult<AvatarImage>> GetAvatarImageById(Guid id)
        {
            var result = new OASISResult<AvatarImage>();
            if (id == Guid.Empty)
            {
                result.Message = "Guid is empty, please speceify a valid Guid.";
                result.IsError = true;
                ErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            var avatarResult = await GetAvatar(id);

            if (!avatarResult.IsError)
            {
                result.Result = new AvatarImage
                {
                    AvatarId = avatarResult.Result.Id,
                    ImageBase64 = avatarResult.Result.Image2D
                };
            }
            else
            {
                result.IsError = true;
                result.Message = avatarResult.Message;
                ErrorHandling.HandleError(ref result, avatarResult.Message);
                return result;
            }

            return result;
        }

        public async Task<OASISResult<AvatarImage>> GetAvatarImageByUsername(string userName)
        {
            var response = new OASISResult<AvatarImage>();
            try
            {
                if (string.IsNullOrEmpty(userName))
                {
                    response.Message = "Username is empty, please speceify a valid username.";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var avatarResult = await AvatarManager.LoadAvatarByUsernameAsync(userName);
                response.Result = new AvatarImage
                {
                    AvatarId = avatarResult.Id,
                    ImageBase64 = avatarResult.Image2D
                };
            }
            catch (Exception e)
            {
                response.IsError = false;
                response.Exception = e;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, response.Message);
            }

            return response;
        }

        public async Task<OASISResult<AvatarImage>> GetAvatarImageByEmail(string email)
        {
            var response = new OASISResult<AvatarImage>();
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    response.Message = "Email is empty, please speceify a valid email.";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var avatarResult = await AvatarManager.LoadAvatarByEmailAsync(email);
                response.Result = new AvatarImage
                {
                    AvatarId = avatarResult.Id,
                    ImageBase64 = avatarResult.Image2D
                };
            }
            catch (Exception e)
            {
                response.IsError = false;
                response.Exception = e;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<string>> Upload2DAvatarImage(AvatarImage image)
        {
            var response = new OASISResult<string>();
            try
            {
                if (image.AvatarId == Guid.Empty)
                {
                    response.Message = "AvatarId property not specified";
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var avatar = await GetAvatar(image.AvatarId);
                avatar.Result.Image2D = image.ImageBase64;
                var saveAvatar = AvatarManager.SaveAvatar(avatar.Result);
                if (saveAvatar.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = saveAvatar.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
                }

                response.Result = "Image Uploaded";
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

        public async Task<OASISResult<IAvatar>> GetById(Guid id)
        {
            return await GetAvatar(id);
        }

        public async Task<OASISResult<IAvatar>> GetByUsername(string userName)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (string.IsNullOrEmpty(userName))
                {
                    response.Message = "UserName property is empty";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                response.Result = await AvatarManager.LoadAvatarByUsernameAsync(userName);
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

        public async Task<OASISResult<IAvatar>> GetByEmail(string email)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    response.Message = "Email property is empty";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                response.Result = await AvatarManager.LoadAvatarByEmailAsync(email);
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, response.Message);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> Create(CreateRequest model)
        {
            var result = new OASISResult<IAvatar>();
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            if ((await AvatarManager.LoadAllAvatarsAsync()).Any(x => x.Email == model.Email))
            {
                result.Message = $"Email '{model.Email}' is already registered";
                result.IsError = true;
                result.IsSaved = false;
                ErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            // map model to new account object
            var avatar = _mapper.Map<IAvatar>(model);
            avatar.CreatedDate = DateTime.UtcNow;
            avatar.Verified = DateTime.UtcNow;

            // hash password
            avatar.Password = BC.HashPassword(model.Password);
            var saveResult = AvatarManager.SaveAvatar(avatar);
            if (saveResult.IsError)
            {
                result.Message = saveResult.Message;
                result.IsError = true;
                result.IsSaved = false;
                ErrorHandling.HandleError(ref result, result.Message);
                return saveResult;
            }

            result.Result = RemoveAuthDetails(avatar);
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
                
                var oasisResult = await GetAvatar(id);
                if (oasisResult.IsError)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = "Avatar not found";
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var origAvatar = oasisResult.Result;

                if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email &&
                    await AvatarManager.LoadAvatarByEmailAsync(avatar.Email) != null)
                {
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = $"Email '{avatar.Email}' is already taken";
                    ErrorHandling.HandleError(ref response, response.Message);
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
                    response.IsError = true;
                    response.IsSaved = false;
                    response.Message = saveResult.Message;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                response.Result = RemoveAuthDetails(saveResult.Result);
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

        public async Task<OASISResult<IAvatar>> UpdateByEmail(string email, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            
            // only admins can update role
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            
            var origAvatar = await AvatarManager.LoadAvatarByEmailAsync(email);

            if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = $"Email '{avatar.Email}' is already taken";
                ErrorHandling.HandleError(ref response, response.Message);
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
                response.Message = saveResult.Message;
                response.IsSaved = false;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            response.Result = RemoveAuthDetails(saveResult.Result);
            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByUsername(string username, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();

            // only admins can update role
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            
            var origAvatar = await AvatarManager.LoadAvatarByUsernameAsync(username);

            if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email &&
                await AvatarManager.LoadAvatarByEmailAsync(avatar.Email) != null)
            {
                response.Message = $"Email '{avatar.Email}' is already taken";
                response.IsError = true;
                response.IsSaved = false;
                ErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            // hash password if it was entered
            if (!string.IsNullOrEmpty(avatar.Password))
                avatar.Password = BC.HashPassword(avatar.Password);

            //TODO: Fix this.
            _mapper.Map(avatar, origAvatar);
            origAvatar.ModifiedDate = DateTime.UtcNow;

            var saveAvatar = AvatarManager.SaveAvatar(origAvatar);
            if (saveAvatar.IsError)
            {
                response.Message = saveAvatar.Message;
                response.IsError = true;
                response.IsSaved = false;
                ErrorHandling.HandleError(ref response, response.Message);
                return response;
            }

            response.Result = RemoveAuthDetails(saveAvatar.Result);
            return response;
        }

        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response.Result = await AvatarManager.DeleteAvatarAsync(id);
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
                response.Result = await AvatarManager.DeleteAvatarByUsernameAsync(username);
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
                response.Result = await AvatarManager.DeleteAvatarByEmailAsync(email);
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
                    var key = Encoding.ASCII.GetBytes(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Security.Secret);
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

        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id)
        {
            var result = new OASISResult<IAvatarDetail>();
            var avatar = await AvatarManager.LoadAvatarDetailAsync(id);

            if (avatar != null) return result;
            result.Message = "AvatarDetail not found";
            result.IsError = true;
            ErrorHandling.HandleError(ref result, result.Message);
            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                var entity = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);
                response.Result = entity;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Exception = e;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, response.Message);
            }

            return response;
        }

        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                var entity = await AvatarManager.LoadAvatarDetailByEmailAsync(email);
                response.Result = entity;
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Exception = e;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                response.Result = await AvatarManager.LoadAllAvatarDetailsAsync();
            }
            catch (Exception e)
            {
                response.Message = e.Message;
                response.Exception = e;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonById(Guid id)
        {
            var response = new OASISResult<string>();
            try
            {
                if (id == Guid.Empty)
                {
                    response.Message = "AvatarId is empty";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                }

                var avatarDetail = await AvatarManager.LoadAvatarDetailAsync(id);
                response.Result = avatarDetail.UmaJson;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.Result = null;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonByUsername(string username)
        {
            var response = new OASISResult<string>();
            try
            {
                var avatarDetail = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);
                response.Result = avatarDetail.UmaJson;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.Result = null;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<string>> GetAvatarUmaJsonByMail(string mail)
        {
            var response = new OASISResult<string>();
            try
            {
                if (string.IsNullOrEmpty(mail))
                {
                    response.Message = "Mail property is empty";
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }

                var avatarDetail = await AvatarManager.LoadAvatarDetailByEmailAsync(mail);
                response.Result = avatarDetail.UmaJson;
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.Result = null;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> GetAvatarByJwt()
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<IAvatar>();
                try
                {
                    if (_httpContextAccessor.HttpContext == null)
                    {
                        response.Message = "Do not found avatar";
                        response.Result = null;
                        ErrorHandling.HandleError(ref response, response.Message);
                        return response;
                    }

                    var avatar = (IAvatar) _httpContextAccessor.HttpContext.Items["Avatar"];
                    if (avatar == null)
                    {
                        response.Message = "Do not found avatar";
                        response.Result = null;
                        ErrorHandling.HandleError(ref response, response.Message);
                        return response;
                    }

                    response.Result = avatar;
                }
                catch (Exception e)
                {
                    response.Exception = e;
                    response.Message = e.Message;
                    response.Result = null;
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, e.Message);
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

        public async Task<OASISResult<IAvatarDetail>> LinkProviderKeyToAvatar(Guid avatarId, ProviderType telosOasis, string telosAccountName)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<IAvatarDetail>();
                try
                {
                    response.Result =
                        AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.TelosOASIS, telosAccountName);
                }
                catch (Exception e)
                {
                    response.Message = e.Message;
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, e.Message);
                }
                return response;
            });
        }

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
                    response.Message = e.Message;
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, e.Message);
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
                    response.Message = e.Message;
                    response.IsError = true;
                    response.IsSaved = false;
                    ErrorHandling.HandleError(ref response, e.Message);
                }
                return response;
            });
        }

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

                    response.Result = AvatarManager.AddKarmaToAvatar(avatarId,
                        (KarmaTypePositive) karmaTypePositiveObject,
                        (KarmaSourceType) karmaSourceTypeObject, addRemoveKarmaToAvatarRequest.KaramSourceTitle,
                        addRemoveKarmaToAvatarRequest.KarmaSourceDesc).Result;
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
                
                response.Result = AvatarManager.RemoveKarmaFromAvatar(avatarId, (KarmaTypeNegative) karmaTypeNegativeObject,
                    (KarmaSourceType) karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle,
                    addKarmaToAvatarRequest.KarmaSourceDesc).Result;
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

        private async Task<OASISResult<IAvatar>> GetAvatar(Guid id)
        {
            var result = new OASISResult<IAvatar>();
            var avatar = await AvatarManager.LoadAvatarAsync(id);

            if (avatar == null)
            {
                result.Message = "Avatar not found";
                result.IsError = true;
                ErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            result.Result = avatar;
            return result;
        }

        private (RefreshToken, IAvatar) GetRefreshToken(string token)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            var avatar = AvatarManager.LoadAllAvatarsWithPasswords()
                .FirstOrDefault(x => x.RefreshTokens.Any(t => t.Token == token));

            if (avatar == null)
                throw new AppException("Invalid token");

            var refreshToken = avatar.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
                throw new AppException("Invalid token");

            return (refreshToken, avatar);
        }

        //TODO: Finish moving everything into AvatarManager.
        private string GenerateJwtToken(IAvatar account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_OASISDNA.OASIS.Security.Secret);
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

        private IAvatar RemoveAuthDetails(IAvatar avatar)
        {
            avatar.VerificationToken = null; //TODO: Put back in when LIVE!
            avatar.Password = null;
            return avatar;
        }

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

            EmailManager.Send(
                avatar.Email,
                "OASIS - Reset Password",
                $@"<h4>Reset Password</h4>
                         {message}"
            );
        }
    }
}