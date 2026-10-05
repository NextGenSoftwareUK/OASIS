using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Email;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly OASISDNA _oasisDNA;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _oasisDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
        }

        public bool IsConfigured()
        {
            try
            {
                // Check if email is disabled in OASIS DNA
                if (_oasisDNA?.OASIS?.Email?.DisableAllEmails == true)
                {
                    _logger?.LogWarning("Email is disabled in OASIS DNA configuration");
                    return false;
                }

                // Check if SMTP settings are configured
                if (string.IsNullOrEmpty(_oasisDNA?.OASIS?.Email?.SmtpHost) ||
                    string.IsNullOrEmpty(_oasisDNA?.OASIS?.Email?.SmtpUser) ||
                    string.IsNullOrEmpty(_oasisDNA?.OASIS?.Email?.SmtpPass))
                {
                    _logger?.LogWarning("SMTP settings are not configured in OASIS DNA");
                    return false;
                }

                // Ensure EmailManager is initialized
                if (!EmailManager.IsInitialized)
                {
                    EmailManager.Initialize(_oasisDNA);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking email configuration");
                return false;
            }
        }

        public async Task<EmailResult> SendVerificationEmailAsync(EmailVerificationData data)
        {
            var result = new EmailResult { Success = false };

            try
            {
                if (!IsConfigured())
                {
                    result.ErrorMessage = "Email service is not configured";
                    return result;
                }

                if (data == null)
                {
                    result.ErrorMessage = "Email verification data is null";
                    return result;
                }

                if (string.IsNullOrEmpty(data.To))
                {
                    result.ErrorMessage = "Recipient email address is required";
                    return result;
                }

                // Build HTML email body
                var emailBody = BuildVerificationEmailHtml(data);

                // Send email using EmailManager
                await Task.Run(() =>
                {
                    try
                    {
                        EmailManager.Send(
                            data.To,
                            data.Subject ?? "OASIS Avatar Email Verification",
                            emailBody,
                            _oasisDNA.OASIS.Email.EmailFrom
                        );
                        result.Success = true;
                        _logger?.LogInformation("Verification email sent successfully to {Email}", data.To);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessage = $"Failed to send email: {ex.Message}";
                        _logger?.LogError(ex, "Error sending verification email to {Email}", data.To);
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                _logger?.LogError(ex, "Unexpected error sending verification email");
                return result;
            }
        }

        private string BuildVerificationEmailHtml(EmailVerificationData data)
        {
            var verificationUrl = data.VerificationUrl ?? 
                $"{_configuration["Email:VerificationBaseUrl"] ?? "https://oasisweb4.com/#"}/avatar/verify-email?token={Uri.EscapeDataString(data.VerificationToken)}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #f4f4f4; padding: 20px; border-radius: 5px;'>
        <h2 style='color: #2c3e50;'>Verify Email</h2>
        <p>Thanks for registering!</p>
        <p>Please click the button below to verify your email address:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{verificationUrl}' 
               style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>
                Verify
            </a>
        </div>
        <p style='margin-top: 30px; font-size: 12px; color: #7f8c8d;'>
            If you did not create an account with OASIS, please ignore this email.
        </p>
    </div>
</body>
</html>";
        }
    }
}



