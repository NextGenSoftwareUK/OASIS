using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        [HttpPost("authenticate")]
        public ActionResult<IAvatar> Authenticate(AuthenticateRequest model)
        {
            var response = _avatarService.Authenticate(model, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("authenticate/{providerType}")]
        public ActionResult<IAvatar> Authenticate(AuthenticateRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
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

        [HttpPost("refresh-token/{providerType}")]
        public ActionResult<IAvatar> RefreshToken(ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
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
        [HttpPost("revoke-token/{providerType}")]
        public IActionResult RevokeToken(RevokeTokenRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return RevokeToken(model);
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
            IAvatar avatar = _avatarService.Register(model, Request.Headers["origin"]);

            if (avatar != null)
            {
                avatar.Password = null;
                return Ok(new { avatar,  message = "Avatar registration successful, please check your email for verification instructions." });
            }
            else
                return Ok(new { message = "ERROR: Avatar already registered." });
        }

        [HttpPost("register/{providerType}")]
        public IActionResult Register(RegisterRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return Register(model);
        }

        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
            _avatarService.VerifyEmail(model.Token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        [HttpPost("verify-email/{providerType}")]
        public IActionResult VerifyEmail(VerifyEmailRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return VerifyEmail(model);
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model)
        {
            _avatarService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new { message = "Please check your email for password reset instructions" });
        }

        [HttpPost("forgot-password/{providerType}")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return ForgotPassword(model);
        }

        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            _avatarService.ValidateResetToken(model);
            return Ok(new { message = "Token is valid" });
        }

        [HttpPost("validate-reset-token/{providerType}")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return ValidateResetToken(model);
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordRequest model)
        {
            _avatarService.ResetPassword(model);
            return Ok(new { message = "Password reset successful, you can now login" });
        }

        [HttpPost("reset-password/{providerType}")]
        public IActionResult ResetPassword(ResetPasswordRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return ResetPassword(model);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        public ActionResult<IEnumerable<IAvatar>> GetAll()
        {
            return Ok(_avatarService.GetAll());
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        public ActionResult<IEnumerable<IAvatar>> GetAll(ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return GetAll();
        }

        [Authorize]
        [HttpGet("{id:Guid}")]
        public ActionResult<IAvatar> GetById(Guid id)
        {
            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            var account = _avatarService.GetById(id);
            return Ok(account);
        }

        [Authorize]
        [HttpGet("{id:Guid}/{providerType}")]
        public ActionResult<IAvatar> GetById(Guid id, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return GetById(id);
        }

        //[Authorize]
        //[HttpGet("GetByIdForProvider/{id}/{providerType}")]
        //public async Task<IAvatar> Get(Guid id, ProviderType providerType)
        //{
        //    //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
        //    GetAndActivateProvider(providerType);
        //    return await AvatarManager.LoadAvatarAsync(id, providerType);
        //}

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        public ActionResult<IAvatar> Create(CreateRequest model)
        {
            return Ok(_avatarService.Create(model));
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        public ActionResult<IAvatar> Create(CreateRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return Ok(_avatarService.Create(model));
        }

        [Authorize]
        [HttpPut("{id:Guid}")]
        public ActionResult<Avatar> Update(Guid id, UpdateRequest model)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            // only admins can update role
            if (Avatar.AvatarType != AvatarType.Wizard)
                model.AvatarType = null;

            return Ok(_avatarService.Update(id, model));
        }

        [Authorize]
        [HttpPut("{id:Guid}/{providerType}")]
        public ActionResult<Avatar> Update(Guid id, UpdateRequest model, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
            return Update(id, model);
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
        [HttpDelete("{id:Guid}/{providerType}")]
        public IActionResult Delete(Guid id, ProviderType providerType)
        {
            GetAndActivateProvider(providerType);
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
