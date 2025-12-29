namespace API.Infrastructure.Extensions.ResultPattern;

/// <summary>
/// Extension methods for converting the result of an operation to an appropriate HTTP action result.
/// These methods map the outcome of a `Result` or `BaseResult` to an `IActionResult`, 
/// allowing for uniform API responses that align with standard HTTP status codes.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a `Result` object to an appropriate `IActionResult` based on the result's success status and error type.
    /// This method returns different HTTP status codes depending on whether the operation was successful or encountered an error.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result object to convert.</param>
    /// <returns>An `IActionResult` that represents the result of the operation.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        ApiResponse<T> apiResponse = result.IsSuccess
            ? ApiResponse<T>.Success(result.Value)
            : ApiResponse<T>.Fail(result.Error, result.Value);

        return result.Error.ErrorType switch
        {
            ErrorType.Conflict => new ConflictObjectResult(apiResponse),
            ErrorType.AlreadyExist => new ConflictObjectResult(apiResponse),
            ErrorType.NotFound => new NotFoundObjectResult(apiResponse),
            ErrorType.BadRequest => new BadRequestObjectResult(apiResponse),
            ErrorType.UnsupportedMediaType => new UnsupportedMediaTypeResult(),
            ErrorType.Forbidden => new ObjectResult(apiResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
            ErrorType.None => new OkObjectResult(apiResponse),
            _ => new ObjectResult(apiResponse) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    /// <summary>
    /// Converts a `BaseResult` object to an appropriate `IActionResult` based on the result's success status and error type.
    /// This method handles cases where the result does not contain a value but still needs to return an HTTP response.
    /// </summary>
    /// <param name="result">The base result object to convert.</param>
    /// <returns>An `IActionResult` that represents the result of the operation.</returns>
    public static IActionResult ToActionResult(this BaseResult result)
    {
        ApiResponse<BaseResult> apiResponse = result.IsSuccess
            ? ApiResponse<BaseResult>.Success(null)
            : ApiResponse<BaseResult>.Fail(result.Error);

        return result.Error.ErrorType switch
        {
            ErrorType.Conflict => new ConflictObjectResult(apiResponse),
            ErrorType.AlreadyExist => new ConflictObjectResult(apiResponse),
            ErrorType.NotFound => new NotFoundObjectResult(apiResponse),
            ErrorType.BadRequest => new BadRequestObjectResult(apiResponse),
            ErrorType.UnsupportedMediaType => new UnsupportedMediaTypeResult(),
            ErrorType.Forbidden => new ObjectResult(apiResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
            ErrorType.None => new OkObjectResult(apiResponse),
            _ => new ObjectResult(apiResponse) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}