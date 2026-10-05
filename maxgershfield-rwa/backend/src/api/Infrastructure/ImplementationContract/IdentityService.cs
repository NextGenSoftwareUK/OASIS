using NetworkType = RadixBridge.Enums.NetworkType;
using Role = Domain.Entities.Role;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Provides identity-related services such as user registration, login, logout, password changes,
/// email confirmation, forgot and reset password,restore account, password reset and delete account.
/// Implements the IIdentityService interface.
/// </summary>
public sealed class IdentityService(
    ILogger<IdentityService> logger,
    DataContext dbContext,
    IHttpContextAccessor accessor,
    IEmailService emailService,
    IConfiguration configuration,
    ISolanaBridge solanaBridge, //use for all bridge factory pattern
    IRadixBridge radixBridge
) : IIdentityService
{
    /// <summary>
    /// Registers a new user.
    /// Validates the uniqueness of the username and email, assigns a default user role,
    /// and generates a verification code for email confirmation.
    /// </summary>
    /// <param name="request">The registration request containing user credentials and information.</param>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>Result</c> containing a <c>RegisterResponse</c> with the new user ID if successful;
    /// otherwise, a failure result with detailed error information.
    /// </returns>
    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(RegisterAsync), start);

        if (await dbContext.Users.IgnoreQueryFilters()
                .AnyAsync(x => x.UserName == request.UserName || x.Email == request.EmailAddress, token))
            return Result<RegisterResponse>.Failure(ResultPatternError.AlreadyExist(Messages.UserAlreadyExist));

        Role? role = await dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Name == Roles.User, token);
        if (role is null)
            return Result<RegisterResponse>.Failure(ResultPatternError.NotFound(Messages.RoleNotFound));

        User user = request.ToEntity(accessor);
        UserRole userRole = CreateUserRole(user.Id, role.Id);

        UserVerificationCode verificationCode = CreateVerificationCode(user.Id, user.CreatedByIp);

        try
        {
            await dbContext.Users.AddAsync(user, token);
            await dbContext.UserRoles.AddAsync(userRole, token);
            await dbContext.UserVerificationCodes.AddAsync(verificationCode, token);

            BaseResult radixRes = await CreateRadixAccountAsync(user.Id, token);
            if (!radixRes.IsSuccess) return Result<RegisterResponse>.Failure(radixRes.Error);

            BaseResult solanaRes = await CreateSolanaAccountAsync(user.Id, token);
            if (!solanaRes.IsSuccess) return Result<RegisterResponse>.Failure(solanaRes.Error);

            logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - start);

            return await dbContext.SaveChangesAsync(token) != 0
                ? Result<RegisterResponse>.Success(new(user.Id))
                : Result<RegisterResponse>.Failure(ResultPatternError.InternalServerError(Messages.RegisterUserFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(RegisterAsync), ex.Message);
            logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - start);
            return Result<RegisterResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.RegisterUserFailed));
        }
    }

    private async Task<BaseResult> CreateRadixAccountAsync(Guid userId, CancellationToken token)
    {
        const decimal minAmount = 0.1m;

        try
        {
            Result<(PublicKey PublicKey, PrivateKey PrivateKey, string SeedPhrase)> createRes =
                radixBridge.CreateAccountAsync();
            if (!createRes.IsSuccess) return BaseResult.Failure(createRes.Error);

            var account = createRes.Value;

            Result<string> addressRes =
                radixBridge.GetAddressAsync(account.PublicKey, AddressType.Account, NetworkType.Test, token);
            if (!addressRes.IsSuccess) return BaseResult.Failure(addressRes.Error);


            Result<TransactionResponse> depositRes =
                await radixBridge.DepositAsync(minAmount, addressRes.Value!);
            if (!depositRes.IsSuccess) return BaseResult.Failure(depositRes.Error);


            VirtualAccount radixAccount = new()
            {
                UserId = userId,
                PrivateKey = account.PrivateKey.RawHex(),
                PublicKey = account.PublicKey.ToString(),
                SeedPhrase = account.SeedPhrase,
                Address = addressRes.Value!,
                NetworkId = await GetNetworkIdAsync(Networks.Radix, token),
                NetworkType = Domain.Enums.NetworkType.Radix,
            };

            await dbContext.VirtualAccounts.AddAsync(radixAccount, token);
        }
        catch (Exception ex)
        {
            BaseResult.Failure(ResultPatternError.InternalServerError(ex.Message));
        }

        return BaseResult.Success();
    }

    private async Task<BaseResult> CreateSolanaAccountAsync(Guid userId, CancellationToken token)
    {
        const decimal minAmount = 0.1m;

        Result<(string PublicKey, string PrivateKey, string SeedPhrase)> createRes =
            await solanaBridge.CreateAccountAsync(token);
        if (!createRes.IsSuccess) return BaseResult.Failure(createRes.Error);

        var account = createRes.Value;

        Result<TransactionResponse> depositRes = await solanaBridge.DepositAsync(minAmount, account.PublicKey);
        if (!depositRes.IsSuccess) return BaseResult.Failure(depositRes.Error);

        VirtualAccount solanaAccount = new()
        {
            UserId = userId,
            PrivateKey = account.PrivateKey,
            PublicKey = account.PublicKey,
            SeedPhrase = account.SeedPhrase,
            Address = account.PublicKey,
            NetworkId = await GetNetworkIdAsync(Networks.Solana, token),
            NetworkType = Domain.Enums.NetworkType.Solana,
        };

        await dbContext.VirtualAccounts.AddAsync(solanaAccount, token);
        return BaseResult.Success();
    }

    private UserRole CreateUserRole(Guid userId, Guid roleId) => new()
    {
        UserId = userId,
        RoleId = roleId,
        CreatedBy = HttpAccessor.SystemId,
        CreatedByIp = accessor.GetRemoteIpAddress(),
    };

    private UserVerificationCode CreateVerificationCode(Guid userId, string? ip)
    {
        int duration = int.Parse(configuration["VerificationCode:DurationInMinute"] ?? "1");
        return new()
        {
            UserId = userId,
            CreatedBy = HttpAccessor.SystemId,
            CreatedByIp = ip,
            Code = VerificationHelper.GenerateVerificationCode(),
            StartTime = DateTimeOffset.UtcNow,
            ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(duration),
            Type = VerificationCodeType.None,
        };
    }

    private async Task<Guid> GetNetworkIdAsync(string networkName, CancellationToken token) =>
        await dbContext.Networks.Where(x => x.Name == networkName)
            .Select(x => x.Id).FirstAsync(token);


    /// <summary>
    /// Authenticates a user by verifying their credentials.
    /// Generates a new token version, issues an access token, logs the login event, and updates last login information.
    /// </summary>
    /// <param name="request">The login request containing the user's email and password.</param>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>Result</c> containing a <c>LoginResponse</c> with the authentication token and expiration data if successful;
    /// otherwise, a failure result with detailed error information.
    /// </returns>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(LoginAsync), date);

        User? user = await dbContext.Users.FirstOrDefaultAsync(x =>
                x.Email == request.Email && x.PasswordHash == HashingUtility.ComputeSha256Hash(request.Password),
            token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(ResultPatternError.BadRequest(Messages.LoginUserIncorrect));
        }

        user.TokenVersion = Guid.NewGuid();
        await dbContext.SaveChangesAsync(token);

        Result<LoginResponse> result = await dbContext.GenerateTokenAsync(user, configuration);
        if (!result.IsSuccess)
        {
            logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.GenerateTokenFailed));
        }

        string? userAgent = accessor.GetUserAgent();
        string remoteIpAddress = accessor.GetRemoteIpAddress();
        user.TotalLogins++;
        user.LastLoginAt = DateTimeOffset.UtcNow;

        UserToken userToken = new()
        {
            UserId = user.Id,
            UserAgent = userAgent,
            Token = result.Value!.Token,
            IpAddress = remoteIpAddress,
            TokenType = TokenType.AccessToken,
            Expiration = result.Value!.ExpiresAt,
        };

        UserLogin userLogin = new()
        {
            UserId = user.Id,
            UserAgent = userAgent,
            Successful = true,
            CreatedByIp = remoteIpAddress,
            CreatedBy = HttpAccessor.SystemId,
            IpAddress = remoteIpAddress
        };
        try
        {
            await dbContext.UserLogins.AddAsync(userLogin, token);
            await dbContext.UserTokens.AddAsync(userToken, token);

            await dbContext.SaveChangesAsync(token);

            logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Success(result.Value);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(LoginAsync), ex.Message);
            logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(ResultPatternError.InternalServerError(Messages.LoginUserFailed));
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// Invalidates the user's token by generating a new token version and updating user metadata.
    /// </summary>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the logout operation succeeded or failed.
    /// </returns>
    public async Task<BaseResult> LogoutAsync(CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(LogoutAsync), date);

        Guid userId = accessor.GetId();
        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(LogoutAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        user.TokenVersion = Guid.NewGuid();
        user.UpdatedBy = userId;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.Version++;

        try
        {
            return await dbContext.SaveChangesAsync(token) != 0
                ? BaseResult.Success()
                : BaseResult.Failure(ResultPatternError.InternalServerError(Messages.LogoutUserFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(LogoutAsync), ex.Message);
            logger.OperationCompleted(nameof(LogoutAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(ResultPatternError.InternalServerError(Messages.LogoutUserFailed));
        }
    }

    /// <summary>
    /// Changes the user's password after verifying the current password.
    /// Also updates the token version and logs the password change event.
    /// </summary>
    /// <param name="request">The change password request containing the old and new passwords.</param>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the password change succeeded or failed.
    /// </returns>
    public async Task<BaseResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ChangePasswordAsync), date);

        Guid? userId = accessor.GetId();
        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(ChangePasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        string hashedOldPassword = HashingUtility.ComputeSha256Hash(request.OldPassword);
        if (user.PasswordHash != hashedOldPassword)
        {
            logger.OperationCompleted(nameof(ChangePasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.ChangePasswordIncorrect));
        }

        user.PasswordHash = HashingUtility.ComputeSha256Hash(request.NewPassword);
        user.UpdatedBy = userId;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.Version++;
        user.LastPasswordChangeAt = DateTimeOffset.UtcNow;
        user.TokenVersion = Guid.NewGuid();

        try
        {
            logger.OperationCompleted(nameof(ChangePasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return await dbContext.SaveChangesAsync(token) != 0
                ? BaseResult.Success()
                : BaseResult.Failure(ResultPatternError.InternalServerError(Messages.ChangePasswordUserFailed));
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ChangePasswordAsync), ex.Message);
            logger.OperationCompleted(nameof(ChangePasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.ChangePasswordUserFailed));
        }
    }

    /// <summary>
    /// Sends a new email confirmation code to the user if their email is not yet confirmed.
    /// Generates and stores a verification code and dispatches an email to the provided address.
    /// </summary>
    /// <param name="request">The email confirmation code request containing the user's email address.</param>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the email was sent successfully or if any errors occurred.
    /// </returns>
    public async Task<BaseResult> SendEmailConfirmationCodeAsync(SendEmailConfirmationCodeRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(SendEmailConfirmationCodeAsync), date);

        User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == request.Email, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(SendEmailConfirmationCodeAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        if (user.EmailConfirmed)
        {
            logger.OperationCompleted(nameof(SendEmailConfirmationCodeAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }

        long verificationCode = VerificationHelper.GenerateVerificationCode();
        UserVerificationCode userVerificationCode = new()
        {
            CreatedByIp = accessor.GetRemoteIpAddress(),
            Code = verificationCode,
            StartTime = DateTimeOffset.UtcNow,
            ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(1),
            UserId = user.Id,
            Type = VerificationCodeType.EmailConfirmation,
            CreatedBy = user.Id,
        };

        try
        {
            await dbContext.UserVerificationCodes.AddAsync(userVerificationCode, token);
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(SendEmailConfirmationCodeAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(
                    ResultPatternError.InternalServerError(Messages.SendEmailConfirmationCodeFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(request.Email,
                Messages.SendEmailConfirmationSubjectMessage,
                $"Your email confirmation code is: {verificationCode}");
            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(SendEmailConfirmationCodeAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(
                    ResultPatternError.InternalServerError(Messages.SendEmailConfirmationCodeFailed));
            }

            logger.OperationCompleted(nameof(SendEmailConfirmationCodeAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(SendEmailConfirmationCodeAsync), ex.Message);
            logger.OperationCompleted(nameof(SendEmailConfirmationCodeAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.SendEmailConfirmationCodeFailed));
        }
    }

    /// <summary>
    /// Confirms the user's email address using a verification code.
    /// Validates the provided code, marks the email as confirmed, and sends a confirmation email notification.
    /// </summary>
    /// <param name="request">The email confirmation request containing the user's email and verification code.</param>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the email confirmation succeeded or failed.
    /// </returns>
    public async Task<BaseResult> ConfirmEmailAsync(ConfirmEmailCodeRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ConfirmEmailAsync), date);

        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        UserVerificationCode? verificationCode = await dbContext.UserVerificationCodes
            .Where(x => x.UserId == user.Id && x.Type == VerificationCodeType.EmailConfirmation)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(token);

        if (verificationCode is null ||
            verificationCode.Code != long.Parse(request.Code) ||
            (verificationCode.ExpiryTime - DateTimeOffset.UtcNow).TotalSeconds >= 60)
        {
            logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.ConfirmEmailInvalidOrExpiredTimeCode));
        }

        if (verificationCode.Code != long.Parse(request.Code))
        {
            logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.ConfirmEmailInvalidOrExpiredTimeCode));
        }

        user.EmailConfirmed = true;
        user.UpdatedBy = user.Id;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.Version++;

        try
        {
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.ConfirmEmailFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(user.Email,
                Messages.ConfirmEmailSubjectMessage,
                Messages.ConfirmEmailBodyMessage);

            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(
                    ResultPatternError.InternalServerError(Messages.EmailFailed));
            }

            logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ConfirmEmailAsync), ex.Message);
            logger.OperationCompleted(nameof(ConfirmEmailAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.ConfirmEmailFailed));
        }
    }

    /// <summary>
    /// Initiates the forgot password process by sending a password reset verification code to the user's email.
    /// </summary>
    /// <param name="request">The forgot password request containing the user's email address.</param>
    /// <param name="token">A cancellation token to cancel the async operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the operation succeeded or failed.
    /// </returns>
    public async Task<BaseResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ForgotPasswordAsync), date);

        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.EmailAddress, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(ForgotPasswordAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        UserVerificationCode resetCode = new()
        {
            UserId = user.Id,
            CreatedByIp = accessor.GetRemoteIpAddress(),
            Code = VerificationHelper.GenerateVerificationCode(),
            StartTime = DateTimeOffset.UtcNow,
            ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(1),
            Type = VerificationCodeType.PasswordReset,
            CreatedBy = user.Id
        };

        try
        {
            await dbContext.UserVerificationCodes.AddAsync(resetCode, token);
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(ForgotPasswordAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(
                    ResultPatternError.InternalServerError(Messages.ForgotPasswordFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(request.EmailAddress,
                Messages.ForgotPasswordSubjectMessage,
                @$"{Messages.ForgotPasswordBody1Message}{resetCode.Code}.{Messages.ForgotPasswordBody2Message}");

            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(ForgotPasswordAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(
                    ResultPatternError.InternalServerError(Messages.EmailFailed));
            }

            logger.OperationCompleted(nameof(ForgotPasswordAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ForgotPasswordAsync), ex.Message);
            logger.OperationCompleted(nameof(ForgotPasswordAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.ForgotPasswordFailed));
        }
    }


    /// <summary>
    /// Resets the user's password after verifying that a valid password reset code exists.
    /// It checks for the user's existence, validates the reset code against expiration and consistency, 
    /// updates the password with the newly provided one, and sends an email notification upon success.
    /// </summary>
    /// <param name="request">The reset password request, including email address and new password.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating success if the password was changed and the confirmation email was sent;
    /// otherwise, an error result with a corresponding error message.
    /// </returns>
    public async Task<BaseResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ResetPasswordAsync), date);

        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.EmailAddress, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        UserVerificationCode? resetCode = await dbContext.UserVerificationCodes
            .Where(x => x.UserId == user.Id && x.Type == VerificationCodeType.PasswordReset)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(token);

        if (resetCode is null)
        {
            logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.ResetPasswordInvalidOrExpiredCode));
        }

        if (resetCode.ExpiryTime < DateTimeOffset.UtcNow ||
            (resetCode.ExpiryTime - DateTimeOffset.UtcNow).TotalSeconds >= 60)
        {
            logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.ResetPasswordInvalidOrExpiredCode));
        }

        user.PasswordHash = HashingUtility.ComputeSha256Hash(request.NewPassword);
        user.UpdatedBy = user.Id;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.Version++;
        user.TokenVersion = Guid.NewGuid();

        try
        {
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.ResetPasswordFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(request.EmailAddress,
                Messages.ResetPasswordSubjectMessage, Messages.ResetPasswordBodyMessage);
            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.EmailFailed));
            }

            logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ResetPasswordAsync), ex.Message);
            logger.OperationCompleted(nameof(ResetPasswordAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(ResultPatternError.InternalServerError(Messages.ResetPasswordFailed));
        }
    }

    /// <summary>
    /// Initiates the account restoration process for a deleted user account.
    /// It verifies that the user exists and is marked as deleted, then generates a verification code 
    /// for restoration purposes and dispatches an email containing that code.
    /// </summary>
    /// <param name="request">The restore account request containing the user's email.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> that indicates whether the restoration process was successfully initiated or not.
    /// </returns>
    public async Task<BaseResult> RestoreAccountAsync(RestoreAccountRequest request, CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(RestoreAccountAsync), date);

        User? user = await dbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == request.Email, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        if (!user.IsDeleted)
        {
            logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.AccountAlreadyActive));
        }

        UserVerificationCode restoreCode = new()
        {
            CreatedByIp = accessor.GetRemoteIpAddress(),
            Code = VerificationHelper.GenerateVerificationCode(),
            StartTime = DateTimeOffset.UtcNow,
            ExpiryTime = DateTimeOffset.UtcNow.AddMinutes(1),
            UserId = user.Id,
            Type = VerificationCodeType.AccountRestore,
            CreatedBy = HttpAccessor.SystemId
        };

        try
        {
            await dbContext.UserVerificationCodes.AddAsync(restoreCode, token);
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.RestoreAccountFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(request.Email,
                Messages.RestoreAccountSubjectMessage,
                $"{Messages.RestoreAccountBodyMessage}{restoreCode.Code}");

            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.EmailFailed));
            }

            logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(RestoreAccountAsync), ex.Message);
            logger.OperationCompleted(nameof(RestoreAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(ResultPatternError.InternalServerError(Messages.RestoreAccountFailed));
        }
    }

    /// <summary>
    /// Confirms the restoration of a deleted user account using a provided verification code.
    /// The method validates that the user exists, checks if the account is deleted, and verifies the restore code 
    /// for correctness and expiry (including a check that the account is not too old to be restored).
    /// It then reverses deletion markers and dispatches a confirmation email.
    /// </summary>
    /// <param name="request">The confirm restore account request containing the user's email and the restore code.</param>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the confirmation of account restoration succeeded or if any errors occurred.
    /// </returns>
    public async Task<BaseResult> ConfirmRestoreAccountAsync(ConfirmRestoreAccountRequest request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ConfirmRestoreAccountAsync), date);

        User? user = await dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email == request.Email, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        if (!user.IsDeleted)
        {
            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }

        UserVerificationCode? restoreCode = await dbContext.UserVerificationCodes
            .Where(x => x.UserId == user.Id && x.Code == long.Parse(request.Code) &&
                        x.Type == VerificationCodeType.AccountRestore)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(token);

        if (restoreCode is null)
        {
            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(
                ResultPatternError.BadRequest(Messages.ConfirmRestoreAccountInvalidOrExpiredCode));
        }

        double difference = (DateTimeOffset.UtcNow - user.DeletedAt!.Value).TotalDays;
        if (difference > 30)
        {
            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(
                ResultPatternError.BadRequest(Messages.ConfirmRestoreAccountInvalidOrExpiredCode));
        }

        if (restoreCode.ExpiryTime < DateTimeOffset.UtcNow ||
            (restoreCode.ExpiryTime - DateTimeOffset.UtcNow).TotalSeconds >= 60)
        {
            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(
                ResultPatternError.BadRequest(Messages.ConfirmRestoreAccountInvalidOrExpiredCode));
        }

        user.DeletedAt = null;
        user.DeletedBy = null;
        user.DeletedByIp = null;
        user.IsDeleted = false;
        user.UpdatedBy = user.Id;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.Version++;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        user.TokenVersion = Guid.NewGuid();

        try
        {
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.ConfirmRestoreAccountFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(request.Email,
                Messages.ConfirmRestoreAccountSubjectMessage,
                Messages.ConfirmRestoreAccountBodyMessage);
            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.EmailFailed));
            }

            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ConfirmRestoreAccountAsync), ex.Message);
            logger.OperationCompleted(nameof(ConfirmRestoreAccountAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(
                ResultPatternError.InternalServerError(Messages.ConfirmRestoreAccountFailed));
        }
    }

    /// <summary>
    /// Deletes (soft-deletes) the current user's account.
    /// Marks the account as deleted by setting deletion metadata and flags, and then sends an email notification 
    /// confirming that the account deletion has taken effect.
    /// </summary>
    /// <param name="token">A cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// A <c>BaseResult</c> indicating whether the account deletion was successful or if an error occurred.
    /// </returns>
    public async Task<BaseResult> DeleteAccountAsync(CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(DeleteAccountAsync), date);

        Guid userId = accessor.GetId();

        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, token);
        if (user is null)
        {
            logger.OperationCompleted(nameof(DeleteAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Failure(ResultPatternError.NotFound(Messages.UserNotFound));
        }

        if (user.IsDeleted)
        {
            logger.OperationCompleted(nameof(DeleteAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }

        user.DeletedAt = DateTimeOffset.UtcNow;
        user.DeletedByIp = accessor.GetRemoteIpAddress();
        user.DeletedBy = userId;
        user.IsDeleted = true;
        user.UpdatedBy = userId;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        user.Version++;
        user.TokenVersion = Guid.NewGuid();

        try
        {
            int res = await dbContext.SaveChangesAsync(token);
            if (res == 0)
            {
                logger.OperationCompleted(nameof(DeleteAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.DeleteAccountFailed));
            }

            BaseResult emailResult = await emailService.SendEmailAsync(user.Email,
                Messages.DeleteAccountSubjectMessage,
                Messages.DeleteAccountBodyMessage);
            if (!emailResult.IsSuccess)
            {
                logger.OperationCompleted(nameof(DeleteAccountAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return BaseResult.Failure(ResultPatternError.InternalServerError(Messages.EmailFailed));
            }

            logger.OperationCompleted(nameof(DeleteAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(DeleteAccountAsync), ex.Message);
            logger.OperationCompleted(nameof(DeleteAccountAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<LoginResponse>.Failure(ResultPatternError.InternalServerError(Messages.DeleteAccountFailed));
        }
    }
}