using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// OAuth 2.0 / OpenID Connect provider endpoints — enables HerzID white-label SSO.
    /// All endpoints are disabled unless OASISDNA.OASIS.Security.Oidc.Enabled = true.
    /// </summary>
    [ApiController]
    public class OidcController : OASISControllerBase
    {
        private AvatarManager AvatarManager => Program.AvatarManager;

        private bool OidcEnabled =>
            OASISDNAManager.OASISDNA?.OASIS?.Security?.Oidc?.Enabled == true;

        private string ResolveIssuer()
        {
            var configured = OASISDNAManager.OASISDNA?.OASIS?.Security?.Oidc?.Issuer;
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.TrimEnd('/');
            var req = HttpContext.Request;
            return $"{req.Scheme}://{req.Host}";
        }

        // ── Discovery document ────────────────────────────────────────────────

        [HttpGet(".well-known/openid-configuration")]
        [AllowAnonymous]
        public IActionResult Discovery()
        {
            if (!OidcEnabled)
                return NotFound(new { error = "OIDC not enabled on this OASIS instance." });

            var issuer = ResolveIssuer();
            var doc = new
            {
                issuer,
                authorization_endpoint         = $"{issuer}/oauth/authorize",
                token_endpoint                 = $"{issuer}/oauth/token",
                userinfo_endpoint              = $"{issuer}/oauth/userinfo",
                jwks_uri                       = $"{issuer}/oauth/jwks",
                response_types_supported       = new[] { "code", "token" },
                subject_types_supported        = new[] { "public" },
                id_token_signing_alg_values_supported = new[] { "HS256" },
                scopes_supported               = new[] { "openid", "profile", "email", "herzid" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
                claims_supported               = new[] { "sub", "iss", "name", "email", "id", "did", "herzid", "herzid_clearance" },
                grant_types_supported          = new[] { "authorization_code", "refresh_token" },
            };

            return Ok(doc);
        }

        // ── JWKS (key set) ────────────────────────────────────────────────────

        [HttpGet("oauth/jwks")]
        [AllowAnonymous]
        public IActionResult Jwks()
        {
            if (!OidcEnabled)
                return NotFound(new { error = "OIDC not enabled on this OASIS instance." });

            var secret = OASISDNAManager.OASISDNA?.OASIS?.Security?.SecretKey ?? "";
            var keyBytes = Encoding.ASCII.GetBytes(secret);
            // HMAC-SHA256 symmetric key — represented as an oct JWK.
            using var hmac = new HMACSHA256(keyBytes);
            var k = Base64UrlEncoder.Encode(keyBytes);
            var jwks = new
            {
                keys = new[]
                {
                    new
                    {
                        kty = "oct",
                        use = "sig",
                        alg = "HS256",
                        k,
                        kid = "oasis-1"
                    }
                }
            };

            return Ok(jwks);
        }

        // ── UserInfo ──────────────────────────────────────────────────────────

        [HttpGet("oauth/userinfo")]
        [Authorize]
        public IActionResult UserInfo()
        {
            if (!OidcEnabled)
                return NotFound(new { error = "OIDC not enabled on this OASIS instance." });

            var avatar = Avatar;
            if (avatar == null)
                return Unauthorized(new { error = "invalid_token" });

            var claims = new Dictionary<string, object>
            {
                ["sub"]   = avatar.Id.ToString(),
                ["name"]  = avatar.FullName ?? "",
                ["email"] = avatar.Email ?? "",
                ["id"]    = avatar.Id.ToString(),
            };

            if (!string.IsNullOrEmpty(avatar.DID))
                claims["did"] = avatar.DID;

            if (!string.IsNullOrEmpty(avatar.HerzId))
            {
                claims["herzid"]           = avatar.HerzId;
                claims["herzid_clearance"] = avatar.HerzClearanceLevel;
                claims["herzid_country"]   = avatar.HerzCountryCode ?? "";
                claims["herzid_seq"]       = avatar.HerzSequentialNumber;
            }

            return Ok(claims);
        }

        // ── Authorize (redirect) ──────────────────────────────────────────────

        /// <summary>
        /// OAuth 2.0 authorization endpoint. Redirects unauthenticated users to the OASIS login
        /// page which POSTs back to this endpoint with credentials, then issues a code.
        /// For Phase 1, issues the code immediately when the caller is already authenticated via
        /// the OASIS bearer token so HERZ sub-apps can exchange tokens without a browser redirect.
        /// </summary>
        [HttpGet("oauth/authorize")]
        [AllowAnonymous]
        public IActionResult Authorize(
            [FromQuery] string response_type,
            [FromQuery] string client_id,
            [FromQuery] string redirect_uri,
            [FromQuery] string scope,
            [FromQuery] string state)
        {
            if (!OidcEnabled)
                return NotFound(new { error = "OIDC not enabled on this OASIS instance." });

            if (response_type != "code")
                return BadRequest(new { error = "unsupported_response_type" });

            var avatar = Avatar;
            if (avatar == null)
            {
                // Not authenticated — redirect to login page with return URL
                var returnUrl = Uri.EscapeDataString(Request.QueryString.Value ?? "");
                return Redirect($"/login?returnUrl=/oauth/authorize{returnUrl}");
            }

            // Avatar is authenticated — issue a short-lived authorization code
            // The code is a signed JWT carrying the avatar ID (expires in 60 s)
            var code = GenerateAuthCode(avatar.Id.ToString(), client_id, redirect_uri);
            var location = $"{redirect_uri}?code={Uri.EscapeDataString(code)}" +
                           (string.IsNullOrEmpty(state) ? "" : $"&state={Uri.EscapeDataString(state)}");
            return Redirect(location);
        }

        // ── Token endpoint ────────────────────────────────────────────────────

        /// <summary>
        /// OAuth 2.0 token endpoint.  Accepts authorization_code and refresh_token grant types.
        /// Returns a JWT access token plus (for code grants) a refresh token.
        /// </summary>
        [HttpPost("oauth/token")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Token([FromForm] OidcTokenRequest form)
        {
            if (!OidcEnabled)
                return NotFound(new { error = "OIDC not enabled on this OASIS instance." });

            var security = OASISDNAManager.OASISDNA?.OASIS?.Security;
            var key      = Encoding.ASCII.GetBytes(security?.SecretKey ?? "");
            var issuer   = ResolveIssuer();

            if (form.grant_type == "authorization_code")
            {
                Guid avatarId;
                try
                {
                    avatarId = ValidateAuthCode(form.code, form.client_id, form.redirect_uri);
                }
                catch
                {
                    return BadRequest(new { error = "invalid_grant" });
                }

                var loadResult = await AvatarManager.LoadAvatarAsync(avatarId);
                if (loadResult.IsError || loadResult.Result == null)
                    return BadRequest(new { error = "invalid_grant", error_description = "Avatar not found." });

                var avatar = loadResult.Result;
                var lifetimeSecs = security?.Oidc?.AccessTokenLifetimeSeconds > 0
                    ? security.Oidc.AccessTokenLifetimeSeconds
                    : (security?.JwtTokenExpirationMinutes ?? 15) * 60;

                var accessToken = BuildAccessToken(avatar.Id.ToString(), avatar.Email, avatar.FullName,
                    avatar.DID, avatar.HerzId, avatar.HerzClearanceLevel, issuer, key, lifetimeSecs);

                return Ok(new
                {
                    access_token  = accessToken,
                    token_type    = "Bearer",
                    expires_in    = lifetimeSecs,
                    refresh_token = avatar.RefreshToken ?? "",
                    scope         = form.scope ?? "openid profile email herzid",
                });
            }

            if (form.grant_type == "refresh_token")
            {
                if (string.IsNullOrEmpty(form.refresh_token))
                    return BadRequest(new { error = "invalid_request", error_description = "refresh_token required." });

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var refreshResult = AvatarManager.RefreshToken(form.refresh_token, ip);
                if (refreshResult.IsError || refreshResult.Result == null)
                    return BadRequest(new { error = "invalid_grant" });

                var avatar = refreshResult.Result;
                var lifetimeSecs = security?.Oidc?.AccessTokenLifetimeSeconds > 0
                    ? security.Oidc.AccessTokenLifetimeSeconds
                    : (security?.JwtTokenExpirationMinutes ?? 15) * 60;

                var accessToken = BuildAccessToken(avatar.Id.ToString(), avatar.Email, avatar.FullName,
                    avatar.DID, avatar.HerzId, avatar.HerzClearanceLevel, issuer, key, lifetimeSecs);

                return Ok(new
                {
                    access_token  = accessToken,
                    token_type    = "Bearer",
                    expires_in    = lifetimeSecs,
                    refresh_token = avatar.RefreshToken ?? form.refresh_token,
                    scope         = form.scope ?? "openid profile email herzid",
                });
            }

            return BadRequest(new { error = "unsupported_grant_type" });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GenerateAuthCode(string avatarId, string clientId, string redirectUri)
        {
            var key = Encoding.ASCII.GetBytes(OASISDNAManager.OASISDNA?.OASIS?.Security?.SecretKey ?? "");
            var claims = new[]
            {
                new Claim("sub",          avatarId),
                new Claim("client_id",    clientId   ?? ""),
                new Claim("redirect_uri", redirectUri ?? ""),
                new Claim("nonce",        Guid.NewGuid().ToString()),
            };
            var descriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Expires            = DateTime.UtcNow.AddSeconds(60),
                Issuer             = ResolveIssuer(),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private Guid ValidateAuthCode(string code, string clientId, string redirectUri)
        {
            var key     = Encoding.ASCII.GetBytes(OASISDNAManager.OASISDNA?.OASIS?.Security?.SecretKey ?? "");
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(code, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(key),
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ClockSkew                = TimeSpan.Zero
            }, out _);

            var sub      = principal.FindFirstValue("sub");
            var cid      = principal.FindFirstValue("client_id");
            var ruri     = principal.FindFirstValue("redirect_uri");

            if (cid != (clientId ?? "") || ruri != (redirectUri ?? ""))
                throw new SecurityTokenException("client_id or redirect_uri mismatch.");

            return Guid.Parse(sub);
        }

        private static string BuildAccessToken(string avatarId, string email, string name,
            string did, string herzId, int herzClearance,
            string issuer, byte[] key, int lifetimeSecs)
        {
            var claims = new List<Claim>
            {
                new Claim("id",                      avatarId),
                new Claim(JwtRegisteredClaimNames.Sub,   avatarId),
                new Claim(JwtRegisteredClaimNames.Email, email   ?? ""),
                new Claim(JwtRegisteredClaimNames.Name,  name    ?? ""),
            };

            if (!string.IsNullOrEmpty(did))
                claims.Add(new Claim("did", did));

            if (!string.IsNullOrEmpty(herzId))
            {
                claims.Add(new Claim("herzid",           herzId));
                claims.Add(new Claim("herzid_clearance", herzClearance.ToString()));
            }

            var descriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Expires            = DateTime.UtcNow.AddSeconds(lifetimeSecs),
                Issuer             = issuer,
                Audience           = issuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }

}
