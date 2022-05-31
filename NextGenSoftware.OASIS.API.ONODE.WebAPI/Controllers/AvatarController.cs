using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        private readonly IAvatarService _avatarService;
        private KeyManager _keyManager = null;

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _keyManager = new KeyManager(result.Result, Program.AvatarManager);
                }

                return _keyManager;
            }
        }

        public AvatarController(IAvatarService avatarService)
        {
            _avatarService = avatarService;
            //KeyManager.Init()
            //KeyManager keyManager = new KeyManager()
        }

        /// <summary>
        ///     Register a new avatar.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<OASISResult<IAvatar>> Register(RegisterRequest model)
        {
            return FormatResponse(await _avatarService.RegisterAsync(model, Request.Headers["origin"]));
        }

        /// <summary>
        ///     Register a new avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to
        ///     be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("register/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> Register(RegisterRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Register(model);
        }


        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the link in the email.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("verify-email")]
        public async Task<OASISResult<bool>> VerifyEmail(string token)
        {
            return FormatResponse(await _avatarService.VerifyEmail(token));
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the link in the email. 
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to
        ///     be used for all future requests too.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpGet("verify-email/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> VerifyEmail(string token, ProviderType providerType, bool setGlobally = false)
        {
            return FormatResponse(await _avatarService.VerifyEmail(token));
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by
        ///     the REST API or other methods that need to POST the data rather than GET.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("verify-email")]
        public async Task<OASISResult<bool>> VerifyEmail(VerifyEmailRequest model)
        {
            return FormatResponse(await VerifyEmail(model.Token));
        }

        /// <summary>
        ///     Verify a newly created avatar by passing in the validation token sent in the verify email. 
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to
        ///     be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("verify-email/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> VerifyEmail(VerifyEmailRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await VerifyEmail(model);
        }

        /// <summary>
        /// Authenticate and log in using the given avatar credentials.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("authenticate")]
        public async Task<OASISResult<IAvatar>> Authenticate(AuthenticateRequest model)
        {
            var response = await Program.AvatarManager.AuthenticateAsync(model.Username, model.Password, ipAddress());

            if (!response.IsError && response.Result != null)
                setTokenCookie(response.Result.RefreshToken);

            return FormatResponse(response);
        }


        /// <summary>
        /// Authenticate and log in using the given avatar credentials. 
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("authenticate/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> Authenticate(AuthenticateRequest model, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Authenticate(model);
        }

        /// <summary>
        /// Authenticate and log in using the given JWT Token.
        /// </summary>
        /// <param name="JWTToken"></param>
        /// <returns></returns>
        [HttpPost("authenticate-token/{JWTToken}")]
        public async Task<OASISResult<string>> Authenticate(string JWTToken)
        {
            return FormatResponse(await _avatarService.ValidateAccountToken(JWTToken));
        }

        /// <summary>
        /// Authenticate and log in using the given JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="JWTToken"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("authenticate-token/{JWTToken}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> Authenticate(string JWTToken, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Authenticate(JWTToken);
        }

        /// <summary>
        ///     Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp;
        ///     authenticated.
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh-token")]
        public async Task<OASISResult<IAvatar>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = await _avatarService.RefreshToken(refreshToken, ipAddress());

            if (!response.IsError && response.Result != null)
                setTokenCookie(response.Result.RefreshToken);

            return FormatResponse(response);
        }

        /// <summary>
        ///     Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp;
        ///     authenticated. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used
        ///     only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("refresh-token/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> RefreshToken(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RefreshToken();
        }

        /// <summary>
        ///     Revoke a given JWT Token (for example, if a user logs out). 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<OASISResult<string>> RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return new OASISResult<string>() { Result = "Token is required", IsError = true };

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Avatar.OwnsToken(token) && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<string>() { Result = "Unauthorized", IsError = true };

            return FormatResponse(await _avatarService.RevokeToken(token, ipAddress()));
        }

        /// <summary>
        ///     Revoke a given JWT Token (for example, if a user logs out). They must be logged in &amp; authenticated for this
        ///     method to work. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used
        ///     for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> RevokeToken(RevokeTokenRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RevokeToken(model);
        }

        /// <summary>
        ///     This will send a password reset email allowing the user to reset their password. Call the
        ///     avatar/validate-reset-token method passing in the reset token received in the email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("forgot-password")]
        public async Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model)
        {
            return FormatResponse(await _avatarService.ForgotPassword(model, Request.Headers["origin"]));
        }

        /// <summary>
        ///     This will send a password reset email allowing the user to reset their password. Call the
        ///     avatar/validate-reset-token method passing in the reset token received in the email. 
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("forgot-password/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await ForgotPassword(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// < returns></returns>
        [HttpPost("validate-reset-token")]
        public async Task<OASISResult<string>> ValidateResetToken(ValidateResetTokenRequest model)
        {
            return FormatResponse(await _avatarService.ValidateResetToken(model));
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// < returns></returns>
        [HttpPost("validate-reset-token/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> ValidateResetToken(ValidateResetTokenRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await ValidateResetToken(model);
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("reset-password")]
        public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model)
        {
            return FormatResponse(await _avatarService.ResetPassword(model));
        }

        /// <summary>
        ///     Call this method passing in the reset token received in the forgotten password email after first calling the
        ///     avatar/forgot-password method. Pass in the provider you wish to use. Set the setglobally flag to false for this
        ///     provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("reset-password/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await ResetPassword(model);
        }

        /// <summary>
        ///     Allows a Wizard(Admin) to create new avatars including other wizards.
        ///     Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("create/{model}")]
        public async Task<OASISResult<IAvatar>> Create(CreateRequest model)
        {
            return FormatResponse(await _avatarService.Create(model));
        }

        /// <summary>
        ///     Allows a Wizard(Admin) to create new avatars including other wizards.
        ///     Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all
        ///     future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("create/{model}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> Create(CreateRequest model, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await _avatarService.Create(model);
        }

        /// <summary>
        /// Get's the terms &amp; services agreement for creating an avatar and joining the OASIS.
        /// </summary>
        /// <returns></returns>
        [HttpGet("get-terms")]
        public async Task<OASISResult<string>> GetTerms()
        {
            return FormatResponse(await _avatarService.GetTerms());
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their id. Pass in the provider you wish to use.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait/{id}")]
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarPortrait>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await _avatarService.GetAvatarPortraitById(id));
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their id. 
        /// Only works for logged in users. Use Authenticate endpoint first to obtain JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAvatarPortraitById(id);
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-username/{username}")]
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarPortrait> { IsError = true, Message = "Unauthorized" };

            return FormatResponse(await _avatarService.GetAvatarPortraitByUsername(username));
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their username. Pass in the provider you wish to us.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAvatarPortraitByUsername(username);
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their email.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-email/{email}")]
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarPortrait> { IsError = true, Message = "Unauthorized" };

            return FormatResponse(await _avatarService.GetAvatarPortraitByEmail(email));
        }

        /// <summary>
        /// Get's the avatar's portrait (2D Image) using their email. Pass in the provider you wish to use.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-avatar-portrait-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAvatarPortraitByEmail(email);
        }

        /// <summary>
        /// Upload's the avatar's portrait (2D Image), which is displayed on the web portal or on web OAPP's.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarPortrait"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("upload-avatar-portrait")]
        public async Task<OASISResult<bool>> UploadAvatarPortrait(AvatarPortrait avatarPortrait)
        {
            // users can get their own account and admins can get any account
            if (avatarPortrait.AvatarId != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool>()
                { Result = false, Message = "Image not uploaded. Unauthorized", IsError = true };

            return FormatResponse(await _avatarService.UploadAvatarPortrait(avatarPortrait));
        }

        /// <summary>
        /// Upload's an avatar's portrait (2D Image), which is displayed on the web portal or on web OAPP's.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarPortrait"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("upload-avatar-portrait/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> UploadAvatarPortrait(AvatarPortrait avatarPortrait, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await UploadAvatarPortrait(avatarPortrait);
        }

        /// <summary>
        /// Get's the avatar's details for a given id. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-id/{id:guid}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id)
        {
            return FormatResponse(await Program.AvatarManager.LoadAvatarDetailAsync(id));
        }

        /// <summary>
        /// Get's the avatar's details for a given id. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-id/{id:guid}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAvatarDetail(id);
        }

        /// <summary>
        /// Get's the avatar's details for a given email. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-email/{email}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
            return FormatResponse(await Program.AvatarManager.LoadAvatarDetailByEmailAsync(email));
        }

        /// <summary>
        /// Get's the avatar's details for a given email. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAvatarDetailByEmail(email);
        }

        /// <summary>
        /// Get's the avatar's details for a given username. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-username/{username}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
            return FormatResponse(await Program.AvatarManager.LoadAvatarDetailByUsernameAsync(username));
        }

        /// <summary>
        /// Get's the avatar's details for a given username. Contains their address, DOB, Karma, XP, Level, Portrait (2D Image), 3DModel, HeartRateData, Chakras, Aurua, Gifts, Stats (HP, Mana, Energy &amp; Staminia), GeneKeys, HumanDesign, Skills, Attributes (Strength, Speed, Dexterity, Toughness, Wisdom, Intelligence, Magic, Vitality &amp; Endurance), SuperPowers, Spells, Achievements &amp; Inventory. They can also access the full Omniverse from inside their avatar. More to come soon... ;-)
        /// Only works for logged in &amp; authenticated Wizards (Admins) or your own avatar. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAvatarDetailByUsername(username);
        }

        /// <summary>
        /// Get's all the avatar details within The OASIS.
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatar-details")]
        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        {
            return FormatResponse(await Program.AvatarManager.LoadAllAvatarDetailsAsync());
        }

        /// <summary>
        /// Get's all the avatar details within The OASIS.
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatar-details/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAllAvatarDetails();
        }

        /// <summary>
        /// Get's all avatars within The OASIS.
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatars")]
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAll()
        {
            return FormatResponse(await Program.AvatarManager.LoadAllAvatarsAsync());
        }

        /// <summary>
        /// Get's all avatars within The OASIS. 
        /// Only works for logged in &amp; authenticated Wizards (Admins). Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-all-avatars/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAll(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetAll();
        }

        /// <summary>
        /// Get's the avatar for the given id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-id/{id}")]
        public async Task<OASISResult<IAvatar>> GetById(Guid id)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar> { Result = null, Message = "Unauthorized", IsError = true };

            result = await Program.AvatarManager.LoadAvatarAsync(id);

            return FormatResponse(result);
        }

        /// <summary>
        /// Get's the avatar for the given id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetById(id);
        }

        /// <summary>
        /// Get's the avatar for the given username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-username/{username}")]
        public async Task<OASISResult<IAvatar>> GetByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar> { Message = "Unauthorized", IsError = true };

            //return await _avatarService.GetByUsername(username);
            return FormatResponse(await Program.AvatarManager.LoadAvatarAsync(username));
        }

        /// <summary>
        /// Get's the avatar for the given username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> GetByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetByUsername(username);
        }

        /// <summary>
        /// Get's the avatar for the given email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-email/{email}")]
        public async Task<OASISResult<IAvatar>> GetByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar> { Message = "Unauthorized", IsError = true };

            //return await _avatarService.GetByEmail(email);
            return FormatResponse(await Program.AvatarManager.LoadAvatarByEmailAsync(email));
        }

        /// <summary>
        /// Get's the avatar for the given email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> GetByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetByUsername(email);
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon...
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [HttpPost("search")]
        public async Task<OASISResult<ISearchResults>> SearchAvatar(SearchParams searchParams)
        {
            return FormatResponse(await _avatarService.Search(searchParams));
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon... 
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("search/{providerType}/{setGlobally}")]
        public async Task<OASISResult<ISearchResults>> SearchAvatar(SearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await _avatarService.Search(searchParams);
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}")]
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            return FormatResponse(await _avatarService.AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest));
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Remove karma from the given avatar. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}")]
        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            return FormatResponse(await _avatarService.RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest));
        }

        /// <summary>
        ///     Remove karma from the given avatar. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or
        ///     true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Update the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-id/{id}")]
        public async Task<OASISResult<IAvatar>> Update(UpdateRequest avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await _avatarService.Update(id, avatar));
        }

        /// <summary>
        ///     Update the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for
        ///     it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatar">The avatar to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-id/{id}/{providerType}/{setGlobally}")]
        //public ActionResult<IAvatar> Update(Guid id, Core.Avatar avatar, ProviderType providerType, bool setGlobally = false)
        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar, ProviderType providerType,
            bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Update(avatar, id);
        }

        /// <summary>
        /// Update the given avatar using their email address.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-email/{email}")]
        public async Task<OASISResult<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await _avatarService.UpdateByEmail(email, avatar));
        }

        /// <summary>
        /// Update the given avatar using their email address.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await UpdateByEmail(avatar, email);
        }

        /// <summary>
        /// Update the given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="username"></param>
        [Authorize]
        [HttpPost("update-by-username/{username}")]
        public async Task<OASISResult<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await _avatarService.UpdateByUsername(username, avatar));
        }

        /// <summary>
        /// Update the given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        [Authorize]
        [HttpPost("update-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await UpdateByUsername(avatar, username);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}")]
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetail(AvatarDetail avatarDetail, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await Program.AvatarManager.UpdateAvatarDetailAsync(id, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail by the avatar's id. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatarDetail">The avatar detail to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetail(Guid id, AvatarDetail avatarDetail, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await UpdateAvatarDetail(avatarDetail, id);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar email address. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}")]
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByEmailAsync(email, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar email address. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await UpdateAvatarDetailByEmail(avatarDetail, email);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar username. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}")]
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" };

            return FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByUsernameAsync(username, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar username. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await UpdateAvatarDetailByUsername(avatarDetail, username);
        }

        /// <summary>
        ///     Delete the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}")]
        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool> { Result = false, IsError = true };

            return FormatResponse(await Program.AvatarManager.DeleteAvatarAsync(id));
        }

        /// <summary>
        ///     Delete the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Delete(id);
        }

        /// <summary>
        ///     Delete the given avatar using their username.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-username/{username}")]
        public async Task<OASISResult<bool>> DeleteByUsername(string username)
        {
            // users can delete their own account and admins can delete any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool> { IsError = true, Message = "Unauthorized", Result = false };

            return FormatResponse(await Program.AvatarManager.DeleteAvatarByUsernameAsync(username));
        }

        /// <summary>
        ///     Delete the given avatar using their username.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> DeleteByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await DeleteByUsername(username);
        }

        /// <summary>
        ///     Delete the given avatar using their email.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-email/{email}")]
        public async Task<OASISResult<bool>> DeleteByEmail(string email)
        {
            // users can delete their own account and admins can delete any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<bool> { IsError = true, Message = "Unauthorized", Result = false };

            return FormatResponse(await Program.AvatarManager.DeleteAvatarByEmailAsync(email));
        }

        /// <summary>
        ///     Delete the given avatar using their email.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> DeleteByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await DeleteByUsername(email);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}")]
        public async Task<OASISResult<string>> GetUmaJsonById(Guid id)
        {
            return FormatResponse(await _avatarService.GetAvatarUmaJsonById(id));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their id.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> GetUmaJsonById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetUmaJsonById(id);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}")]
        public async Task<OASISResult<string>> GetUmaJsonByUsername(string username)
        {
            return FormatResponse(await _avatarService.GetAvatarUmaJsonByUsername(username));
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> GetUmaJsonByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetUmaJsonByUsername(username);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}")]
        public async Task<OASISResult<string>> GetUmaJsonByEmail(string email)
        {
            return await GetUmaJsonByUsername(email);
        }

        /// <summary>
        /// Get's the 3D Model UMA JSON for a given avatar using their email.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-uma-json-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<string>> GetUmaJsonByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetUmaJsonByEmail(email);
        }

        /// <summary>
        /// Get's the logged in avatar.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-logged-in-avatar")]
        public async Task<OASISResult<IAvatar>> GetLoggedInAvatar()
        {
            return FormatResponse(await _avatarService.GetLoggedInAvatar());
        }

        /// <summary>
        /// Get's the logged in avatar.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-logged-in-avatar/{providerType}/{setGlobally}")]
        public async Task<OASISResult<IAvatar>> GetLoggedInAvatar(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await GetLoggedInAvatar();
        }


        ///// <summary>
        /////     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        ///// </summary>
        ///// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPublicKeyToAvatarByAvatarId")]
        //public OASISResult<bool> LinkProviderPublicKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPublicKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        //}


        ///// <summary>
        /////     Link's a given Avatar to a Providers Public Key (private/public key pairs or username, accountname, unique id, agentId, hash, etc).
        ///// </summary>
        ///// <param name="linkProviderKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPublicKeyToAvatarByUsername")]
        //public OASISResult<bool> LinkProviderPublicKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPublicKeyToAvatar(linkProviderKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        //}

        ///// <summary>
        /////     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        ///// </summary>
        ///// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPrivateKeyToAvatarByAvatarId")]
        //public OASISResult<bool> LinkProviderPrivateKeyToAvatarByAvatarId(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPrivateKeyToAvatar(avatarID, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        //}

        ///// <summary>
        /////     Link's a given Avatar to a Providers Private Key (password, crypto private key, etc).
        ///// </summary>
        ///// <param name="linkProviderPrivateKeyToAvatarParams">The params include AvatarId, ProviderTyper &amp; ProviderKey</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("LinkProviderPrivateKeyToAvatarByUsername")]
        //public OASISResult<bool> LinkProviderPrivateKeyToAvatarByUsername(LinkProviderKeyToAvatarParams linkProviderPrivateKeyToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(linkProviderPrivateKeyToAvatarParams);

        //    if (isValid)
        //        return KeyManager.LinkProviderPrivateKeyToAvatar(linkProviderPrivateKeyToAvatarParams.AvatarUsername, providerTypeToLinkTo, linkProviderPrivateKeyToAvatarParams.ProviderKey, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISResult<bool>(false) { IsError = true, Message = errorMessage };
        //}

        ///*
        ///// <summary>
        /////     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatar")]
        //public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatar(Guid avatarId, string providerTypeToLinkTo, string providerTypeToloadAvatarFrom)
        //{
        //    object providerTypeToLinkToObject = null;
        //    object providerTypeToLoadAvatarFromObject = null;
        //    ProviderType providerTypeToLinkToEnumValue = ProviderType.Default;
        //    ProviderType providerTypeToloadAvatarFromEnumValue = ProviderType.Default;

        //    if (string.IsNullOrEmpty(providerTypeToLinkTo))
        //        return (new OASISResult<KeyPair> { IsError = true, Message = $"The providerTypeToLinkTo param cannot be null. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

        //    if (!string.IsNullOrEmpty(providerTypeToLinkTo) && !Enum.TryParse(typeof(ProviderType), providerTypeToLinkTo, out providerTypeToLinkToObject))
        //        return (new OASISResult<KeyPair> { IsError = true, Message = $"The given providerTypeToLinkTo {providerTypeToLinkTo} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

        //    if (!string.IsNullOrEmpty(providerTypeToloadAvatarFrom) && !Enum.TryParse(typeof(ProviderType), providerTypeToloadAvatarFrom, out providerTypeToLoadAvatarFromObject))
        //        return (new OASISResult<KeyPair> { IsError = true, Message = $"The given providerTypeToloadAvatarFrom {providerTypeToloadAvatarFrom} is invalid. Valid values include: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" });

        //    if (providerTypeToLinkToObject != null)
        //        providerTypeToLinkToEnumValue = (ProviderType)providerTypeToLinkToObject;

        //    if (providerTypeToLoadAvatarFromObject != null)
        //        providerTypeToloadAvatarFromEnumValue = (ProviderType)providerTypeToLoadAvatarFromObject;

        //    return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarId, providerTypeToLinkToEnumValue, providerTypeToloadAvatarFromEnumValue);
        //}*/


        ///// <summary>
        /////     Generate's a new unique private/public keypair &amp; then links to the given avatar for the given provider type.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId")]
        //public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByAvatarId(LinkProviderKeyToAvatarParams generateKeyPairAndLinkProviderKeysToAvatarParams)
        //{
        //    bool isValid;
        //    string errorMessage = "";
        //    ProviderType providerTypeToLinkTo;
        //    ProviderType providerTypeToLoadAvatarFrom;
        //    Guid avatarID;

        //    (isValid, providerTypeToLinkTo, providerTypeToLoadAvatarFrom, avatarID, errorMessage) = ValidateLinkProviderKeyToAvatarParams(generateKeyPairAndLinkProviderKeysToAvatarParams);

        //    if (isValid)
        //        return KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatar(avatarID, providerTypeToLinkTo, providerTypeToLoadAvatarFrom);
        //    else
        //        return new OASISResult<KeyPair>() { IsError = true, Message = errorMessage };
        //}

        ///// <summary>
        /////     Get's a given avatar's unique storage key for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's avatarId.</param>
        ///// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderUniqueStorageKeyForAvatar")]
        //public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderUniqueStorageKeyForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's unique storage key for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the unique storage key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderUniqueStorageKeyForAvatar")]
        //public OASISResult<string> GetProviderUniqueStorageKeyForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderUniqueStorageKeyForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's id.</param>
        ///// <param name="providerType">The provider type to retreive the private key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPrivateKeyForAvatar")]
        //public OASISResult<string> GetProviderPrivateKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's private key for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the private key for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPrivateKeyForAvatar")]
        //public OASISResult<string> GetProviderPrivateKeyForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPrivateKeyForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="avatarId">The Avatar's id.</param>
        ///// <param name="providerType">The provider type to retreive the public keys for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPublicKeysForAvatar")]
        //public OASISResult<List<string>> GetProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPublicKeysForAvatar(avatarId, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <param name="providerType">The provider type to retreive the public keys for.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetProviderPublicKeysForAvatar")]
        //public OASISResult<List<string>> GetProviderPublicKeysForAvatar(string username, ProviderType providerType)
        //{
        //    return KeyManager.GetProviderPublicKeysForAvatar(username, providerType);
        //}

        ///// <summary>
        /////     Get's a given avatar's public keys for the given provider type.
        ///// </summary>
        ///// <param name="username">The Avatar's username.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GetAllProviderPublicKeysForAvatar")]
        //public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatar(string username)
        //{
        //    return KeyManager.GetAllProviderPublicKeysForAvatar(username);
        //}

        ///// <summary>
        /////     Generate's a new unique private/public keypair for a given provider type.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPairForProvider")]
        //public OASISResult<KeyPair> GenerateKeyPairForProvider(ProviderType providerType)
        //{
        //    return KeyManager.GenerateKeyPair(providerType);
        //}

        ///// <summary>
        /////     Generate's a new unique private/public keypair.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("GenerateKeyPair")]
        //public OASISResult<KeyPair> GenerateKeyPair(string keyPrefix)
        //{
        //    return KeyManager.GenerateKeyPair(keyPrefix);
        //}








        /*
        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{id:Guid}/{telosAccountName}")]
        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar(Guid id, string telosAccountName)
        {
            return await _avatarService.LinkProviderKeyToAvatar(id, ProviderType.TelosOASIS, telosAccountName);
        }

        /// <summary>
        ///     Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<OASISResult<IAvatarDetail>> LinkTelosAccountToAvatar2(
            LinkProviderKeyToAvatar linkProviderKeyToAvatar)
        {
            return await _avatarService.LinkProviderKeyToAvatar(linkProviderKeyToAvatar.AvatarID,
                ProviderType.TelosOASIS, linkProviderKeyToAvatar.ProviderUniqueStorageKey);
        }


        /// <summary>
        ///     Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public async Task<OASISResult<IAvatarDetail>> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName);
        }

        /// <summary>
        ///     Link's a given holochain AgentID to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentID}")]
        public async Task<OASISResult<IAvatarDetail>> LinkHolochainAgentIDToAvatar(Guid avatarId,
            string holochainAgentID)
        {
            return await _avatarService.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentID);
        }*/

        ///// <summary>
        /////     Get's the provider key for the given avatar and provider type.
        ///// </summary>
        ///// <param name="avatarUsername">The avatar username.</param>
        ///// <param name="providerType">The provider type.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("{avatarUsername}/{providerType}")]
        //public async Task<OASISResult<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        //{
        //    //return await _avatarService.GetProviderKeyForAvatar(avatarUsername, providerType);
        //    return await Program.AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType);
        //}

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            return HttpContext.Connection.RemoteIpAddress != null
                ? HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString()
                : string.Empty;
        }

        private OASISResult<T> FormatResponse<T>(OASISResult<T> response)
        {
            //Make sure no Error Details are in the Message.
            if (response.IsError && response.Message.IndexOf("\n\nError Details:\n") > 0)
                response.Message = response.Message.Substring(0, response.Message.IndexOf("\n\nError Details:\n"));

            //Replace unsupported chars.
            if (!string.IsNullOrEmpty(response.Message))
            {
                response.Message = response.Message.Replace("\n", " ").Trim();
                response.Message = response.Message.Replace("\r", " ").Trim();
            }

            if (!string.IsNullOrEmpty(response.DetailedMessage))
            {
                response.DetailedMessage = response.DetailedMessage.Replace("\n", " ").Trim();
                response.DetailedMessage = response.DetailedMessage.Replace("\r", " ").Trim();
            }

            return response;
        }
    }
}