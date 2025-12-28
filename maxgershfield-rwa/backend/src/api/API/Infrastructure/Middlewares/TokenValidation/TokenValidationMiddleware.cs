namespace API.Infrastructure.Middlewares.TokenValidation;

/// <summary>
/// Middleware to validate tokens in incoming HTTP requests.
/// If the request is to an ignored URL, it bypasses token validation.
/// Otherwise, it validates the user's authentication status and token version.
/// </summary>
public sealed class TokenValidationMiddleware(
    RequestDelegate next,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TokenValidationMiddleware> logger)
{
    /// <summary>
    /// Invokes the token validation logic for each HTTP request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(InvokeAsync), date);
        try
        {
            string requestPath = context.Request.Path.ToString().ToLower().TrimEnd('/');


            if ((context.User.Identity is null || context.User.Claims.IsNullOrEmpty()) && context.User.Identity?.IsAuthenticated == true)
            {
                logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                await WriteErrorResponse(context, Messages.TokenValidationContextNull);
                return;
            }


            if (IgnoreUrl.IgnoreUrls.Contains(requestPath))
            {
                logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                await next(context);
                return;
            }

            if (context.User.Identity is { IsAuthenticated: true })
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                string? userId = context.User.FindFirst(x => x.Type == CustomClaimTypes.Id)?.Value;
                string? tokenVersionClaim = context.User.FindFirst(x => x.Type == CustomClaimTypes.TokenVersion)?.Value;

                if (userId is null || tokenVersionClaim is null)
                {
                    logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                    await WriteErrorResponse(context, Messages.TokenValidationInvalidTokenData);
                    return;
                }

                User? user = await dbContext.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user is null || user.TokenVersion.ToString() != tokenVersionClaim)
                {
                    logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                    await WriteErrorResponse(context, Messages.TokenValidationInvalidTokenVersion);
                    return;
                }
            }

            logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            await next(context);
        }
        catch (Exception e)
        {
            logger.OperationException(nameof(InvokeAsync), e.Message);
            logger.OperationCompleted(nameof(InvokeAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        }
    }

    /// <summary>
    /// Writes an error response when token validation fails.
    /// </summary>
    private async Task WriteErrorResponse(HttpContext context, string message)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(WriteErrorResponse), date);

        try
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = MediaTypeNames.Application.Json;

            object response = new { error = message };
            string jsonResponse = JsonConvert.SerializeObject(response);

            logger.LogWarning($"{context.Request.Path}: {message}");

            logger.OperationCompleted(nameof(WriteErrorResponse), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            await context.Response.WriteAsync(jsonResponse);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(WriteErrorResponse), ex.Message);
            logger.OperationCompleted(nameof(WriteErrorResponse), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
        }
    }
}