namespace BuildingBlocks.Patterns.Result;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// Inherits from <see cref="BaseResult"/> and includes a typed value.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public sealed class Result<T> : BaseResult
{
    /// <summary>
    /// Gets the value returned by the operation, if successful. May be <c>null</c> if not applicable.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="error">The error information associated with a failed result.</param>
    /// <param name="value">The value returned by the operation, if any.</param>
    private Result(bool isSuccess, ResultPatternError error, T? value)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with an optional value.
    /// </summary>
    /// <param name="value">The value to return. If omitted, defaults to <c>default(T)</c>.</param>
    /// <returns>A successful <see cref="Result{T}"/> instance.</returns>
    public static Result<T> Success(T? value = default)
        => new(true, ResultPatternError.None(), value);

    /// <summary>
    /// Creates a failed result with the specified error and an optional value.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <param name="value">The value to return, even in failure. Defaults to <c>default(T)</c>.</param>
    /// <returns>A failed <see cref="Result{T}"/> instance.</returns>
    public static Result<T> Failure(ResultPatternError error, T value = default!)
        => new(false, error, value);
}