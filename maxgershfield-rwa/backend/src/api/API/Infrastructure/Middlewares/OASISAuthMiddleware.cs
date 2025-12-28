namespace API.Infrastructure.Middlewares;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BuildingBlocks.Extensions.Logger;

/// <summary>
/// Middleware to validate OASIS JWT tokens and extract user/avatar context
/// This middleware validates OASIS-issued JWT tokens and attaches avatar information to the request context
/// </summary>
public sealed class OASISAuthMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<OASISAuthMiddleware> logger)
{
    /// <summary>
    /// Invokes the OASIS authentication logic for each HTTP request
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(InvokeAsync), date);

        try
        {
            // Skip for public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                await next(context);
                return;
            }

            // Try to extract and validate OASIS JWT token
            string? token = ExtractTokenFromHeader(context.Request);
            
            if (!string.IsNullOrEmpty(token))
            {
                var tokenInfo = ValidateOASISToken(token);
                
                if (tokenInfo != null)
                {
                    // Attach OASIS user context to request
                    context.Items["OASISAvatarId"] = tokenInfo.AvatarId;
                    context.Items["OASISUsername"] = tokenInfo.Username;
                    context.Items["OASISEmail"] = tokenInfo.Email;
                    
                    // Create claims principal for the user
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, tokenInfo.AvatarId),
                        new("AvatarId", tokenInfo.AvatarId),
                        new(ClaimTypes.Name, tokenInfo.Username ?? ""),
                        new(ClaimTypes.Email, tokenInfo.Email ?? "")
                    };

                    if (!string.IsNullOrEmpty(tokenInfo.Username))
                    {
                        claims.Add(new Claim("Username", tokenInfo.Username));
                    }

                    var identity = new ClaimsIdentity(claims, "OASIS");
                    context.User = new ClaimsPrincipal(identity);
                }
            }

            logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            await next(context);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(InvokeAsync), ex.Message);
            logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            
            // On error, continue to next middleware (don't block request)
            await next(context);
        }
    }

    /// <summary>
    /// Extracts JWT token from Authorization header
    /// </summary>
    private string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    /// <summary>
    /// Validates OASIS JWT token and extracts user information
    /// Note: This decodes the token without full signature validation
    /// For production, you should validate the token signature using OASIS's public key
    /// </summary>
    private OASISTokenInfo? ValidateOASISToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Read token without validation (for now)
            // In production, validate signature using OASIS's JWT secret/key
            if (!tokenHandler.CanReadToken(token))
            {
                logger.LogWarning("Invalid token format");
                return null;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                logger.LogWarning("Token has expired");
                return null;
            }

            // Extract claims
            string? avatarId = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == "AvatarId" || 
                c.Type == "avatarId" || 
                c.Type == "sub" ||
                c.Type == ClaimTypes.NameIdentifier)?.Value;

            string? username = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == "Username" || 
                c.Type == "username" ||
                c.Type == ClaimTypes.Name)?.Value;

            string? email = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == "Email" || 
                c.Type == "email" ||
                c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(avatarId))
            {
                logger.LogWarning("AvatarId not found in token");
                return null;
            }

            return new OASISTokenInfo
            {
                AvatarId = avatarId,
                Username = username,
                Email = email
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate OASIS token");
            return null;
        }
    }

    /// <summary>
    /// Checks if the endpoint is a public endpoint that doesn't require authentication
    /// </summary>
    private bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/api/v1/auth/register",
            "/api/v1/auth/login"
        };

        string requestPath = path.ToString().ToLower().TrimEnd('/');
        return publicPaths.Any(p => requestPath.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Internal class to hold OASIS token information
    /// </summary>
    private class OASISTokenInfo
    {
        public string AvatarId { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Email { get; set; }
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class OASISAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseOASISAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OASISAuthMiddleware>();
    }
}

