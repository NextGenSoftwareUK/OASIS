using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public interface IEmailService
    {
        void Send(string to, string subject, string html, string from = null);
    }

    public class EmailService : IEmailService
    {
        public EmailService()
        {

        }

        public void Send(string to, string subject, string html, string from = null)
        {
            // create message
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(from ?? OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.EmailFrom);
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };

            // send email
            using var smtp = new SmtpClient();
            //smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, SecureSocketOptions.StartTls);
            smtp.Connect(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpHost, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpPort, SecureSocketOptions.None);
            smtp.Authenticate(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpUser, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}