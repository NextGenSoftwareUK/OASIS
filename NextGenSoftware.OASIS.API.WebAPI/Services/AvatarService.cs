using AutoMapper;
using BC = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.WebAPI;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class AvatarService : IAvatarService
    {
       // private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly OASISSettings _OASISSettings;
        private readonly IEmailService _emailService;
        //private AvatarManager _avatarManager;

        public AvatarManager AvatarManager
        {
            get
            {
                 return Program.AvatarManager;

                //if (_avatarManager == null)
                //{
                //    _avatarManager = new AvatarManager();
                //    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
                //}

                //return _avatarManager;
            }
        }

        //private void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        //{
        //    //TODO: Log and handle errors here.
        //}

        //public AvatarService(
        //    DataContext context,
        //    IMapper mapper,
        //    IOptions<OASISSettings> OASISSettings,
        //    IEmailService emailService)
        //{
        //  //  _context = context;
        //    _mapper = mapper;
        //    _OASISSettings = OASISSettings.Value;
        //    _emailService = emailService;
        //}

        public AvatarService(
            IMapper mapper,
            IOptions<OASISSettings> OASISSettings,
            IEmailService emailService)
        {
            _mapper = mapper;
            _OASISSettings = OASISSettings.Value;
            _emailService = emailService;
        }

        //public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        public IAvatar Authenticate(AuthenticateRequest model, string ipAddress)
        {
            IAvatar avatar = AvatarManager.LoadAvatar(model.Email);

            if (!avatar.IsVerified)
                throw new AppException("Avatar has not been verified. Please check your email.");

            if (avatar == null || !BC.Verify(model.Password, avatar.Password))
                throw new AppException("Email or password is incorrect");

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = generateJwtToken(avatar);
            var refreshToken = generateRefreshToken(ipAddress);

            // save refresh token
            //if (avatar.RefreshToken == null)
            //    avatar.RefreshTokens = new List<RefreshToken>();

            avatar.RefreshTokens.Add(refreshToken);
            avatar.JwtToken = jwtToken;
            avatar.RefreshToken = refreshToken.Token;

            //TODO: Get Async working!
            avatar = RemoveAuthDetails(AvatarManager.SaveAvatar(avatar));
           // avatar.RefreshTokens.Add(refreshToken);
            //avatar.RefreshToken = refreshToken.Token;

            return avatar;

            //_context.Update(account);
            //_context.SaveChanges();

            //var response = _mapper.Map<AuthenticateResponse>(avatar);
            //response.JwtToken = jwtToken;
            //response.RefreshToken = refreshToken.Token;
            //return response;
        }

        //public AuthenticateResponse RefreshToken(string token, string ipAddress)
        public IAvatar RefreshToken(string token, string ipAddress)
        {
            (RefreshToken refreshToken, IAvatar avatar) = getRefreshToken(token);

            // replace old refresh token with a new one and save
            var newRefreshToken = generateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            avatar.RefreshTokens.Add(newRefreshToken);

            avatar.JwtToken = generateJwtToken(avatar);
            avatar = RemoveAuthDetails(AvatarManager.SaveAvatar(avatar));
           // avatar.RefreshToken = newRefreshToken.Token;
            return avatar;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, avatar) = getRefreshToken(token);

            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            AvatarManager.SaveAvatar(avatar);

            //_context.Update(account);
            //_context.SaveChanges();
        }

        public IAvatar Register(RegisterRequest model, string origin)
        {
            IEnumerable<IAvatar> avatars = AvatarManager.LoadAllAvatars();

            //TODO: {PERFORMANCE} Add this method to the providers so more efficient.
            //if (_context.Accounts.Any(x => x.Email == model.Email))
            if (avatars.Any(x => x.Email == model.Email))
            //if (AvatarManager.LoadAvatar(model.Email) == null)
            {
                // send already registered error in email to prevent account enumeration
                sendAlreadyRegisteredEmail(model.Email, origin);
                return null;
            }

            // map model to new account object
            //IAvatar avatar = _mapper.Map<IAvatar>(model);
            IAvatar avatar = new Core.Avatar() { FirstName = model.FirstName, LastName = model.LastName, Password = model.Password, Title = model.Title, Email = model.Email, AvatarType = (AvatarType)Enum.Parse(typeof(AvatarType),model.AvatarType), AcceptTerms = model.AcceptTerms };


            // first registered account is an admin
            //var isFirstAccount = _context.Accounts.Count() == 0;

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            
            //TODO: Not sure if this is a good idea or not? Currently you can register as a wizard (admin) or normal user.
            // The normal register screen will create user types but if logged in as a wizard, then they can create other wizards.
            //var isFirstAccount = avatars.Count() == 0;
            //avatar.AvatarType = isFirstAccount ? AvatarType.Wizard : AvatarType.User;

            avatar.CreatedDate = DateTime.UtcNow;
            avatar.VerificationToken = randomTokenString();

            // hash password
            avatar.Password = BC.HashPassword(model.Password);

            // save account
            //  _context.Accounts.Add(account);
            // _context.SaveChanges();
            //AvatarManager.SaveAvatarAsync(avatar);

            //TODO: Get async version working ASAP! :)
            avatar = AvatarManager.SaveAvatar(avatar);
            sendVerificationEmail(avatar, origin);

            return RemoveAuthDetails(avatar);
        }

        public void VerifyEmail(string token)
        {
            //var account = _context.Accounts.SingleOrDefault(x => x.VerificationToken == token);

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatarsWithPasswords().FirstOrDefault(x => x.VerificationToken == token);

            if (avatar == null) throw new AppException("Verification failed");

            avatar.Verified = DateTime.UtcNow;
            avatar.VerificationToken = null;

            AvatarManager.SaveAvatar(avatar);
           //_context.Accounts.Update(account);
           // _context.SaveChanges();
        }

        public void ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            // var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.Email == model.Email);

            // always return ok response to prevent email enumeration
            if (avatar == null) return;

            // create reset token that expires after 1 day
            avatar.ResetToken = randomTokenString();
            avatar.ResetTokenExpires = DateTime.UtcNow.AddDays(24);

            AvatarManager.SaveAvatar(avatar);
           // _context.Accounts.Update(account);
           // _context.SaveChanges();

            // send email
            sendPasswordResetEmail(avatar, origin);
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.ResetToken == model.Token &&
                x.ResetTokenExpires > DateTime.UtcNow);

            //var account = _context.Accounts.SingleOrDefault(x =>
            //    x.ResetToken == model.Token &&
            //    x.ResetTokenExpires > DateTime.UtcNow);

            if (avatar == null)
                throw new AppException("Invalid token");
        }

        public void ResetPassword(ResetPasswordRequest model)
        {
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.ResetToken == model.Token &&
                x.ResetTokenExpires > DateTime.UtcNow);

            //var account = _context.Accounts.SingleOrDefault(x =>
            //    x.ResetToken == model.Token &&
            //    x.ResetTokenExpires > DateTime.UtcNow);

            if (avatar == null)
                throw new AppException("Invalid token");

            // update password and remove reset token
            avatar.Password = BC.HashPassword(model.Password);
            avatar.PasswordReset = DateTime.UtcNow;
            avatar.ResetToken = null;
            avatar.ResetTokenExpires = null;

            AvatarManager.SaveAvatar(avatar);
            //_context.Accounts.Update(avatar);
            // _context.SaveChanges();
        }

        //public IEnumerable<AccountResponse> GetAll()
        //{
        //    return _mapper.Map<IList<AccountResponse>>(AvatarManager.LoadAllAvatars());
        //}

        public IEnumerable<IAvatar> GetAll()
        {
            return AvatarManager.LoadAllAvatars();
        }

        //public AccountResponse GetById(Guid id)
        //{
        //    var account = getAvatar(id);
        //    return _mapper.Map<AccountResponse>(account);
        //}

        public IAvatar GetById(Guid id)
        {
            return getAvatar(id);
        }

        //public AccountResponse Create(CreateRequest model)
        public IAvatar Create(CreateRequest model)
        {
            // validate
            //if (_context.Accounts.Any(x => x.Email == model.Email))
            //   throw new AppException($"Email '{model.Email}' is already registered");

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            if (AvatarManager.LoadAllAvatars().Any(x => x.Email == model.Email))
                throw new AppException($"Email '{model.Email}' is already registered");

            // map model to new account object
            IAvatar avatar = _mapper.Map<IAvatar>(model);
            avatar.CreatedDate = DateTime.UtcNow;
            avatar.Verified = DateTime.UtcNow;

            // hash password
            avatar.Password = BC.HashPassword(model.Password);

            // save account
            // _context.Accounts.Add(account);
            // _context.SaveChanges();
            AvatarManager.SaveAvatar(avatar);

            //return _mapper.Map<AccountResponse>(avatar);
            return RemoveAuthDetails(avatar);
        }

        //public AccountResponse Update(Guid id, UpdateRequest model)
        public IAvatar Update(Guid id, UpdateRequest model)
        {
            IAvatar avatar = getAvatar(id);

            //if (account.Email != model.Email && _context.Accounts.Any(x => x.Email == model.Email))

            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!
            if (AvatarManager.LoadAllAvatars().Any(x => x.Email == model.Email))
                throw new AppException($"Email '{model.Email}' is already taken");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(model.Password))
                avatar.Password = BC.HashPassword(model.Password);

            // copy model to account and save
            _mapper.Map(model, avatar);
            avatar.ModifiedDate = DateTime.UtcNow;

            return RemoveAuthDetails(AvatarManager.SaveAvatar(avatar));

           // _context.Accounts.Update(account);
           // _context.SaveChanges();

            //return _mapper.Map<AccountResponse>(avatar);
        }

        public void Delete(Guid id)
        {
            //TODO: Implement ASAP!
            //AvatarManager.DeleteAvatar(id);

            //var account = getAvatar(id);
            //_context.Accounts.Remove(account);
            //_context.SaveChanges();
        }

        // helper methods

        //private Avatar getAvatar(int id)
        private IAvatar getAvatar(Guid id)
        {
            IAvatar avatar = AvatarManager.LoadAvatar(id);

            if (avatar == null) 
                throw new KeyNotFoundException("Avatar not found");

            //avatar.Password = null;
            return avatar;
        }

        private (RefreshToken, IAvatar) getRefreshToken(string token)
        {
            //var account = _context.Accounts.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!

            List<IAvatar> avatars = (List<IAvatar>)AvatarManager.LoadAllAvatars();


            IAvatar avatar = AvatarManager.LoadAllAvatars().FirstOrDefault(x => x.RefreshTokens.Any(t => t.Token == token));

            if (avatar == null) 
                throw new AppException("Invalid token");

            var refreshToken = avatar.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive) 
                throw new AppException("Invalid token");

            return (refreshToken, avatar);
        }

        private string generateJwtToken(IAvatar account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_OASISSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = randomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private IAvatar RemoveAuthDetails(IAvatar avatar)
        {
          //  avatar.VerificationToken = null; //TODO: Put back in when LIVE!
          
            avatar.Password = null;
            // avatar.RefreshToken = null;
            //avatar.RefreshTokens = null;

            return avatar;
        }

        private string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        private void sendVerificationEmail(IAvatar avatar, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var verifyUrl = $"{origin}/account/verify-email?token={avatar.VerificationToken}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                             <p><code>{avatar.VerificationToken}</code></p>";
            }

            _emailService.Send(
                to: avatar.Email,
                subject: "Sign-up Verification API - Verify Email",
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         {message}"
            );
        }

        private void sendAlreadyRegisteredEmail(string email, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
                message = $@"<p>If you don't know your password please visit the <a href=""{origin}/account/forgot-password"">forgot password</a> page.</p>";
            else
                message = "<p>If you don't know your password you can reset it via the <code>/accounts/forgot-password</code> api route.</p>";

            _emailService.Send(
                to: email,
                subject: "Sign-up Verification API - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>
                         <p>Your email <strong>{email}</strong> is already registered.</p>
                         {message}"
            );
        }

        private void sendPasswordResetEmail(IAvatar avatar, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/account/reset-password?token={avatar.ResetToken}";
                message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to reset your password with the <code>/accounts/reset-password</code> api route:</p>
                             <p><code>{avatar.ResetToken}</code></p>";
            }

            _emailService.Send(
                to: avatar.Email,
                subject: "Sign-up Verification API - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                         {message}"
            );
        }
    }
}
