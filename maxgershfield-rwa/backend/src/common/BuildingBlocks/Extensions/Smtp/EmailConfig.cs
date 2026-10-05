namespace BuildingBlocks.Extensions.Smtp;

/// <summary>
/// Represents the configuration settings for SMTP email sending.
/// </summary>
public sealed class EmailConfig
{
    /// <summary>
    /// Gets or sets the SMTP server address (e.g., smtp.gmail.com).
    /// </summary>
    public required string SmtpServer { get; init; }

    /// <summary>
    /// Gets or sets the SMTP port (typically 587 for TLS or 465 for SSL).
    /// </summary>
    public required int SmtpPort { get; init; }

    /// <summary>
    /// Gets or sets the sender's email address (e.g., sender@example.com).
    /// </summary>
    public required string SenderEmailAddress { get; init; }

    /// <summary>
    /// Gets or sets the name of the sender that will appear in the "From" field of the email.
    /// </summary>
    public required string SenderName { get; init; }

    /// <summary>
    /// Gets or sets the application-specific password (for example, Gmail App Password).
    /// This is used for authentication with the SMTP server.
    /// </summary>
    public required string AppPassword { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable SSL (Secure Sockets Layer) encryption for the connection.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableSsl { get; init; } = true;

    /// <summary>
    /// Gets or sets the timeout (in milliseconds) for the SMTP connection attempt.
    /// </summary>
    public int Timeout { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts in case of failure while sending an email.
    /// </summary>
    public int MaxRetryAttempts { get; init; }

    /// <summary>
    /// Gets or sets the delay (in milliseconds) between retry attempts when an email fails to send.
    /// </summary>
    public int RetryDelay { get; init; }
}