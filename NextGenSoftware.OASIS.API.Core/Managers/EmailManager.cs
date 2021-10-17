using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using NextGenSoftware.OASIS.API.DNA;
using System.Net;
using System.Net.Mail;
//using System.Net.Mail;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public static class EmailManager
    {
        private static OASISDNA _OASISDNA;

        public static bool IsInitialized
        {
            get
            {
                return _OASISDNA != null;
            }
        }

        public static void Initialize(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
        }

        public static void Send(string to, string subject, string html, string from = null)
        {
            // For some unknown reason the emails sent from the code below (using mailkit) never arrive, the standard .net code after works! lol ;-)
            
            /*
            // create message
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(from ?? _OASISDNA.OASIS.Email.EmailFrom);
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };

            // send email
            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            //smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, SecureSocketOptions.StartTls);
            smtp.Connect(_OASISDNA.OASIS.Email.SmtpHost, _OASISDNA.OASIS.Email.SmtpPort, SecureSocketOptions.StartTls);
            smtp.Authenticate(_OASISDNA.OASIS.Email.SmtpUser, _OASISDNA.OASIS.Email.SmtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);*/

            MailAddress addressTo = new MailAddress(to);
            MailAddress addressFrom = new MailAddress(_OASISDNA.OASIS.Email.SmtpUser);

            MailMessage message = new MailMessage(from ?? _OASISDNA.OASIS.Email.EmailFrom, to);
            message.Subject = subject;
            message.Body = html;

            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(_OASISDNA.OASIS.Email.SmtpHost, _OASISDNA.OASIS.Email.SmtpPort)
            {
                Credentials = new NetworkCredential(_OASISDNA.OASIS.Email.SmtpUser, _OASISDNA.OASIS.Email.SmtpPass),
                EnableSsl = true
            };

            try
            {
                client.Send(message);
            }
            catch (SmtpException ex)
            {
                LoggingManager.Log(string.Concat("ERROR Sending Email. Exception: ", ex.ToString()), Enums.LogType.Error);
            }
        }
    }
}