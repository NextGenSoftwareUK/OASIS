namespace Application.Contracts;

/// <summary>
/// Provides comprehensive identity management capabilities, including user registration, authentication,
/// password management, email confirmation, and account restoration/deletion.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Registers a new user in the system.
    /// Validates the registration data, ensuring that the provided username and email are unique,
    /// and returns a response containing the new user's identifier upon success.
    /// </summary>
    /// <param name="request">The registration request containing user credentials and other required information.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{RegisterResponse}"/> indicating success with a registration response if the operation succeeds;
    /// otherwise, an appropriate error result.
    /// </returns>
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken token = default);

    /// <summary>
    /// Authenticates the user with the provided login credentials.
    /// Issues access tokens and updates login-related metadata upon successful authentication.
    /// </summary>
    /// <param name="request">The login request containing the user's email and password.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Result{LoginResponse}"/> containing token and expiration details if authentication succeeds;
    /// otherwise, an error result.
    /// </returns>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken token = default);

    /// <summary>
    /// Logs out the current user by invalidating the session or token.
    /// </summary>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating whether the logout operation was successful.
    /// </returns>
    Task<BaseResult> LogoutAsync(CancellationToken token = default);

    /// <summary>
    /// Changes the current user's password after verifying the current password.
    /// Updates the password hash and relevant security metadata.
    /// </summary>
    /// <param name="request">The change password request containing the current and new passwords.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating success if the password was changed; otherwise, an error result.
    /// </returns>
    Task<BaseResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken token = default);

    /// <summary>
    /// Sends an email with a confirmation code to the user, used to verify the email address.
    /// </summary>
    /// <param name="request">The request that includes the email address to which the confirmation code should be sent.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating success if the email was sent; otherwise, an error result.
    /// </returns>
    Task<BaseResult> SendEmailConfirmationCodeAsync(SendEmailConfirmationCodeRequest request,
        CancellationToken token = default);

    /// <summary>
    /// Confirms the user's email address using the provided confirmation code.
    /// Marks the email as confirmed upon successful verification.
    /// </summary>
    /// <param name="request">The confirmation request that contains the email address and verification code.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating whether email confirmation was successful.
    /// </returns>
    Task<BaseResult> ConfirmEmailAsync(ConfirmEmailCodeRequest request, CancellationToken token = default);

    /// <summary>
    /// Initiates the forgot password process by sending password recovery instructions to the user’s email.
    /// </summary>
    /// <param name="request">The request containing the user's email address for password recovery.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating success if the email was dispatched; otherwise, an error result.
    /// </returns>
    Task<BaseResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken token = default);

    /// <summary>
    /// Resets the user's password using a valid password reset verification code.
    /// Updates the password hash and associated security metadata.
    /// </summary>
    /// <param name="request">The request that includes the user's email and the new password.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating whether the password reset operation was successful.
    /// </returns>
    Task<BaseResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken token = default);

    /// <summary>
    /// Initiates the account restoration process for a soft-deleted user account.
    /// Generates a verification code and sends it to the user's email to confirm account restoration.
    /// </summary>
    /// <param name="request">The restore account request containing the user's email address.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating success if the restoration initiation was successful; otherwise, an error result.
    /// </returns>
    Task<BaseResult> RestoreAccountAsync(RestoreAccountRequest request, CancellationToken token = default);

    /// <summary>
    /// Confirms the restoration of a soft-deleted user account using the provided verification code.
    /// If the verification is successful and within the allowed restoration period,
    /// the account is reactivated.
    /// </summary>
    /// <param name="request">The confirmation request containing the user's email and the restore code.</param>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating whether the account restoration confirmation was successful.
    /// </returns>
    Task<BaseResult> ConfirmRestoreAccountAsync(ConfirmRestoreAccountRequest request,
        CancellationToken token = default);

    /// <summary>
    /// Soft-deletes the current user's account.
    /// Marks the account as deleted and sends an email notification confirming the deletion.
    /// </summary>
    /// <param name="token">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="BaseResult"/> indicating whether the account deletion operation was successful.
    /// </returns>
    Task<BaseResult> DeleteAccountAsync(CancellationToken token = default);
}