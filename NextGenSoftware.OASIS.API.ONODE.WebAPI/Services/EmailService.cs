//using MailKit.Net.Smtp;
//using MailKit.Security;
//using MimeKit;
//using MimeKit.Text;
//using System.Net;
//using System.Net.Mail;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
//{
//    public interface IEmailService
//    {
//        void Send(string to, string subject, string html, string from = null);
//    }

//    public class EmailService : IEmailService
//    {
//        public EmailService()
//        {

//        }

//        public void Send(string to, string subject, string html, string from = null)
//        {
//            // create message
//            var email = new MimeMessage();
//            email.Sender = MailboxAddress.Parse(from ?? OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.EmailFrom);
//            email.To.Add(MailboxAddress.Parse(to));
//            email.Subject = subject;
//            email.Body = new TextPart(TextFormat.Html) { Text = html };

//            // send email
//            using var smtp = new MailKit.Net.Smtp.SmtpClient();
//            //smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, SecureSocketOptions.StartTls);
//            smtp.Connect(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpHost, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpPort, SecureSocketOptions.StartTls);
//            smtp.Authenticate(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpUser, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpPass);
//            smtp.Send(email);
//            smtp.Disconnect(true);



//            MailAddress addressTo = new MailAddress(to);
//            MailAddress addressFrom = new MailAddress(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpUser);

//            MailMessage message = new MailMessage(from, to);
//            message.Subject = subject + "2";
//            message.Body = html;

//            //System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpHost, 2525)
//            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpHost, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpPort)
//            {
//                Credentials = new NetworkCredential(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpUser, OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Email.SmtpPass),
//                EnableSsl = true
//            };
//            // code in brackets above needed if authentication required   

//            try
//            {
//                client.Send(message);
//            }
//            catch (SmtpException ex)
//            {
//                //log here.
//                throw;
//            }
//        }
//    }
//}