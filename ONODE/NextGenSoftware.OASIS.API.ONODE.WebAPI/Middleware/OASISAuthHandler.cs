using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    /// <summary>Bridges the already validated JWT principal without discarding subject or privilege claims.</summary>
    public class OASISAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public OASISAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = Context.Items[JwtMiddleware.ValidatedPrincipalKey] as ClaimsPrincipal;
            var avatar = Context.Items["Avatar"] as IAvatar;
            if (principal?.Identity?.IsAuthenticated != true || avatar == null)
                return Task.FromResult(AuthenticateResult.NoResult());
            if (principal.FindFirst("sub")?.Value != avatar.Id.ToString("D"))
                return Task.FromResult(AuthenticateResult.Fail("Validated JWT and avatar identity disagree."));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
