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
            if (headers.Count == 0) { await _next(context); return; }
            if (headers.Count != 1 || !AuthenticationHeaderValue.TryParse(headers[0], out var header) ||
                !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(header.Parameter))
            { await Reject(context, 401, "INVALID_AUTHORIZATION_HEADER", "A single Bearer authorization value is required."); return; }
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
            var avatar = await Program.AvatarManager.LoadAvatarAsync(id);
            if (avatar.IsError)
            { await Reject(context, 503, "AVATAR_LOOKUP_UNAVAILABLE", "The authenticated avatar could not be loaded."); return; }
            if (avatar.Result == null || avatar.Result.Id != id)
            { await Reject(context, 401, "INVALID_AVATAR", "The authenticated avatar no longer exists."); return; }
            context.User = principal;
            context.Items[ValidatedPrincipalKey] = principal;
            context.Items["Avatar"] = avatar.Result;
            OASISRequestContext.CurrentAvatarId = id;
            OASISRequestContext.CurrentAvatar = avatar.Result;
            await _next(context);
        }

        private static Task Reject(HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            return context.Response.WriteAsJsonAsync(new { IsError = true, Code = code, Message = message }, context.RequestAborted);
        }
    }
}
