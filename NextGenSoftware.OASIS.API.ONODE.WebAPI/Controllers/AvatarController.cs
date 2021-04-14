using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/avatar")]
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        private readonly IAvatarService _avatarService;

        //public AvatarController(IOptions<OASISSettings> OASISSettings, IAvatarService avatarService) : base(OASISSettings)
        public AvatarController(IAvatarService avatarService)
        {
            _avatarService = avatarService;
        }

        public AvatarManager AvatarManager
        {
            get
            {
                return Program.AvatarManager;

                //if (_avatarManager == null)
                //{
                //    _avatarManager = new AvatarManager(GetAndActivateProvider());
                //    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
                //}

                //return _avatarManager;
            }
        }

        /// <summary>
        /// Get's all avatars (only works for logged in &amp; authenticated Wizards (Admins)).
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll")]
        public ActionResult<IEnumerable<IAvatar>> GetAll()
        {
            return Ok(_avatarService.GetAll());
        }

        /// <summary>
        /// Get's all avatars (only works for logged in &amp; authenticated Wizards (Admins)) for a given provider. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType" description="test desc"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll/{providerType}")]
        public ActionResult<IEnumerable<IAvatar>> GetAll(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return GetAll();
        }

        /// <summary>
        /// Get's the avatar for the given id. You must be logged in &amp; authenticated for this to work.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetById/{id}")]
        public ActionResult<IAvatar> GetById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            var account = _avatarService.GetById(id);
            return Ok(account);
        }

        /// <summary>
        /// Get's the avatar for the given id. You must be logged in &amp; authenticated for this to work. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetById/{id}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return GetById(id);
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon...
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams)
        {
            return Ok(AvatarManager.SearchAsync(searchParams).Result);
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon... Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok(AvatarManager.SearchAsync(searchParams).Result);
        }


        /// <summary>
        /// Authenticate and log in using the given avatar credentials. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("authenticate/{providerType}/{setGlobally}")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Authenticate(model);
        }

        
        /// <summary>
        /// Authenticate and log in using the given avatar credentials.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            AuthenticateResponse response = _avatarService.Authenticate(model, ipAddress());

            if (!response.IsError && response.Avatar != null)
                setTokenCookie(response.Avatar.RefreshToken);

            return Ok(response);
        }

        /// <summary>
        /// Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp; authenticated.
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh-token")]
        public ActionResult<IAvatar> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _avatarService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
            
        }

        /// <summary>
        /// Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp; authenticated. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("refresh-token/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RefreshToken(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RefreshToken();
        }

        /// <summary>
        /// Revoke a given JWT Token (for example, if a user logs out). They must be logged in &amp; authenticated for this method to work.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Avatar.OwnsToken(token) && Avatar.AvatarType.Value != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            _avatarService.RevokeToken(token, ipAddress());
            return Ok(new { message = "Token revoked" });
        }

        /// <summary>
        /// Revoke a given JWT Token (for example, if a user logs out). They must be logged in &amp; authenticated for this method to work. This will only work if you are already logged &amp; authenticated. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("revoke-token/{providerType}/{setGlobally}")]
        public IActionResult RevokeToken(RevokeTokenRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RevokeToken(model);
        }

        /// <summary>
        /// Register a new avatar.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
            object avatarTypeObject = null;

            if (!Enum.TryParse(typeof(AvatarType), model.AvatarType, out avatarTypeObject))
                return Ok(string.Concat("ERROR: AvatarType needs to be one of the values found in AvatarType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(AvatarType))));

            IAvatar avatar = _avatarService.Register(model, Request.Headers["origin"]);

            if (avatar != null)
            {
                avatar.Password = null;
                return Ok(new { avatar, message = "Avatar registration successful, please check your email for verification instructions." });
            }
            else
                return Ok(new { message = "ERROR: Avatar already registered." });
        }

        /// <summary>
        /// Register a new avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register/{providerType}/{setGlobally}")]
        public IActionResult Register(RegisterRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Register(model);
        }


        /// <summary>
        /// Verify a newly created avatar by passing in the validation token sent in the verify email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("verify-email")]
        public IActionResult VerifyEmail(string token)
        {
            _avatarService.VerifyEmail(token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        /// <summary>
        /// Verify a newly created avatar by passing in the validation token sent in the verify email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
            _avatarService.VerifyEmail(model.Token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        /// <summary>
        /// Verify a newly created avatar by passing in the validation token sent in the verify email. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("verify-email/{providerType}/{setGlobally}")]
        public IActionResult VerifyEmail(VerifyEmailRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return VerifyEmail(model);
        }

        /// <summary>
        /// This will send a password reset email allowing the user to reset their password. Call the avatar/validate-reset-token method passing in the reset token received in the email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model)
        {
            _avatarService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new { message = "Please check your email for password reset instructions" });
        }

        /// <summary>
        /// This will send a password reset email allowing the user to reset their password. Call the avatar/validate-reset-token method passing in the reset token received in the email. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("forgot-password/{providerType}/{setGlobally}")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return ForgotPassword(model);
        }

        /// <summary>
        /// Call this method passing in the reset token received in the forgotten password email after first calling the avatar/forgot-password method.
        /// </summary>
        /// <param name = "model" ></ param >
        /// < returns ></ returns >
        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            _avatarService.ValidateResetToken(model);
            return Ok(new { message = "Token is valid" });
        }

        /// <summary>
        /// Call this method passing in the reset token received in the forgotten password email after first calling the avatar/forgot-password method. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        //[HttpPost("validate-reset-token/{providerType}/{setGlobally}")]
        //public IActionResult ValidateResetToken(ValidateResetTokenRequest model, ProviderType providerType, bool setGlobally = false)
        //{
        //    GetAndActivateProvider(providerType, setGlobally);
        //    return ValidateResetToken(model);
        //}

        /// <summary>
        /// Call this method passing in the reset token received in the forgotten password email after first calling the avatar/forgot-password method.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordRequest model)
        {
            _avatarService.ResetPassword(model);
            return Ok(new { message = "Password reset successful, you can now login" });
        }

        /// <summary>
        /// Call this method passing in the reset token received in the forgotten password email after first calling the avatar/forgot-password method. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("reset-password/{providerType}/{setGlobally}")]
        public IActionResult ResetPassword(ResetPasswordRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return ResetPassword(model);
        }

        /// <summary>
        /// Allows a Wizard(Admin) to create new avatars including other wizards.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("Create/{model}")]
        public ActionResult<IAvatar> Create(CreateRequest model)
        {
            return Ok(_avatarService.Create(model));
        }

        /// <summary>
        /// Allows a Wizard(Admin) to create new avatars including other wizards. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("Create/{model}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> Create(CreateRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok(_avatarService.Create(model));
        }

        /*
        /// <summary>
        /// Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatar">The avatar to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar")]
        public ActionResult<IAvatar> AddKarmaToAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            return Ok(Program.AvatarManager.AddKarmaToAvatar(addKarmaToAvatarRequest.Avatar, addKarmaToAvatarRequest.KarmaType, addKarmaToAvatarRequest.karmaSourceType, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc));
        }

        /// <summary>
        /// Add positive karma to the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar">The avatar to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return AddKarmaToAvatar(addKarmaToAvatarRequest);
        }*/

        /// <summary>
        /// Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatarId}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            object karmaTypePositiveObject = null;
            object karmaSourceTypeObject = null;

            if (!Enum.TryParse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType, out karmaTypePositiveObject))
                return Ok(string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypePositive enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaTypePositive))));

            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                return Ok(string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaSourceType))));

            return Ok(Program.AvatarManager.AddKarmaToAvatar(avatarId, (KarmaTypePositive)karmaTypePositiveObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc));
        }

        /// <summary>
        /// Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma was earnt (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was earnt. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatarId}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Ok(AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest));
        }

        /*
        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        /// </summary>
        /// <param name="avatar">The avatar to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            return Ok(Program.AvatarManager.RemoveKarmaFromAvatar(addKarmaToAvatarRequest.Avatar, addKarmaToAvatarRequest.KarmaType, addKarmaToAvatarRequest.karmaSourceType, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc));
        }

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar">The avatar to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RemoveKarmaFromAvatar(addKarmaToAvatarRequest);
        }
        */

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatarId}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            object karmaTypeNegativeObject = null;
            object karmaSourceTypeObject = null;

            if (!Enum.TryParse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType, out karmaTypeNegativeObject))
                return Ok(string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypeNegative enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaTypeNegative))));

            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                return Ok(string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaSourceType))));

            return Ok(Program.AvatarManager.RemoveKarmaFromAvatar(avatarId, (KarmaTypeNegative)karmaTypeNegativeObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc));
        }

        /// <summary>
        /// Remove karma from the given avatar. They must be logged in &amp; authenticated for this method to work. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game, karamSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karamSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatarId}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        //[Authorize(AvatarType.Wizard)]
        //[HttpPost("Create/{model}/{providerType}")]
        //public ActionResult<IAvatar> Create(CreateRequest model, ProviderType providerType)
        //{
        //    GetAndActivateProvider(providerType);
        //    return Ok(_avatarService.Create(model));
        //}



        //[Authorize]
        //[HttpGet("GetByIdForProvider/{id}/{providerType}")]
        //public async Task<IAvatar> Get(Guid id, ProviderType providerType)
        //{
        //    //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
        //    GetAndActivateProvider(providerType);
        //    return await AvatarManager.LoadAvatarAsync(id, providerType);
        //}



        /// <summary>
        /// Update the given avatar. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Update/{id}")]
        //public ActionResult<IAvatar> Update(Core.Avatar avatar, Guid id)
        public ActionResult<IAvatar> Update(UpdateRequest avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            // only admins can update role
            //if (avatar.AvatarType != AvatarType.Wizard)
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            //model.AvatarType = null;

            //return Ok(_avatarService.Update(id, model));
            return Ok(_avatarService.Update(id, avatar));
        }

        /// <summary>
        /// Update the given avatar. They must be logged in &amp; authenticated for this method to work. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatar">The avatar to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Update/{id}/{providerType}/{setGlobally}")]
        //public ActionResult<IAvatar> Update(Guid id, Core.Avatar avatar, ProviderType providerType, bool setGlobally = false)
        public ActionResult<IAvatar> Update(Guid id, UpdateRequest avatar, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Update(avatar, id);
        }


        /// <summary>
        /// Delete the given avatar. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}")]
        public IActionResult Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            _avatarService.Delete(id);

            return Ok(new { message = "Account deleted successfully" });
        }

        /// <summary>
        /// Delete the given avatar. They must be logged in &amp; authenticated for this method to work. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public IActionResult Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Delete(id);
        }
       
        /// <summary>
        /// Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{telosAccountName}")]
        public IActionResult LinkTelosAccountToAvatar(Guid avatarId, string telosAccountName)
        {
            return Ok(AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.TelosOASIS, telosAccountName));
        }

        /// <summary>
        /// Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public IActionResult LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return Ok(AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName));
        }

        /// <summary>
        /// Link's a given holochain AgentID to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentID}")]
        public IActionResult LinkHolochainAgentIDToAvatar(Guid avatarId, string holochainAgentID)
        {
            return Ok(AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentID));
        }

        ///// <summary>
        ///// Get's the provider key for the given avatar and provider type.
        ///// </summary>
        ///// <param name="avatarId">The id of the avatar.</param>
        ///// <param name="providerType">The provider type.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("{avatarId}")]
        //public IActionResult GetProviderKeyForAvatar(Guid avatarId, ProviderType providerType)
        //{
        //    return Ok(AvatarManager.GetProviderKeyForAvatar(avatarId, providerType));
        //}

        /// <summary>
        /// Get's the provider key for the given avatar and provider type.
        /// </summary>
        /// <param name="avatarUsername">The avatar username.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarUsername}/{providerType}")]
        public IActionResult GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        {
            return Ok(AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType));
        }

        /// <summary>
        /// Get's the private provider key for the given avatar and provider type.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="providerType">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
       [HttpPost("{avatarId}/{providerType}")]
       public IActionResult GetPrivateProviderKeyForAvatar(Guid avatarId, ProviderType providerType)
       {
           return Ok(AvatarManager.GetPrivateProviderKeyForAvatar(avatarId, providerType));
       }

        /*
       /// <summary>
       /// Get's all the provider keys for the given avatar.
       /// </summary>
       /// <param name="avatarId">The id of the avatar.</param>
       /// <returns></returns>
       [Authorize]
       [HttpPost("{avatarId}")]
       public IActionResult GetAllProviderKeysForAvatar(Guid avatarId)
       {
           return Ok(AvatarManager.GetAllProviderKeysForAvatar(avatarId));
       }

       /// <summary>
       /// Get's all the private provider keys for the given avatar.
       /// </summary>
       /// <param name="avatarId">The id of the avatar.</param>
       /// <returns></returns>
       [Authorize]
       [HttpPost("{avatarId}")]
       public IActionResult GetAllPrivateProviderKeysForAvatar(Guid avatarId)
       {
           return Ok(AvatarManager.GetAllPrivateProviderKeysForAvatar(avatarId));
       }*/
    
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
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
