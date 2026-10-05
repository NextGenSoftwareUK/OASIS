namespace BuildingBlocks.Patterns.ApiResponse;

/// <summary>
/// Represents a standard API response structure used for returning both success and failure responses.
/// This class contains a flag indicating whether the response was successful, an error pattern, and the data if available.
/// </summary>
/// <typeparam name="T">The type of the data being returned in the response.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the API response was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Contains information about any errors that occurred in the response.
    /// </summary>
    public ResultPatternError Error { get; init; }

    /// <summary>
    /// The data being returned in the response. May be null in case of failure.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Private constructor to initialize the ApiResponse with success, error, and data information.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the response was successful.</param>
    /// <param name="error">The error information if the response is not successful.</param>
    /// <param name="data">The data to return if the response is successful.</param>
    private ApiResponse(bool isSuccess, ResultPatternError error, T? data)
    {
        IsSuccess = isSuccess;
        Error = error;
        Data = data;
    }

    /// <summary>
    /// Creates a successful API response with the provided data.
    /// </summary>
    /// <param name="data">The data to be included in the success response.</param>
    /// <returns>An ApiResponse indicating success with the provided data.</returns>
    public static ApiResponse<T> Success(T? data) => new(true, ResultPatternError.None(), data);

    /// <summary>
    /// Creates a failed API response with the provided error.
    /// </summary>
    /// <param name="error">The error to be included in the failure response.</param>
    /// <returns>An ApiResponse indicating failure with the provided error and no data.</returns>
    public static ApiResponse<T> Fail(ResultPatternError error) => new(false, error, default);

    /// <summary>
    /// Creates a failed API response with the provided error and optional data.
    /// </summary>
    /// <param name="error">The error to be included in the failure response.</param>
    /// <param name="value">The optional data to be included in the failure response.</param>
    /// <returns>An ApiResponse indicating failure with the provided error and data.</returns>
    public static ApiResponse<T> Fail(ResultPatternError error, T? value) => new(false, error, value);
}