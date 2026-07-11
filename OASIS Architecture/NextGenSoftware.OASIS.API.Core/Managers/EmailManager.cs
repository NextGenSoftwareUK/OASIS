using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Resend;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
//using System.Net.Mail;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    // Minimal IOptionsSnapshot wrapper
    public class OptionsSnapshot<T> : IOptionsSnapshot<T> where T : class
    {
        public OptionsSnapshot(T value) => Value = value;
        public T Value { get; }
        public T Get(string? name) => Value;
    }


    public static class EmailManager
    {
        private static OASISDNA _OASISDNA;
        private static IResend _resend;

        public static string LogoBase64 { get; set; }

        //static EmailManager()
        //{

        //}

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

            var options = new OptionsSnapshot<ResendClientOptions>(new ResendClientOptions
            {
                ApiToken = OASISDNA.OASIS.Email.ResendKey
            });

            var httpClient = new HttpClient();
            _resend = new ResendClient(options, httpClient);

            try
            {
                LogoBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "OASISLogo.jpg")));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError($"Error occured in EmailManager.Initialize creating LogoBase64. Reason: {e}");
            }
        }

        public static async Task SendAsync(string to, string subject, string html, string from = null)
        {
            if (_OASISDNA.OASIS.Email.DisableAllEmails)
                return;

            var message = new EmailMessage();
            message.From = from ?? _OASISDNA.OASIS.Email.EmailFrom;
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = html;
            await _resend.EmailSendAsync(message);
        }

        public static void Send(string to, string subject, string html, string from = null)
        {
            if (_OASISDNA.OASIS.Email.DisableAllEmails)
                return;

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


            //MailAddress addressTo = new MailAddress(to);
            //MailAddress addressFrom = new MailAddress(_OASISDNA.OASIS.Email.SmtpUser);

            //MailMessage message = new MailMessage(from ?? _OASISDNA.OASIS.Email.EmailFrom, to);
            //message.IsBodyHtml = true;
            //message.Subject = subject;
            //message.Body = html;

            //System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(_OASISDNA.OASIS.Email.SmtpHost, _OASISDNA.OASIS.Email.SmtpPort)
            //{
            //    Credentials = new NetworkCredential(_OASISDNA.OASIS.Email.SmtpUser, _OASISDNA.OASIS.Email.SmtpPass),
            //    EnableSsl = true
            //};

            //try
            //{
            //    client.Send(message);
            //}
            //catch (SmtpException ex)
            //{
            //    LoggingManager.Log(string.Concat("ERROR Sending Email. Exception: ", ex.ToString()), LogType.Error);
            //}
        }
    }
}