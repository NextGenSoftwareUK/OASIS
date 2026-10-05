using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Middleware;

/// <summary>
/// Middleware to authenticate merchant requests using JWT tokens or API keys
/// </summary>
public class MerchantAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MerchantAuthMiddleware> _logger;

    public MerchantAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<MerchantAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IShipexProRepository repository, MerchantAuthService authService)
    {
        // Skip authentication for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var merchantId = await AuthenticateRequestAsync(context, repository, authService);
            
            if (merchantId.HasValue)
            {
                // Add merchant context to request
                context.Items["MerchantId"] = merchantId.Value;
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new { error = "Unauthorized" });
                await context.Response.WriteAsync(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(new { error = "Internal server error" });
            await context.Response.WriteAsync(json);
        }
    }

    private async Task<Guid?> AuthenticateRequestAsync(
        HttpContext context,
        IShipexProRepository repository,
        MerchantAuthService authService)
    {
        // Try JWT token first
        var token = ExtractTokenFromHeader(context.Request);
        if (!string.IsNullOrEmpty(token))
        {
            var merchantId = ValidateJwtToken(token);
            if (merchantId.HasValue)
            {
                // Verify merchant exists and is active
                var merchantResult = await repository.GetMerchantAsync(merchantId.Value);
                if (!merchantResult.IsError && merchantResult.Result != null && merchantResult.Result.IsActive)
                {
                    return merchantId;
                }
            }
        }

        // Try API key
        var apiKey = ExtractApiKeyFromHeader(context.Request);
        if (!string.IsNullOrEmpty(apiKey))
        {
            var merchantResult = await authService.ValidateApiKeyAsync(apiKey);
            if (!merchantResult.IsError && merchantResult.Result != null && merchantResult.Result.IsActive)
            {
                return merchantResult.Result.MerchantId;
            }
        }

        return null;
    }

    private string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    private string? ExtractApiKeyFromHeader(HttpRequest request)
    {
        // Check X-API-Key header
        var apiKey = request.Headers["X-API-Key"].ToString();
        if (!string.IsNullOrEmpty(apiKey))
        {
            return apiKey;
        }

        // Check Authorization header with ApiKey scheme
        var authHeader = request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("ApiKey "))
        {
            return authHeader.Substring("ApiKey ".Length).Trim();
        }

        return null;
    }

    private Guid? ValidateJwtToken(string token)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"] ?? "YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32Characters";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "ShipexPro",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "ShipexPro",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var merchantIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(merchantIdClaim, out var merchantId))
            {
                return merchantId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
        }

        return null;
    }

    private bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/api/shipexpro/merchant/register",
            "/api/shipexpro/merchant/login"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class MerchantAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseMerchantAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MerchantAuthMiddleware>();
    }
}




