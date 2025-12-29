namespace BuildingBlocks.Extensions.Smtp;

/// <summary>
/// Interface for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Asynchronously sends an email to the specified recipient.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The body of the email.</param>
    /// <returns>A task representing the result of the email sending operation.</returns>
    Task<BaseResult> SendEmailAsync(string toEmail, string subject, string body);
}