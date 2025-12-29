namespace API.Controllers.Base;

/// <summary>
/// Base controller class for all API controllers.
/// This class is responsible for providing common functionality that can be shared across
/// multiple controllers, such as error handling and API conventions.
/// </summary>
[ApiController]
[ApiConventionType(typeof(CustomApiConventions))]
public class BaseController : ControllerBase
{
    /// <summary>
    /// Gets the error message from the ModelState if it is not valid.
    /// If the ModelState is valid, it returns null. If there are errors, it aggregates them
    /// into a single string, separated by semicolons.
    /// </summary>
    protected string? GetErrorMessage
    {
        get
        {
            // If the ModelState is valid, no error message is returned.
            // Otherwise, it returns all errors in the ModelState.
            return ModelState.IsValid
                ? null
                : string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
        }
    }
}