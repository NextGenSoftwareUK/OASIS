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
    //[Route("api/[avatar]")] //TODO: Get this working, think better way?
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
       // private AvatarManager _avatarManager;
        private readonly IAvatarService _avatarService;
      //  private readonly IMapper _mapper;

        public AvatarController(IOptions<OASISSettings> OASISSettings, IAvatarService avatarService) : base(OASISSettings)
        {
            _avatarService = avatarService;
        }

        //        public AccountsController(
        //            IAccountService accountService,
        //            IMapper mapper, IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //        {
        //            _accountService = accountService;
        //            _mapper = mapper;
        //        }


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

        //private void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        //{
            
        //}

        /*
        //[AllowAnonymous]
        //[HttpPost("PostAuthenticate", Name ="AuthenticateAvatar")]
        [HttpPost]
        public async Task<IActionResult> PostAuthenticate([FromBody] AuthenticateModel model)
        {
            //IEnumerable<IAvatar>_avatars = await AvatarManager.LoadAllAvatarsAsync();
            //var avatar = await Task.Run(() => _avatars.SingleOrDefault(x => x.Username == model.Username && x.Password == model.Password));

            GetAndActivateProvider();
            IAvatar avatar = AvatarManager.LoadAvatar(model.Username, model.Password);

            if (avatar == null)
                return BadRequest(new { message = "Avatar name or password is incorrect" });

            avatar.Password = null;
            return Ok(avatar);
        }*/

        //TODO: Some of the above code may be useful...
        [HttpPost("authenticate")]
        //public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        public ActionResult<IAvatar> Authenticate(AuthenticateRequest model)
        {
            //GetAndActivateProvider();
            var response = _avatarService.Authenticate(model, ipAddress());
            setTokenCookie(response.RefreshToken);
            //Avatar = response;
            // HttpContext.Items["Avatar"] = response; //TODO: Need to check why I needed to put this in? Used to work without?! hmmm....

            //Avatar = response;
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        //public ActionResult<AuthenticateResponse> RefreshToken()
        public ActionResult<IAvatar> RefreshToken()
        {
          //  GetAndActivateProvider();
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _avatarService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            //GetAndActivateProvider();

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

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
           // GetAndActivateProvider();
            IAvatar avatar = _avatarService.Register(model, Request.Headers["origin"]);

            if (avatar != null)
            {
                avatar.Password = null;
                return Ok(new { avatar,  message = "Avatar registration successful, please check your email for verification instructions." });
            }
            else
                return Ok(new { message = "ERROR: Avatar already registered." });
        }

        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
          //  GetAndActivateProvider();
            _avatarService.VerifyEmail(model.Token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model)
        {
           // GetAndActivateProvider();
            _avatarService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new { message = "Please check your email for password reset instructions" });
        }

        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            //GetAndActivateProvider();
            _avatarService.ValidateResetToken(model);
            return Ok(new { message = "Token is valid" });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordRequest model)
        {
            //GetAndActivateProvider();
            _avatarService.ResetPassword(model);
            return Ok(new { message = "Password reset successful, you can now login" });
        }

        [Authorize(AvatarType.Wizard)]
        [HttpGet]
        //public ActionResult<IEnumerable<AccountResponse>> GetAll()
        public ActionResult<IEnumerable<IAvatar>> GetAll()
        {
           // GetAndActivateProvider();
            var accounts = _avatarService.GetAll();
            return Ok(accounts);
        }

        [Authorize]
        [HttpGet("{id:int}")]
        //public ActionResult<AccountResponse> GetById(Guid id)
        public ActionResult<IAvatar> GetById(Guid id)
        {
           // GetAndActivateProvider();

            // users can get their own account and admins can get any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            var account = _avatarService.GetById(id);
            return Ok(account);
        }

        [Authorize(AvatarType.Wizard)]
        [HttpPost]
        //public ActionResult<AccountResponse> Create(CreateRequest model)
        public ActionResult<IAvatar> Create(CreateRequest model)
        {
            //GetAndActivateProvider();
            return Ok(_avatarService.Create(model));
        }

        [Authorize]
        [HttpPut("{id:Guid}")]
        //public ActionResult<AccountResponse> Update(Guid id, UpdateRequest model)
        //public ActionResult<IAvatar> Update(Guid id, UpdateRequest model)
        public ActionResult<Avatar> Update(Guid id, UpdateRequest model)
        {
            //GetAndActivateProvider();

            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

            // only admins can update role
            if (Avatar.AvatarType != AvatarType.Wizard)
                model.AvatarType = null;

            return Ok(_avatarService.Update(id, model));
        }

        [Authorize]
        [HttpDelete("{id:Guid}")]
        public IActionResult Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType != AvatarType.Wizard)
                return Unauthorized(new { message = "Unauthorized" });

           // GetAndActivateProvider();
            _avatarService.Delete(id);

            return Ok(new { message = "Account deleted successfully" });
        }




        //TODO: Come back to this...
        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    List<Avatar> avatars = AvatarManager.LoadAllAvatarsAsync().Result;

        //    foreach (Avatar avatar in avatars)
        //        avatar.Password = null;
        //    }
        //    return Ok(avatars);
        //}

        //// GET api/values
        //[HttpGet]
        //public ActionResult<IEnumerable<string>> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public ActionResult<string> Get(int id)
        //{
        //    return "value";
        //}


        // GET api/values/5
        //[HttpGet("GetAvatarById/{sequenceNo}/{phaseNo}")]
        [HttpGet("GetAvatarById/{id}")]
        //[HttpGet("{id}")]
        public async Task<IAvatar> Get(Guid id)
        {
           //  GetAndActivateProvider();
            return await AvatarManager.LoadAvatarAsync(id);

            //TODO: Blank out private fields especially password, etc.
            // Only leave other private fields if the id mateches the logged in avatar...
        }


        // GET api/values/5
        //[HttpGet("{id}")]
        [HttpGet("GetAvatarByIdForProvider/{id}/{providerType}")]
        public async Task<IAvatar> Get(Guid id, ProviderType providerType)
        {
            //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
            GetAndActivateProvider(providerType);
            return await AvatarManager.LoadAvatarAsync(id, providerType);

            //TODO: Blank out private fields especially password, etc.
            // Only leave other private fields if the id mateches the logged in avatar...
        }

        /*
        //[HttpGet("{id}")]
        [HttpGet("GetAvatarByProviderKey/{id}")]
        public async Task<IAvatar> Get(string providerKey)
        {
            return await Program.AvatarManager.LoadAvatarAsync(providerKey);
        }

        [HttpGet("GetAvatarByProviderKeyForProvider/{id}/{providerType}")]
        public async Task<IAvatar> Get(string providerKey, ProviderType providerType)
        {
            return await Program.AvatarManager.LoadAvatarAsync(providerKey, providerType);
        }
        */

        /*
        [HttpGet("GetAvatarByUsernameAndPassword/{username}/{password}")]
        public IAvatar Get(string username, string password)
        {
            //TODO: Get Async version working... 
            //return await Program.AvatarManager.LoadAvatarAsync(username, password);
            return Program.AvatarManager.LoadAvatar(username, password);
        }

        [HttpGet("GetAvatarByUsernameAndPasswordForProvider/{username}/{password}/{providerType}")]
        public async Task<IAvatar> Get(string username, string password, ProviderType providerType)
        {
            return await Program.AvatarManager.LoadAvatarAsync(username, password, providerType);
        }*/

        [HttpGet("GetAvatar/{username}/{password}")]
        //[HttpGet("GetAvatarByUsernameAndPassword/{username}/{password}")]
        //[HttpGet("/{username}/{password}")]
        public IAvatar Get(string username, string password)
        {
            //TODO: Get Async version working... 
            //return await Program.AvatarManager.LoadAvatarAsync(username, password);

            //  ActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), OASISSettings.Value.StorageProviders.DefaultProvider));

           // GetAndActivateProvider();
            return AvatarManager.LoadAvatar(username, password);
        }

        [HttpGet("GetAvatarForProvider/{providerType}/{username}/{password}")]
        //[HttpGet("GetAvatarByUsernameAndPasswordForProvider/{username}/{password}/{providerType}")]
        //[HttpGet("/{username}/{password}/{providerType}")]
        public async Task<IAvatar> Get(ProviderType providerType, string username, string password)
        {
            //TODO: Get Async version working... 
            //return await Program.AvatarManager.LoadAvatarAsync(username, password, providerType);

            GetAndActivateProvider(providerType);
            return AvatarManager.LoadAvatar(username, password, providerType);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] IAvatar value)
        {
            AvatarManager.SaveAvatarAsync(value);
        }

           // helper methods

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
