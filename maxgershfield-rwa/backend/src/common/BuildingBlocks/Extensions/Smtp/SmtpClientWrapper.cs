using BuildingBlocks.Extensions.Resources;

namespace BuildingBlocks.Extensions.Smtp;

public sealed class SmtpClientWrapper(EmailConfig emailConfig, SmtpClient client) : ISmtpClientWrapper
{
    /// <summary>
    /// Asynchronously connects to the SMTP server and authenticates the user.
    /// Retries connection up to the maximum specified number of attempts.
    /// </summary>
    /// <returns>A task representing the result of the connection attempt.</returns>
    public async Task<BaseResult> ConnectAsync()
    {
        for (int attempt = 1; attempt <= emailConfig.MaxRetryAttempts; attempt++)
        {
            try
            {
                client.Timeout = emailConfig.Timeout;
                await client.ConnectAsync(emailConfig.SmtpServer, emailConfig.SmtpPort, emailConfig.EnableSsl);
                await client.AuthenticateAsync(emailConfig.SenderEmailAddress, emailConfig.AppPassword);
                return BaseResult.Success();
            }
            catch (Exception ex)
            {
                // Retry on failure up to the max number of attempts
                if (attempt == emailConfig.MaxRetryAttempts)
                {
                    return BaseResult.Failure(
                        ResultPatternError.InternalServerError($"SMTP Connection Failed: {ex.Message}"));
                }

                await Task.Delay(emailConfig.RetryDelay); // Wait before retrying
            }
        }

        // Return failure if the connection could not be established
        return BaseResult.Failure(
            ResultPatternError.InternalServerError(Messages.SmtpConnectionFailed));
    }

    /// <summary>
    /// Asynchronously sends the specified email message.
    /// </summary>
    /// <param name="emailMessage">The email message to be sent.</param>
    /// <returns>A task representing the result of the email sending operation.</returns>
    public async Task<BaseResult> SendMessageAsync(MimeMessage emailMessage)
    {
        try
        {
            await client.SendAsync(emailMessage);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            // Return failure if email sending fails
            return BaseResult.Failure(ResultPatternError.InternalServerError($"Email Sending Failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Asynchronously disconnects from the SMTP server.
    /// </summary>
    /// <returns>A task representing the result of the disconnection operation.</returns>
    public async Task<BaseResult> DisconnectAsync()
    {
        try
        {
            await client.DisconnectAsync(true);
            return BaseResult.Success();
        }
        catch (Exception ex)
        {
            // Return failure if disconnect fails
            return BaseResult.Failure(ResultPatternError.InternalServerError($"SMTP Disconnect Failed: {ex.Message}"));
        }
    }
}