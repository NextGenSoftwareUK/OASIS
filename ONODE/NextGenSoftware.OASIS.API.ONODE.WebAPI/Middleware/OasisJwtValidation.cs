using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public static class OasisJwtValidation
    {
        public static ClaimsPrincipal Validate(string token, string secret, string configuredIssuer)
        {
            if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("The OASIS JWT signing key is not configured.");
            string issuer = string.IsNullOrEmpty(configuredIssuer) ? "OASIS" : configuredIssuer;
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, new TokenValidationParameters {
                ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = issuer,
                ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero,
                AuthenticationType = "OASIS", NameClaimType = JwtRegisteredClaimNames.Sub
            }, out _);
            var subjects = principal.FindAll(JwtRegisteredClaimNames.Sub).ToArray();
            var ids = principal.FindAll("id").ToArray();
            if (subjects.Length != 1 || ids.Length != 1 || !Guid.TryParseExact(subjects[0].Value, "D", out var subject) ||
                !Guid.TryParseExact(ids[0].Value, "D", out var id) || id != subject)
                throw new SecurityTokenValidationException("JWT avatar claims are missing or inconsistent.");
            var aliases = principal.FindAll(ClaimTypes.NameIdentifier).ToArray();
            if (aliases.Length > 1 || (aliases.Length == 1 && (!Guid.TryParseExact(aliases[0].Value, "D", out var alias) || alias != subject)))
                throw new SecurityTokenValidationException("JWT subject aliases disagree.");
            return principal;
        }
    }
}
