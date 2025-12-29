namespace API.Controllers.V1;

/// <summary>
/// Controller that handles identity-related operations, such as user registration, login, password management, 
/// and account recovery. All actions are routed under the "auth" base path for authentication and authorization tasks.
/// </summary>
[Route($"{ApiAddresses.Base}/auth")]
public sealed class IdentityController(IIdentityService identityService) : V1BaseController
{
    /// <summary>
    /// Registers a new user with the provided registration details.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating whether registration was successful or not.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
        => (await identityService.RegisterAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Logs in a user using the provided credentials.
    /// </summary>
    /// <param name="request">The login request containing user credentials.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the login attempt.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => (await identityService.LoginAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Logs out a currently authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the logout operation.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
        => (await identityService.LogoutAsync(cancellationToken)).ToActionResult();

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    /// <param name="request">The request containing the current and new password.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the password change.</returns>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
        => (await identityService.ChangePasswordAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Sends a confirmation code to the user's email for email verification.
    /// </summary>
    /// <param name="request">The request containing the user's email information.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of sending the email confirmation code.</returns>
    [HttpPost("email/confirm/code")]
    public async Task<IActionResult> SendEmailConfirmCodeAsync([FromBody] SendEmailConfirmationCodeRequest request,
        CancellationToken cancellationToken)
        => (await identityService.SendEmailConfirmationCodeAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Confirms the email address of the user using the provided confirmation code.
    /// </summary>
    /// <param name="request">The request containing the confirmation code.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the email confirmation process.</returns>
    [HttpPost("email/confirm")]
    public async Task<IActionResult> EmailConfirmAsync([FromBody] ConfirmEmailCodeRequest request,
        CancellationToken cancellationToken)
        => (await identityService.ConfirmEmailAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Initiates the password reset process for users who have forgotten their password.
    /// </summary>
    /// <param name="request">The request containing the user's email for password recovery.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the forgot password request.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
        => (await identityService.ForgotPasswordAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Resets the password for a user who has requested a password reset.
    /// </summary>
    /// <param name="request">The request containing the new password and reset token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the password reset.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
        => (await identityService.ResetPasswordAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Initiates the account restoration process for a deleted or inactive account.
    /// </summary>
    /// <param name="request">The request containing the necessary information to restore the account.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the account restoration process.</returns>
    [HttpPost("restore")]
    [AllowAnonymous]
    public async Task<IActionResult> RestoreAccountAsync([FromBody] RestoreAccountRequest request,
        CancellationToken cancellationToken)
        => (await identityService.RestoreAccountAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Confirms the restoration of a previously deleted account.
    /// </summary>
    /// <param name="request">The request containing the confirmation details for account restoration.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of confirming account restoration.</returns>
    [HttpPost("restore/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmRestoreAccountAsync([FromBody] ConfirmRestoreAccountRequest request,
        CancellationToken cancellationToken)
        => (await identityService.ConfirmRestoreAccountAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Deletes the authenticated user's account permanently.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the account deletion.</returns>
    [HttpDelete]
    public async Task<IActionResult> DeleteAccountAsync(CancellationToken cancellationToken)
        => (await identityService.DeleteAccountAsync(cancellationToken)).ToActionResult();
}