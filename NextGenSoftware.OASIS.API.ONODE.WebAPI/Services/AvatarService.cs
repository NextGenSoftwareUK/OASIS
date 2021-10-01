using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class AvatarService : IAvatarService
    {
        private readonly IMapper _mapper;
        private readonly OASISDNA _OASISDNA;
    //    private readonly IEmailService _emailService;
        
       // private readonly IConfiguration _configuration;

        public AvatarManager AvatarManager
        {
            get
            {
                 return Program.AvatarManager;
            }
        }

        public AvatarService(
            IMapper mapper,
            IOptions<OASISDNA> OASISSettings)
           // IEmailService emailService)
        {
            _mapper = mapper;
            _OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
          //  _emailService = emailService;
           // _configuration = configuration;
        }
        
        public string GetTerms()
        {
            return _OASISDNA.OASIS.Terms;
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            //OASISResult<IAvatar> result = AvatarManager.Authenticate(model.Email, model.Password, ipAddress, _OASISDNA.OASIS.Security.Secret);
            OASISResult<IAvatar> result = AvatarManager.Authenticate(model.Email, model.Password, ipAddress);

            if (!result.IsError)
                return new AuthenticateResponse() { Message = "Avatar Successfully Authenticated.", Avatar = result.Result };
            else
                return new AuthenticateResponse() { Message = result.Message, IsError = true };
        }

        //public AuthenticateResponse RefreshToken(string token, string ipAddress)
        public IAvatar RefreshToken(string token, string ipAddress)
        {
            (RefreshToken refreshToken, IAvatar avatar) = GetRefreshToken(token);

            // replace old refresh token with a new one and save
            var newRefreshToken = GenerateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            avatar.RefreshTokens.Add(newRefreshToken);

            avatar.RefreshToken = newRefreshToken.Token;
            avatar.JwtToken = GenerateJwtToken(avatar);
            avatar = RemoveAuthDetails(AvatarManager.SaveAvatar(avatar).Result);
            return avatar;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, avatar) = GetRefreshToken(token);

            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            avatar.IsBeamedIn = false;
            avatar.LastBeamedOut = DateTime.Now;

            AvatarManager.SaveAvatar(avatar);
        }

        public IAvatar Register(RegisterRequest model, string origin)
        {
             if (string.IsNullOrEmpty(origin))
                 origin = Program.CURRENT_OASISAPI; 

            return AvatarManager.Register(model.Title, model.FirstName, model.LastName, model.Email, model.Password, (AvatarType)Enum.Parse(typeof(AvatarType), model.AvatarType), origin, model.CreatedOASISType).Result;
        }

        public OASISResult<bool> VerifyEmail(string token)
        {
            return AvatarManager.VerifyEmail(token);
        }

        public void ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            //TODO: {PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.Email == model.Email);

            // always return ok response to prevent email enumeration
            if (avatar == null) return;

            // create reset token that expires after 1 day
            avatar.ResetToken = RandomTokenString();
            avatar.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

            AvatarManager.SaveAvatar(avatar);

            // send email
            SendPasswordResetEmail(avatar, origin);
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.ResetToken == model.Token &&
                x.ResetTokenExpires > DateTime.UtcNow);

            if (avatar == null)
                throw new AppException("Invalid token");
        }

        public void ResetPassword(ResetPasswordRequest model)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.ResetToken == model.Token &&
                x.ResetTokenExpires > DateTime.UtcNow);

            if (avatar == null)
                throw new AppException("Invalid token");

            // update password and remove reset token
            avatar.Password = BC.HashPassword(model.Password);
            avatar.PasswordReset = DateTime.UtcNow;
            avatar.ResetToken = null;
            avatar.ResetTokenExpires = null;

            AvatarManager.SaveAvatar(avatar);
        }

        public IEnumerable<IAvatar> GetAll()
        {
            return AvatarManager.LoadAllAvatars();
        }

        //TODO: MAKE ALL METHODS WORK LIKE THIS! ;-) THANKS!
        public OASISResult<AvatarImage> GetAvatarImageById(Guid id)
        {
            OASISResult<AvatarImage> result = new OASISResult<AvatarImage>();

            if (id == Guid.Empty)
            {
                result.Message = "Guid is empty, please speceify a valid Guid.";
                result.IsError = true;
            }

            OASISResult<IAvatar> avatarResult = GetAvatar(id);

            if (!avatarResult.IsError)
                result.Result = new AvatarImage(Encoding.ASCII.GetBytes(avatarResult.Result.Image2D));
            else
            {
                result.IsError = true;
                result.Message = avatarResult.Message;
            }

            return result;
        }

        public async Task<OASISResult<AvatarImage>> GetAvatarImageByUsername(string userName)
        {
            var response = new OASISResult<AvatarImage>();

            try
            {
                var avatarResult = await AvatarManager.LoadAvatarByUsernameAsync(userName);
                response.Result = new AvatarImage(Encoding.ASCII.GetBytes(avatarResult.Image2D));
            }
            catch (Exception e)
            {
                response.IsError = false;
                response.Exception = e;
                response.Message = e.Message;
            }
            
            return response;
        }
        
        public async Task<OASISResult<AvatarImage>> GetAvatarImageByEmail(string email)
        {
            var response = new OASISResult<AvatarImage>();

            try
            {
                var avatarResult = await AvatarManager.LoadAvatarByEmailAsync(email);
                response.Result = new AvatarImage(Encoding.ASCII.GetBytes(avatarResult.Image2D));
            }
            catch (Exception e)
            {
                response.IsError = false;
                response.Exception = e;
                response.Message = e.Message;
            }
            
            return response;
        }

        public void Upload2DAvatarImage(Guid id, byte[] image)
        {
            if (id == Guid.Empty)
                return;

            var image2d = Encoding.ASCII.GetString(image);
            //var avatar = GetAvatarDetail(id);
            var avatar = GetAvatar(id);
            avatar.Result.Image2D = image2d;

            //AvatarManager.SaveAvatarDetail(avatar);
            AvatarManager.SaveAvatar(avatar.Result);
        }

        public IAvatar GetById(Guid id)
        {
            return GetAvatar(id).Result;
        }

        public async Task<IAvatar> GetByUsername(string userName)
        {
            return await AvatarManager.LoadAvatarByUsernameAsync(userName);
        }

        public async Task<IAvatar> GetByEmail(string email)
        {
            return await AvatarManager.LoadAvatarByEmailAsync(email);
        }

        public IAvatar Create(CreateRequest model)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            if (AvatarManager.LoadAllAvatars().Any(x => x.Email == model.Email))
                throw new AppException($"Email '{model.Email}' is already registered");

            // map model to new account object
            IAvatar avatar = _mapper.Map<IAvatar>(model);
            avatar.CreatedDate = DateTime.UtcNow;
            avatar.Verified = DateTime.UtcNow;

            // hash password
            avatar.Password = BC.HashPassword(model.Password);
            AvatarManager.SaveAvatar(avatar);
            return RemoveAuthDetails(avatar);
        }

        public async Task<IAvatar> Update(Guid id, UpdateRequest avatar)
        {
             var oasisResult = GetAvatar(id);
             if (oasisResult.IsError)
                 return null;
             
             var origAvatar = oasisResult.Result;

             if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email && await AvatarManager.LoadAvatarByEmailAsync(avatar.Email) != null)
                throw new AppException($"Email '{avatar.Email}' is already taken");

             // hash password if it was entered
             if (!string.IsNullOrEmpty(avatar.Password))
                 avatar.Password = BC.HashPassword(avatar.Password);

             //TODO: Fix this.
             _mapper.Map(avatar, origAvatar);
             origAvatar.ModifiedDate = DateTime.UtcNow;

            return RemoveAuthDetails(AvatarManager.SaveAvatar(origAvatar).Result);
        }

        public async Task<IAvatar> UpdateByEmail(string email, UpdateRequest avatar)
        {
            IAvatar origAvatar = await AvatarManager.LoadAvatarByEmailAsync(email);

            if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email)
                throw new AppException($"Email '{avatar.Email}' is already taken");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(avatar.Password))
                avatar.Password = BC.HashPassword(avatar.Password);

            //TODO: Fix this.
            _mapper.Map(avatar, origAvatar);
            origAvatar.ModifiedDate = DateTime.UtcNow;
            return RemoveAuthDetails(AvatarManager.SaveAvatar(origAvatar).Result);
        }

        public async Task<IAvatar> UpdateByUsername(string username, UpdateRequest avatar)
        {
            IAvatar origAvatar = await AvatarManager.LoadAvatarByUsernameAsync(username);

            if (!string.IsNullOrEmpty(avatar.Email) && avatar.Email != origAvatar.Email && (await AvatarManager.LoadAvatarByEmailAsync(avatar.Email) != null))
                throw new AppException($"Email '{avatar.Email}' is already taken");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(avatar.Password))
                avatar.Password = BC.HashPassword(avatar.Password);

            //TODO: Fix this.
            _mapper.Map(avatar, origAvatar);
            origAvatar.ModifiedDate = DateTime.UtcNow;

            return RemoveAuthDetails(AvatarManager.SaveAvatar(origAvatar).Result);
        }

        public bool Delete(Guid id)
        {
            // Default to soft delete.
            return AvatarManager.DeleteAvatar(id);
        }

        public async Task<bool> DeleteByUsername(string username)
        {
            return await AvatarManager.DeleteAvatarByUsernameAsync(username);
        }

        public async Task<bool> DeleteByEmail(string email)
        {
            return await AvatarManager.DeleteAvatarByEmailAsync(email);
        }
        
        public OASISResult<string> ValidateAccountToken(string accountToken)
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
            }
            
            return response;
        }
        
        
        //public async Task<ApiResponse<IAvatarThumbnail>> GetAvatarThumbnail(Guid id)
        //{
        //    var response = new ApiResponse<IAvatarThumbnail>();
        //    try
        //    {
        //        var thumbnail = await AvatarManager.LoadAvatarThumbnailAsync(id);
        //        response.Payload = thumbnail;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Code = ApiConstantsCodes.Failed;
        //        response.Message = $"{ApiConstantsContents.Failed} - {ex.Message}";
        //    }
        //    return response; 
        //}

        /*
        public async Task<ApiResponse<IAvatarDetail>> GetAvatarDetail(Guid id)
        {
            var response = new ApiResponse<IAvatarDetail>();
            try
            {
                var detail = await AvatarManager.LoadAvatarDetailsAsync(id);
                response.Payload = detail;
            }
            catch (Exception ex)
            {
                response.Code = ApiConstantsCodes.Failed;
                response.Message = $"{ApiConstantsContents.Failed} - {ex.Message}";
            }
            return response;
        }

        public async Task<ApiResponse<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            var response = new ApiResponse<IEnumerable<IAvatarDetail>>();
            try
            {
                var details = await AvatarManager.LoadAllAvatarDetailsAsync();
                response.Payload = details;
            }
            catch (Exception ex)
            {
                response.Code = ApiConstantsCodes.Failed;
                response.Message = $"{ApiConstantsContents.Failed} - {ex.Message}";
            }
            return response;
        }*/

        private OASISResult<IAvatar> GetAvatar(Guid id)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            IAvatar avatar = AvatarManager.LoadAvatar(id);

            if (avatar == null)
            {
                result.Message = "Avatar not found";
                result.IsError = true;
            }

            result.Result = avatar;
            return result;
        }

        public OASISResult<IAvatarDetail> GetAvatarDetail(Guid id)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            IAvatarDetail avatar = AvatarManager.LoadAvatarDetail(id);

            if (avatar == null)
            {
                result.Message = "AvatarDetail not found";
                result.IsError = true;
            }

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
            }
            
            return response;
        }

        public OASISResult<IEnumerable<IAvatarDetail>> GetAllAvatarDetails()
        {
            return new OASISResult<IEnumerable<IAvatarDetail>>(AvatarManager.LoadAllAvatarDetails());
        }

        private (RefreshToken, IAvatar) GetRefreshToken(string token)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatarsWithPasswords().FirstOrDefault(x => x.RefreshTokens.Any(t => t.Token == token));

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
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new RefreshToken
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
            // avatar.RefreshToken = null;
            //avatar.RefreshTokens = null;

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
                message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to reset your password with the <code>/avatar/reset-password</code> api route:</p>
                             <p><code>{avatar.ResetToken}</code></p>";
            }

            EmailManager.Send(
                to: avatar.Email,
                subject: "OASIS - Reset Password",
                html: $@"<h4>Reset Password</h4>
                         {message}"
            );

            //_emailService.Send(
            //    to: avatar.Email,
            //    subject: "OASIS - Reset Password",
            //    html: $@"<h4>Reset Password</h4>
            //             {message}"
            //);
        }
    }
}
