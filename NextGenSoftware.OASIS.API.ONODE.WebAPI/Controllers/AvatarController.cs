using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/avatar")]
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        private readonly IAvatarService _avatarService;

        public AvatarController(IAvatarService avatarService)
        {
            _avatarService = avatarService;
        }

        public AvatarManager AvatarManager
        {
            get
            {
                return Program.AvatarManager;
            }
        }
        
        [HttpGet("GetTerms")]
        public OASISResult<string> GetTerms()
        {
            return new() { Result = _avatarService.GetTerms(), IsError = false};
        }

        [Authorize]
        [HttpGet("GetAvatarImage/{id}")]
        public OASISResult<AvatarImage> GetAvatarImageById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new() { Result = null, IsError = true, Message = "Unauthorized" };
            return _avatarService.GetAvatarImageById(id);
        }
        
        [Authorize]
        [HttpGet("GetAvatarImageByUsername/{username}")]
        public async Task<OASISResult<AvatarImage>> GetAvatarImageByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarImage>() {IsError = true, Message = "Unauthorized"};
            return await _avatarService.GetAvatarImageByUsername(username);
        }
        
        [Authorize]
        [HttpGet("GetAvatarImageByEmail/{email}")]
        public async Task<OASISResult<AvatarImage>> GetAvatarImageByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<AvatarImage>() {IsError = true, Message = "Unauthorized"};
            return await _avatarService.GetAvatarImageByEmail(email);
        }

        [Authorize]
        [HttpPost("Upload2DAvatarImage")]
        public OASISResult<string> Upload2DAvatarImage(AvatarImage avatarImage)
        {
            // users can get their own account and admins can get any account
            if (avatarImage.AvatarId != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new() { Result = "Image not uploaded", Message = "Unauthorized", IsError = true };
            _avatarService.Upload2DAvatarImage(avatarImage);
            return new() { Result = "Image Uploaded", Message = "Success", IsError = false };
        }

        //[Authorize(AvatarType.Wizard)]
        //[HttpGet("GetThumbnailAvatar/{id:guid}")]
        //public async Task<ApiResponse<IAvatarThumbnail>> GetThumbnailAvatar(Guid id)
        //{
        //    return await _avatarService.GetAvatarThumbnail(id);
        //}

        //[Authorize(AvatarType.Wizard)]
        //[HttpGet("GetAvatarDetail/{id:guid}")]
        //public async Task<ApiResponse<IAvatarDetail>> GetAvatarDetail(Guid id)
        //{
        //    return await _avatarService.GetAvatarDetail(id);
        //}

        //[Authorize(AvatarType.Wizard)]
        //[HttpGet("GetAllAvatarDetail")]
        //public async Task<ApiResponse<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails()
        //{
        //    return await _avatarService.GetAllAvatarDetails();
        //}

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAvatarDetail/{id:guid}")]
        public OASISResult<IAvatarDetail> GetAvatarDetail(Guid id)
        {
            return _avatarService.GetAvatarDetail(id);
        }
        
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAvatarDetailByEmail/{email}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        {
            return await _avatarService.GetAvatarDetailByEmail(email);
        }
        
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAvatarDetailByUsername/{username}")]
        public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        {
            return await _avatarService.GetAvatarDetailByUsername(username);
        }
        
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAllAvatarDetails")]
        public OASISResult<IEnumerable<IAvatarDetail>> GetAllAvatarDetails()
        {
            return _avatarService.GetAllAvatarDetails();
        }

        /// <summary>
        /// Get's all avatars (only works for logged in &amp; authenticated Wizards (Admins)).
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll")]
        public OASISResult<IEnumerable<IAvatar>> GetAll()
        {
            return new() { Result = _avatarService.GetAll(), IsError = false };
        }

        /// <summary>
        /// Get's all avatars (only works for logged in &amp; authenticated Wizards (Admins)) for a given provider. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType" description="test desc"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll/{providerType}")]
        public OASISResult<IEnumerable<IAvatar>> GetAll(ProviderType providerType, bool setGlobally = false)
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
        public OASISResult<IAvatar> GetById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() { Result = null, Message = "Unauthorized", IsError = true };
            var account = _avatarService.GetById(id);
            return new() { Result = account, Message = "Unauthorized", IsError = false };
        }
        
        [Authorize]
        [HttpGet("GetByUsername/{username}")]
        public async Task<OASISResult<IAvatar>> GetByUsername(string username)
        {
            // users can get their own account and admins can get any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() { Message = "Unauthorized", IsError = true};
            var account = await _avatarService.GetByUsername(username);
            return new OASISResult<IAvatar>(account);
        }
        
        [Authorize]
        [HttpGet("GetByEmail/{email}")]
        public async Task<OASISResult<IAvatar>> GetByEmail(string email)
        {
            // users can get their own account and admins can get any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<IAvatar>() { Message = "Unauthorized", IsError = true };
            var account = await _avatarService.GetByEmail(email);
            return new OASISResult<IAvatar>(account);
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
        public OASISResult<IAvatar> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
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
        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams)
        {
            return new() { Result = await AvatarManager.SearchAsync(searchParams), IsError = false };
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon... Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return new() { Result = await AvatarManager.SearchAsync(searchParams), IsError = false};
        }


        /// <summary>
        /// Authenticate and log in using the given avatar credentials. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("authenticate/{providerType}/{setGlobally}")]
        public OASISResult<AuthenticateResponse> Authenticate(AuthenticateRequest model, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
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
        public OASISResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            var response = _avatarService.Authenticate(model, ipAddress());
            if (!response.IsError && response.Avatar != null)
                setTokenCookie(response.Avatar.RefreshToken);
            return new OASISResult<AuthenticateResponse>() { Result = response };
        }
        
        [HttpPost("AuthenticateToken/{token}")]
        public OASISResult<string> Authenticate(string token)
        {
            return _avatarService.ValidateAccountToken(token);
        }

        /// <summary>
        /// Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp; authenticated.
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh-token")]
        public OASISResult<IAvatar> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _avatarService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return new OASISResult<IAvatar>() {Result = response, IsError = false};
        }

        /// <summary>
        /// Refresh and generate a new JWT Security Token. This will only work if you are already logged in &amp; authenticated. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("refresh-token/{providerType}/{setGlobally}")]
        public OASISResult<IAvatar> RefreshToken(ProviderType providerType, bool setGlobally = false)
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
        public OASISResult<string> RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return new() { Result = "Token is required", IsError = true };

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Avatar.OwnsToken(token) && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new() { Result = "Unauthorized", IsError = true };

            _avatarService.RevokeToken(token, ipAddress());
            return new() { Result = "Token revoked", IsError = false };
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
        public OASISResult<string> RevokeToken(RevokeTokenRequest model, ProviderType providerType, bool setGlobally = false)
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
        public OASISResult<IAvatar> Register(RegisterRequest model)
        {
            object avatarTypeObject = null;

            if (!Enum.TryParse(typeof(AvatarType), model.AvatarType, out avatarTypeObject))
                return new() { Result = null, Message = string.Concat("ERROR: AvatarType needs to be one of the values found in AvatarType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(AvatarType))), IsError = true};

            IAvatar avatar = _avatarService.Register(model, Request.Headers["origin"]);

            if (avatar != null)
            {
                avatar.Password = null;
                return new() { Result = avatar, Message = "Avatar registration successful, please check your email for verification instructions.", IsError = false};
            }
            else
                return new() { Result = null, IsError = true, Message = "ERROR: Avatar already registered." };
        }

        /// <summary>
        /// Register a new avatar. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register/{providerType}/{setGlobally}")]
        public OASISResult<IAvatar> Register(RegisterRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Register(model);
        }


        /// <summary>
        /// Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by the link in the email.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("verify-email")]
        public OASISResult<bool> VerifyEmail(string token)
        {
            return _avatarService.VerifyEmail(token);
        }

        /// <summary>
        /// Verify a newly created avatar by passing in the validation token sent in the verify email. This method is used by the REST API or other methods that need to POST the data rather than GET.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("verify-email")]
        public OASISResult<bool> VerifyEmail(VerifyEmailRequest model)
        {
            return VerifyEmail(model.Token); 
        }

        /// <summary>
        /// Verify a newly created avatar by passing in the validation token sent in the verify email. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("verify-email/{providerType}/{setGlobally}")]
        public OASISResult<bool> VerifyEmail(VerifyEmailRequest model, ProviderType providerType, bool setGlobally = false)
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
        public OASISResult<string> ForgotPassword(ForgotPasswordRequest model)
        {
            _avatarService.ForgotPassword(model, Request.Headers["origin"]);
            return new() { Result = "Please check your email for password reset instructions", IsError = false, Message = "Success" };
        }

        /// <summary>
        /// This will send a password reset email allowing the user to reset their password. Call the avatar/validate-reset-token method passing in the reset token received in the email. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("forgot-password/{providerType}/{setGlobally}")]
        public OASISResult<string> ForgotPassword(ForgotPasswordRequest model, ProviderType providerType, bool setGlobally = false)
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
        public OASISResult<string> ValidateResetToken(ValidateResetTokenRequest model)
        {
            _avatarService.ValidateResetToken(model);
            return new OASISResult<string>() { Result = "Token is valid", IsError = false, Message = "Success" };
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
        public OASISResult<string> ResetPassword(ResetPasswordRequest model)
        {
            _avatarService.ResetPassword(model);
            return new() { Result = "Password reset successful, you can now login", Message = "Success", IsError = false };
        }

        /// <summary>
        /// Call this method passing in the reset token received in the forgotten password email after first calling the avatar/forgot-password method. Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [HttpPost("reset-password/{providerType}/{setGlobally}")]
        public OASISResult<string> ResetPassword(ResetPasswordRequest model, ProviderType providerType, bool setGlobally = false)
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
        public OASISResult<IAvatar> Create(CreateRequest model)
        {
            return new() { Result = _avatarService.Create(model), IsError = false};
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
        public OASISResult<IAvatar> Create(CreateRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return new() { Result = _avatarService.Create(model), IsError = false};
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
        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            object karmaTypePositiveObject = null;
            object karmaSourceTypeObject = null;

            if (!Enum.TryParse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType,
                out karmaTypePositiveObject))
                return new() {Result = null, Message = string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypePositive enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaTypePositive))), IsError = true};

            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                return new() {Result = null, Message = string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaSourceType))), IsError = true};
            return Program.AvatarManager.AddKarmaToAvatar(avatarId, (KarmaTypePositive)karmaTypePositiveObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc);
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
        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
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
        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            object karmaTypeNegativeObject = null;
            object karmaSourceTypeObject = null;

            if (!Enum.TryParse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType, out karmaTypeNegativeObject))
                return new() {Result = null, Message = string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypeNegative enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaTypeNegative))), IsError = false };
            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
                return new() { Result = null, Message = string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be:\n\n", EnumHelper.GetEnumValues(typeof(KarmaSourceType))) };
            return Program.AvatarManager.RemoveKarmaFromAvatar(avatarId, (KarmaTypeNegative)karmaTypeNegativeObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc);
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
        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType, bool setGlobally = false)
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
        public async Task<OASISResult<IAvatar>> Update(UpdateRequest avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new() { Result = null, IsError = true, Message = "Unauthorized" };

            // only admins can update role
            //if (avatar.AvatarType != AvatarType.Wizard)
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            //model.AvatarType = null;

            //return Ok(_avatarService.Update(id, model));
            return new() { Result = await _avatarService.Update(id, avatar), IsError = false };
        }
        
        [Authorize]
        [HttpPost("UpdateByEmail/{email}")]
        public async Task<OASISResult<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new() { Result = null, IsError = true, Message = "Unauthorized" };
            // only admins can update role
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            return new() { Result = await _avatarService.UpdateByEmail(email, avatar), IsError = false };
        }
        
        [Authorize]
        [HttpPost("UpdateByUsername/{email}")]
        public async Task<OASISResult<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new() { Result = null, IsError = true, Message = "Unauthorized" };
            // only admins can update role
            if (avatar.AvatarType != "Wizard")
                avatar.AvatarType = null;
            return new() { Result = await _avatarService.UpdateByUsername(username, avatar), IsError = false };
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
        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Update(avatar, id);
        }


        /// <summary>
        /// Delete the given avatar. They must be logged in &amp; authenticated for this method to work. 
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}")]
        public OASISResult<string> Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<string>() { Result = "Unauthorized", IsError = true };
            _avatarService.Delete(id);
            return new() { Result = "Account deleted successfully", Message = "Success", IsError = false };
        }
        
        [Authorize]
        [HttpDelete("DeleteByUsername/{username}")]
        public async Task<OASISResult<string>> DeleteByUsername(string username)
        {
            // users can delete their own account and admins can delete any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<string>() { IsError = true, Message = "Unauthorized", Result = "Not Deleted!" };
            await _avatarService.DeleteByUsername(username);
            return new OASISResult<string>("Account deleted successfully");
        }
        
        [Authorize]
        [HttpDelete("DeleteByEmail/{email}")]
        public async Task<OASISResult<string>> DeleteByEmail(string email)
        {
            // users can delete their own account and admins can delete any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return new OASISResult<string>() { IsError = true, Message = "Unauthorized", Result = "Not Deleted!" };
            await _avatarService.DeleteByEmail(email);
            return new OASISResult<string>("Account deleted successfully");
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
        public OASISResult<string> Delete(Guid id, ProviderType providerType, bool setGlobally = false)
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
        [HttpPost("{avatarId:Guid}/{telosAccountName}")]
        public OASISResult<IAvatarDetail> LinkTelosAccountToAvatar(Guid avatarId, string telosAccountName)
        {
            return new() { Result = AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.TelosOASIS, telosAccountName), IsError = false };
        }

        /// <summary>
        /// Link's a given telosAccount to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="telosAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost()]
        public OASISResult<IAvatarDetail> LinkTelosAccountToAvatar2(LinkProviderKeyToAvatar linkProviderKeyToAvatar)
        {
            return new() { Result = AvatarManager.LinkProviderKeyToAvatar(linkProviderKeyToAvatar.AvatarID, ProviderType.TelosOASIS, linkProviderKeyToAvatar.ProviderKey), IsError = false };
        }


        /// <summary>
        /// Link's a given eosioAccountName to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="eosioAccountName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{eosioAccountName}")]
        public OASISResult<IAvatarDetail> LinkEOSIOAccountToAvatar(Guid avatarId, string eosioAccountName)
        {
            return new() {Result = AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.EOSIOOASIS, eosioAccountName), IsError = false};
        }

        /// <summary>
        /// Link's a given holochain AgentID to the given avatar.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="holochainAgentID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{avatarId}/{holochainAgentID}")]
        public OASISResult<IAvatarDetail> LinkHolochainAgentIDToAvatar(Guid avatarId, string holochainAgentID)
        {
            return new() { Result = AvatarManager.LinkProviderKeyToAvatar(avatarId, ProviderType.HoloOASIS, holochainAgentID), IsError = false };
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
        public OASISResult<string> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType)
        {
            return new()
                {Result = AvatarManager.GetProviderKeyForAvatar(avatarUsername, providerType), IsError = false};
        }

        /// <summary>
        /// Get's the private provider key for the given avatar and provider type.
        /// </summary>
        /// <param name="avatarId">The id of the avatar.</param>
        /// <param name="providerType">The id of the avatar.</param>
        /// <returns></returns>
        
         [Authorize]
       [HttpPost("{avatarId}/{providerType}")]
       public OASISResult<string> GetPrivateProviderKeyForAvatar(Guid avatarId, ProviderType providerType)
        {
            return new()
                {Result = AvatarManager.GetPrivateProviderKeyForAvatar(avatarId, providerType), IsError = false};
        }

        [Authorize]
        [HttpGet("GetUMAJsonById/{id}")]
        public async Task<OASISResult<string>> GetUmaJsonById(Guid id)
        {
            return await _avatarService.GetAvatarUmaJsonById(id);
        }
        
        [Authorize]
        [HttpGet("GetUMAJsonByUsername/{username}")]
        public async Task<OASISResult<string>> GetUmaJsonByUsername(string username)
        {
            return await _avatarService.GetAvatarUmaJsonByUsername(username);
        }
        
        [Authorize]
        [HttpGet("GetUMAJsonByMail/{mail}")]
        public async Task<OASISResult<string>> GetUmaJsonMail(string mail)
        {
            return await _avatarService.GetAvatarUmaJsonByMail(mail);
        }

        [Authorize]
        [HttpGet("GetAvatarByJwt")]
        public async Task<OASISResult<IAvatar>> GetAvatarByJwt()
        {
            var id  = User.Claims.FirstOrDefault(i => i.Type == "id")?.Value;
            return await _avatarService.GetAvatarByJwt(new Guid(id ?? string.Empty));
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
