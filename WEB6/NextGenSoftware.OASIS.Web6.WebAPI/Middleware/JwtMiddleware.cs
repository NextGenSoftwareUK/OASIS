using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Middleware
{
    /// <summary>
    /// Validates the OASIS JWT from the Authorization: Bearer header and attaches the avatar
    /// to the request context. On failure it skips silently — AuthorizeAttribute decides whether
    /// to reject based on all available auth mechanisms (JWT or API key).
    ///
    /// HISTORY / WHY THINGS ARE THE WAY THEY ARE (2026-07-05):
    ///
    /// 1. MIDDLEWARE WAS MISSING FROM PIPELINE
    ///    The original Web6 Program.cs never called app.UseMiddleware&lt;JwtMiddleware&gt;(), so the
    ///    JWT was silently ignored on every request and AuthorizeAttribute always returned 401.
    ///    Fix: added app.UseMiddleware&lt;JwtMiddleware&gt;() to Program.cs.
    ///
    /// 2. NUGET VERSION CONFLICT (GetValidLkgConfigurations MissingMethodException)
    ///    Web6 WebAPI inherited System.IdentityModel.Tokens.Jwt 6.35.0 from OASIS API Core.
    ///    A transitive dependency pulled in a newer Microsoft.IdentityModel.Tokens that added
    ///    GetValidLkgConfigurations() — a method 6.35.0 doesn't call, causing a MissingMethodException.
    ///    Fix: pinned System.IdentityModel.Tokens.Jwt to 8.19.1 in the .csproj (same as ONODE/Web4).
    ///
    /// 3. ISSUER / AUDIENCE VALIDATION DISABLED
    ///    The Web4 OASIS API issues JWTs without iss or aud claims. The older 6.35.0 library was
    ///    lenient about this; 8.19.1 is strict and throws IDX10206/IDX10211 when those claims are
    ///    absent but validation is enabled. Since all OASIS APIs share one secret key there is no
    ///    multi-issuer scenario that these checks would protect against.
    ///    Fix: ValidateIssuer = false, ValidateAudience = false.
    ///    Security note: ValidateIssuerSigningKey = true is still enforced — the signature is always
    ///    verified against the shared OASIS SecretKey, so forged tokens are rejected.
    ///
    /// 4. AVATAR DB LOAD IS BEST-EFFORT
    ///    Web6 is an AI routing layer. When deployed without a storage provider (or when the DB is
    ///    temporarily unavailable), AvatarManager.Instance.LoadAvatarAsync may fail. The avatar ID
    ///    extracted from the JWT claims is set on the context regardless, so AuthorizeAttribute can
    ///    confirm the request is authenticated without a full DB round-trip.
    ///
    /// 5. SILENT CATCH ON JWT FAILURE
    ///    The original middleware wrote a 401 directly to the response on any JWT exception, which
    ///    killed the request even when a valid API key (X-Web6-Api-Key) was also present. Now a bad
    ///    JWT is silently skipped so the API key path in AuthorizeAttribute still works.
    /// </summary>
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
                await AttachAvatarToContext(context, token);
            await _next(context);
        }

        private async Task AttachAvatarToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Security.SecretKey);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,  // cryptographic signature always verified
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,           // Web4 tokens have no iss claim (see note 3 above)
                    ValidateAudience = false,         // Web4 tokens have no aud claim (see note 3 above)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                // Set AvatarId from claims immediately — avatar DB load below is best-effort only (see note 4).
                context.Items["AvatarId"] = id;
                OASISRequestContext.CurrentAvatarId = id;

                try
                {
                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(id, false, false);
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        context.Items["Avatar"] = avatarResult.Result;
                        OASISRequestContext.CurrentAvatar = avatarResult.Result;
                    }
                }
                catch
                {
                    // Avatar DB unavailable — JWT is still valid, AvatarId is set, request proceeds (see note 4).
                }
            }
            catch
            {
                // Invalid JWT — skip silently. AuthorizeAttribute will reject if no other auth succeeds (see note 5).
            }
        }
    }
}
