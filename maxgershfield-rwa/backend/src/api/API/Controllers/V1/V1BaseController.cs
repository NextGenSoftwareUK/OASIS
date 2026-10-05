namespace API.Controllers.V1;

/// <summary>
/// Base controller for versioned API (V1).
/// This class provides common functionality and security to all controllers in version 1 of the API.
/// It is decorated with the [Authorize] attribute to ensure that all actions require authentication.
/// </summary>
[Authorize]
public class V1BaseController : BaseController;