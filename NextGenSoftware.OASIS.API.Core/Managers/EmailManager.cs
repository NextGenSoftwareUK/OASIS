using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using NextGenSoftware.OASIS.API.DNA;
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
            // create message
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(from ?? _OASISDNA.OASIS.Email.EmailFrom);
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };

            // send email
            using var smtp = new SmtpClient();
            //smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, SecureSocketOptions.StartTls);
            smtp.Connect(_OASISDNA.OASIS.Email.SmtpHost, _OASISDNA.OASIS.Email.SmtpPort, SecureSocketOptions.None);
            smtp.Authenticate(_OASISDNA.OASIS.Email.SmtpUser, _OASISDNA.OASIS.Email.SmtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
