//using System;
//using System.Net.Http.Headers;
//using System.Security.Claims;
//using System.Text;
//using System.Text.Encodings.Web;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using NextGenSoftware.OASIS.API.Core;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
//{
//    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
//    {
//        //private static AvatarManager _avatarManager;

//        //public AvatarManager AvatarManager
//        //{
//        //    get
//        //    {
//        //        if (_avatarManager == null)
//        //        {
//        //            _avatarManager = new AvatarManager(GetAndActivateProvider());
//        //            _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
//        //        }

//        //        return _avatarManager;
//        //    }
//        //}

//        public BasicAuthenticationHandler(
//            IOptionsMonitor<AuthenticationSchemeOptions> options,
//            ILoggerFactory logger,
//            UrlEncoder encoder,
//            ISystemClock clock)
//            //IAvatarService avatarService) //TODO: See if this param can be taken out? I can't see where this BasicAuthenticationHandler is instanitated?
//            : base(options, logger, encoder, clock)
//        {
//           // _avatarService = avatarService;
//        }

//        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
//        {
//            if (!Request.Headers.ContainsKey("Authorization"))
//                return AuthenticateResult.Fail("Missing Authorization Header");

//            IAvatar avatar = null;

//            try
//            {
//                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
//                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
//                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
//                var username = credentials[0];
//                var password = credentials[1];

//                //avatar = await _userService.Authenticate(username, password);
//                //avatar = await Program.AvatarManager.Authenticate(username, password);
                
//                //TODO: Get Async working ASAP!
//                //avatar = await Program.AvatarManager.LoadAvatarAsync(username, password);
//                avatar = Program.AvatarManager.LoadAvatar(username, password);
//            }
//            catch
//            {
//                return AuthenticateResult.Fail("Invalid Authorization Header");
//            }

//            if (avatar == null)
//                return AuthenticateResult.Fail("Invalid Username or Password");

//            var claims = new[] 
//            {
//                new Claim(ClaimTypes.NameIdentifier, avatar.Id.ToString()),
//                new Claim(ClaimTypes.Name, avatar.Username),
//            };

//            var identity = new ClaimsIdentity(claims, Scheme.Name);
//            var principal = new ClaimsPrincipal(identity);
//            var ticket = new AuthenticationTicket(principal, Scheme.Name);

//            return AuthenticateResult.Success(ticket);
//        }
//    }
//}