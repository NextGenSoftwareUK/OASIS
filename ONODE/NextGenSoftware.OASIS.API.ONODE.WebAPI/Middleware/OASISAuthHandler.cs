using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    // Bridges the custom JwtMiddleware (which populates context.Items["Avatar"]) into
    // ASP.NET Core's authentication/authorization pipeline so that [Authorize] attributes
    // work correctly.  JwtMiddleware must run before UseAuthentication in the pipeline.
    public class OASISAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public OASISAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var avatar = Context.Items["Avatar"] as IAvatar;
            if (avatar == null)
                return Task.FromResult(AuthenticateResult.Fail("No authenticated avatar"));

            var claims = new[] { new Claim("id", avatar.Id.ToString()) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
