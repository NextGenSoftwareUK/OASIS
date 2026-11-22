using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.Web7.WebAPI.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        public JwtMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Request.Headers.Authorization;
            if (headers.Count == 0) { await _next(context); return; }
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            context.Items.Remove("Avatar");
            context.Items.Remove("AvatarId");
            OASISRequestContext.CurrentAvatar = null;
            OASISRequestContext.CurrentAvatarId = null;
            OASISRequestContext.AvatarRoutingOverrides = null;
            if (headers.Count != 1 || !AuthenticationHeaderValue.TryParse(headers[0], out var header) ||
                !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(header.Parameter))
            { await Reject(context, 401, "INVALID_AUTHORIZATION_HEADER", "A single Bearer authorization value is required."); return; }

            var security = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Security;
            if (string.IsNullOrWhiteSpace(security?.SecretKey))
            { await Reject(context, 503, "ACCESS_VALIDATION_UNAVAILABLE", "Access-token validation is not configured."); return; }
            ClaimsPrincipal principal;
            try
            {
                var issuer = string.IsNullOrEmpty(security.Oidc?.Issuer) ? "OASIS" : security.Oidc.Issuer;
                principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(header.Parameter,
                    CreateValidationParameters(Encoding.ASCII.GetBytes(security.SecretKey), issuer), out _);
            }
            catch (Exception error) when (error is SecurityTokenException || error is ArgumentException || error is InvalidOperationException)
            { await Reject(context, 401, "INVALID_ACCESS_TOKEN", "The access token is invalid or expired."); return; }
            if (!AvatarSessionIdentity.TryReadSubject(principal, out var id) ||
                !AvatarSessionIdentity.TryReadBinding(principal, out var sessionId, out var authority))
            { await Reject(context, 401, "INVALID_ACCESS_SESSION", "The access token has no valid session binding."); return; }

            var loaded = await AvatarManager.Instance.LoadAvatarForAccessSessionAsync(id, sessionId, authority);
            if (loaded.IsError)
            {
                var unavailable = loaded.ErrorCode == "ACCESS_SESSION_AUTHORITY_UNAVAILABLE" || loaded.ErrorCode == "ACCESS_SESSION_MODE_UNSUPPORTED";
                await Reject(context, unavailable ? 503 : 401, loaded.ErrorCode, loaded.Message);
                return;
            }
            context.User = principal;
            context.Items["AvatarId"] = id;
            context.Items["Avatar"] = loaded.Result;
            OASISRequestContext.CurrentAvatarId = id;
            OASISRequestContext.CurrentAvatar = loaded.Result;
            await _next(context);
        }

        private static TokenValidationParameters CreateValidationParameters(byte[] key, string issuer) => new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = issuer,
            ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero,
            AuthenticationType = "Bearer", NameClaimType = JwtRegisteredClaimNames.Sub
        };

        private static Task Reject(HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            return context.Response.WriteAsJsonAsync(new { IsError = true, Code = code, Message = message }, context.RequestAborted);
        }
    }
}
