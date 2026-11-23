using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class JwtMiddleware
    {
        public const string ValidatedPrincipalKey = "OASIS.ValidatedPrincipal";
        public const string AuthenticationErrorItemKey = "OASIS.AuthenticationError";
        private readonly RequestDelegate _next;
        public JwtMiddleware(RequestDelegate next) { _next = next; }

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Request.Headers.Authorization;
            if (context.WebSockets.IsWebSocketRequest && context.Request.Path.StartsWithSegments("/ws/onode"))
            {
                string credential = null;
                foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
                {
                    if (!protocol.StartsWith("oasis-jwt.", StringComparison.Ordinal)) continue;
                    if (credential != null || headers.Count != 0)
                    { await Reject(context, 401, "INVALID_AUTHORIZATION_HEADER", "A single WebSocket bearer credential is required."); return; }
                    credential = protocol.Substring("oasis-jwt.".Length);
                }
                if (credential != null)
                    headers = new Microsoft.Extensions.Primitives.StringValues("Bearer " + credential);
            }
            if (headers.Count == 0) { await _next(context); return; }
            if (headers.Count != 1 || !AuthenticationHeaderValue.TryParse(headers[0], out var header) ||
                !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(header.Parameter))
            { await Reject(context, 401, "INVALID_AUTHORIZATION_HEADER", "A single Bearer authorization value is required."); return; }
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            context.Items.Remove(ValidatedPrincipalKey);
            context.Items.Remove(AuthenticationErrorItemKey);
            context.Items.Remove("Avatar");
            context.Items.Remove("AvatarId");
            OASISRequestContext.CurrentAvatar = null;
            OASISRequestContext.CurrentAvatarId = null;
            OASISRequestContext.AvatarRoutingOverrides = null;
            ClaimsPrincipal principal;
            try
            {
                var security = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Security;
                principal = OasisJwtValidation.Validate(header.Parameter, security?.SecretKey, security?.Oidc?.Issuer);
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException || ex is InvalidOperationException)
            {
                // Authentication policy belongs to the endpoint/filter. Continuing without an authenticated
                // principal lets the HyperDrive exchange validate its signed offline-session grant while
                // authorized bearer-only endpoints are still rejected by their authorization filter.
                context.Items[AuthenticationErrorItemKey] = ex.Message;
                await _next(context);
                return;
            }
            var id = Guid.Parse(principal.FindFirst("sub").Value);
            if (!AvatarSessionIdentity.TryReadBinding(principal, out var sessionId, out var authority))
            {
                context.Items[AuthenticationErrorItemKey] = "The access token has no valid session binding.";
                await _next(context);
                return;
            }
            RequestTimingMiddleware.Mark(context, RequestTimingMiddleware.Phase.AvatarLookup);
            var avatar = await Program.AvatarManager.LoadAvatarForAccessSessionAsync(id, sessionId, authority);
            if (avatar.IsError)
            {
                if (avatar.ErrorCode == "ACCESS_SESSION_AUTHORITY_UNAVAILABLE" || avatar.ErrorCode == "ACCESS_SESSION_MODE_UNSUPPORTED")
                    await Reject(context, 503, avatar.ErrorCode, avatar.Message);
                else
                {
                    // Bearer-only filters refuse the unauthenticated request. Signed offline-grant
                    // exchange retains its separately specified endpoint authentication policy.
                    context.Items[AuthenticationErrorItemKey] = avatar.Message;
                    await _next(context);
                }
                return;
            }
            context.User = principal;
            context.Items[ValidatedPrincipalKey] = principal;
            context.Items["Avatar"] = avatar.Result;
            OASISRequestContext.CurrentAvatarId = id;
            OASISRequestContext.CurrentAvatar = avatar.Result;
            // The avatar's saved HyperDrive settings override the DNA defaults for its own requests only.
            var routing = NextGenSoftware.OASIS.API.Core.Configuration.AvatarHyperDriveSettings.FromAvatar(avatar.Result);
            if (routing.IsError)
            { await Reject(context, 409, routing.ErrorCode, routing.Message); return; }
            OASISRequestContext.AvatarRoutingOverrides = routing.Result;
            RequestTimingMiddleware.Mark(context, RequestTimingMiddleware.Phase.Endpoint);
            await _next(context);
        }

        private static Task Reject(HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            return context.Response.WriteAsJsonAsync(new { IsError = true, Code = code, Message = message }, context.RequestAborted);
        }
    }
}
