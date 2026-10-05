namespace BuildingBlocks.Extensions.Smtp;

/// <summary>
/// Interface for interacting with an SMTP client to send emails.
/// </summary>
public interface ISmtpClientWrapper
{
    /// <summary>
    /// Asynchronously connects to the SMTP server.
    /// </summary>
    /// <returns>A task representing the result of the connection attempt.</returns>
    Task<BaseResult> ConnectAsync();

    /// <summary>
    /// Asynchronously sends an email message through the SMTP server.
    /// </summary>
    /// <param name="emailMessage">The email message to be sent.</param>
    /// <returns>A task representing the result of the email sending operation.</returns>
    Task<BaseResult> SendMessageAsync(MimeMessage emailMessage);

    /// <summary>
    /// Asynchronously disconnects from the SMTP server.
    /// </summary>
    /// <returns>A task representing the result of the disconnection operation.</returns>
    Task<BaseResult> DisconnectAsync();
}