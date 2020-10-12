using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.API.WebAPI;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/avatar")]
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        private readonly IAvatarService _avatarService;

        public AvatarController(IOptions<OASISSettings> OASISSettings, IAvatarService avatarService) : base(OASISSettings)
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

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll")]
        public ActionResult<IEnumerable<IAvatar>> GetAll()
        {
            return Ok(_avatarService.GetAll());
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetAll/{providerType}")]
        public ActionResult<IEnumerable<IAvatar>> GetAll(ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return GetAll();
        }

        [Authorize]
        [HttpGet("GetById/{id}")]
        public ActionResult<IAvatar> GetById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            var account = _avatarService.GetById(id);
            return Ok(account);
        }

        [Authorize]
        [HttpGet("GetById/{id}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> GetById(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return GetById(id);
        }

        [HttpGet("Search/{searchParams}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams)
        {
            return Ok(AvatarManager.SearchAsync(searchParams).Result);
        }

        [HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        public ActionResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            return Ok(AvatarManager.SearchAsync(searchParams).Result);
        }

        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            AuthenticateResponse response = _avatarService.Authenticate(model, ipAddress());

            if (!response.IsError && response.Avatar != null)
                setTokenCookie(response.Avatar.RefreshToken);

            return Ok(response);
        }

        [HttpPost("authenticate/{providerType}/{setGlobally}")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Authenticate(model);
        }

        [HttpPost("refresh-token")]
        public ActionResult<IAvatar> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _avatarService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("refresh-token/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RefreshToken(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RefreshToken();
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Avatar.OwnsToken(token) && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            _avatarService.RevokeToken(token, ipAddress());
            return Ok(new { message = "Token revoked" });
        }

        [Authorize]
        [HttpPost("revoke-token/{providerType}/{setGlobally}")]
        public IActionResult RevokeToken(RevokeTokenRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RevokeToken(model);
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
            IAvatar avatar = _avatarService.Register(model, Request.Headers["origin"]);

            if (avatar != null)
            {
                avatar.Password = null;
                return Ok(new { avatar, message = "Avatar registration successful, please check your email for verification instructions." });
            }
            else
                return Ok(new { message = "ERROR: Avatar already registered." });
        }

        [HttpPost("register/{providerType}/{setGlobally}")]
        public IActionResult Register(RegisterRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Register(model);
        }

        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
            _avatarService.VerifyEmail(model.Token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        [HttpPost("verify-email/{providerType}/{setGlobally}")]
        public IActionResult VerifyEmail(VerifyEmailRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return VerifyEmail(model);
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model)
        {
            _avatarService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new { message = "Please check your email for password reset instructions" });
        }

        [HttpPost("forgot-password/{providerType}/{setGlobally}")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return ForgotPassword(model);
        }

        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            _avatarService.ValidateResetToken(model);
            return Ok(new { message = "Token is valid" });
        }

        [HttpPost("validate-reset-token/{providerType}/{setGlobally}")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return ValidateResetToken(model);
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordRequest model)
        {
            _avatarService.ResetPassword(model);
            return Ok(new { message = "Password reset successful, you can now login" });
        }

        [HttpPost("reset-password/{providerType}/{setGlobally}")]
        public IActionResult ResetPassword(ResetPasswordRequest model, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return ResetPassword(model);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost("Create/{model}")]
        public ActionResult<IAvatar> Create(CreateRequest model)
        {
            return Ok(_avatarService.Create(model));
        }

        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(IAvatar avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Ok(Program.AvatarManager.AddKarmaToAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }

        [Authorize]
        [HttpPost("AddKarmaToAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> AddKarmaToAvatar(IAvatar avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return AddKarmaToAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(IAvatar avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc)
        {
            return Ok(Program.AvatarManager.RemoveKarmaFromAvatarAsync(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc));
        }

        [Authorize]
        [HttpPost("RemoveKarmaFromAvatar/{avatar}/{karmaType}/{karmaSourceType}/{karamSourceTitle}/{karmaSourceDesc}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> RemoveKarmaFromAvatar(IAvatar avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return RemoveKarmaFromAvatar(avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
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




        [Authorize]
        [HttpPut("Update/{id}")]
        public ActionResult<IAvatar> Update(Core.Avatar avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            // only admins can update role
            if (Avatar.AvatarType != AvatarType.Wizard)
                avatar.AvatarType = Avatar.AvatarType;
            //model.AvatarType = null;

            //return Ok(_avatarService.Update(id, model));
            return Ok(_avatarService.Update(id, avatar));
        }

        [Authorize]
        [HttpPut("Update/{id}/{providerType}/{setGlobally}")]
        public ActionResult<IAvatar> Update(Guid id, Core.Avatar avatar, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Update(avatar, id);
        }

       

        [Authorize]
        [HttpDelete("{id:Guid}")]
        public IActionResult Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            _avatarService.Delete(id);

            return Ok(new { message = "Account deleted successfully" });
        }

        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public IActionResult Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return Delete(id);
        }

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
