namespace BuildingBlocks.Patterns.Result;

/// <summary>
/// Defines a set of error categories used to classify the type of failure in a result.
/// 
/// This enum is useful for assigning semantic meaning to errors in a consistent way 
/// across your application or API.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error. Represents a successful or neutral state.
    /// </summary>
    None,

    /// <summary>
    /// The request was invalid or malformed (HTTP 400).
    /// </summary>
    BadRequest,

    /// <summary>
    /// The requested resource was not found (HTTP 404).
    /// </summary>
    NotFound,

    /// <summary>
    /// The entity already exists and cannot be created again (custom 409 use case).
    /// </summary>
    AlreadyExist,

    /// <summary>
    /// There is a conflict with the current state of the resource (HTTP 409).
    /// </summary>
    Conflict,

    /// <summary>
    /// An unexpected internal server error occurred (HTTP 500).
    /// </summary>
    InternalServerError,
    UnsupportedMediaType,
    Forbidden
}