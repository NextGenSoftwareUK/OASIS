namespace BuildingBlocks.Extensions.Smtp;

/// <summary>
/// Service to handle sending emails using SMTP.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly ISmtpClientWrapper _smtpClientWrapper;
    private readonly EmailConfig _emailConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="smtpClientWrapper">The SMTP client wrapper used for sending messages.</param>
    /// <param name="emailConfig">The configuration for email settings.</param>
    public EmailService(ISmtpClientWrapper smtpClientWrapper, EmailConfig emailConfig)
    {
        _smtpClientWrapper = smtpClientWrapper ?? throw new ArgumentNullException(nameof(smtpClientWrapper));
        _emailConfig = emailConfig ?? throw new ArgumentNullException(nameof(emailConfig));
    }

    /// <summary>
    /// Asynchronously sends an email to the specified recipient.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The body of the email.</param>
    /// <returns>A task representing the result of the email sending operation.</returns>
    public async Task<BaseResult> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            MimeMessage emailMessage = CreateEmailMessage(toEmail, subject, body);

            // Connect to the SMTP server
            BaseResult connectResult = await _smtpClientWrapper.ConnectAsync();
            if (!connectResult.IsSuccess) return connectResult;

            // Send the email
            BaseResult sendResult = await _smtpClientWrapper.SendMessageAsync(emailMessage);
            if (!sendResult.IsSuccess) return sendResult;

            // Disconnect from the SMTP server
            BaseResult disconnectResult = await _smtpClientWrapper.DisconnectAsync();
            return disconnectResult;
        }
        catch (Exception ex)
        {
            // Catch unexpected errors and return as failure result
            return Result<BaseResult>.Failure(
                ResultPatternError.InternalServerError($"Unexpected error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates a <see cref="MimeMessage"/> with the given recipient, subject, and body.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The body of the email.</param>
    /// <returns>A <see cref="MimeMessage"/> representing the email to be sent.</returns>
    private MimeMessage CreateEmailMessage(string toEmail, string subject, string body)
    {
        return new MimeMessage
        {
            From = { new MailboxAddress(_emailConfig.SenderName, _emailConfig.SenderEmailAddress) },
            To = { new MailboxAddress(toEmail, toEmail) },
            Subject = subject,
            Body = new TextPart(TextFormat.Plain) { Text = body }
        };
    }
}