namespace BuildingBlocks.Extensions.ResultPattern;

/// <summary>
/// Represents the base result of an operation, encapsulating the success status and error details.
/// 
/// This class is useful for returning a standardized response from methods, indicating
/// whether the operation succeeded or failed, along with an optional error payload.
/// </summary>
public class BaseResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Contains error information if the operation failed; otherwise, it is <see cref="ResultPatternError.None"/>.
    /// </summary>
    public ResultPatternError Error { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseResult"/> class.
    /// Use <see cref="Success"/> or <see cref="Failure"/> factory methods to create instances.
    /// </summary>
    /// <param name="isSuccess">Indicates the success state of the operation.</param>
    /// <param name="error">The associated error information if any.</param>
    protected BaseResult(bool isSuccess, ResultPatternError error)
    {
        Error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Creates a successful result with an optional message.
    /// </summary>
    /// <param name="message">Optional message to describe the success result. Default is "Ok".</param>
    /// <returns>A <see cref="BaseResult"/> instance representing success.</returns>
    public static BaseResult Success(string message = "Ok")
        => new(true, ResultPatternError.None(message));

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error details describing why the operation failed.</param>
    /// <returns>A <see cref="BaseResult"/> instance representing failure.</returns>
    public static BaseResult Failure(ResultPatternError error)
        => new(false, error);
}