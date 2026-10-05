namespace ScGen.API.Infrastructure.Controllers.Base;

/// <summary>
/// This controller handles unhandled exceptions for the API and returns a corresponding error response.
/// </summary>
[Route("/error")]
[ApiExplorerSettings(IgnoreApi = true)] 
public class ErrorController(ILogger<ErrorController> logger) : BaseController
{
    /// <summary>
    /// Handles different types of errors and returns appropriate responses based on the exception type.
    /// Logs the exception details for debugging purposes.
    /// </summary>
    /// <returns>An IActionResult that represents the error response.</returns>
    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch, HttpOptions, HttpHead]
    public IActionResult HandleError()
    {
        // Retrieves the exception that was caught by the exception handler middleware.
        Exception? exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

        // Logs the exception details if an exception exists.
        if (exception != null)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        // Returns a problem response based on the type of exception.
        return exception switch
        {
            // Handles validation exceptions (e.g., model validation errors)
            ValidationException validationEx => Problem(
                title: "Validation exception!",
                detail: validationEx.Message,
                statusCode: StatusCodes.Status400BadRequest,
                instance: HttpContext.Request.Path,
                type: exception.HelpLink
            ),

            // Handles unauthorized access exceptions
            UnauthorizedAccessException => Problem(
                title: "Unauthorized access!",
                detail: "You are not authorized to access this resource.",
                statusCode: StatusCodes.Status401Unauthorized,
                instance: HttpContext.Request.Path,
                type: "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/401"
            ),

            // Handles operation canceled exceptions (e.g., request canceled by client)
            OperationCanceledException => Problem(
                title: "Request canceled",
                detail: "The request was canceled by the client.",
                statusCode: StatusCodes.Status499ClientClosedRequest,
                instance: HttpContext.Request.Path,
                type: "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/499"
            ),

            // Default case for unexpected errors
            _ => Problem(
                title: "An unexpected error occurred",
                detail: "Something went wrong. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError,
                instance: HttpContext.Request.Path,
                type: "https://learn.microsoft.com/"
            )
        };
    }
}