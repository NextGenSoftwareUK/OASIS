namespace API.Infrastructure.DI;

/// <summary>
/// This class registers email service dependencies within the DI container for the application.
/// It configures services necessary for sending emails, including SMTP client and email configuration.
/// </summary>
public static class EmailRegister
{
    /// <summary>
    /// Registers the email service related dependencies in the DI container.
    /// It loads email configuration settings from the application's configuration and adds the necessary services for email handling.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to register services with.</param>
    /// <returns>The WebApplicationBuilder instance with the email service dependencies registered.</returns>
    public static WebApplicationBuilder AddEmailService(this WebApplicationBuilder builder)
    {
        EmailConfig? emailConfig = builder.Configuration
            .GetSection("EmailConfiguration")
            .Get<EmailConfig>();

        builder.Services.AddSingleton(emailConfig!);

        builder.Services.AddTransient<SmtpClient>();

        builder.Services.AddScoped<IEmailService, EmailService>();

        builder.Services.AddScoped<ISmtpClientWrapper, SmtpClientWrapper>();

        return builder;
    }
}