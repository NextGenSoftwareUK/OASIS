using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            bool emailValid = false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                emailValid = addr.Address == email;
            }
            catch
            {
                return false;
            }

            //TODO: Not sure if we need to verify the email again?
            if (emailValid)
                return IsValidEmail2(email);

            return emailValid;
        }

        //TODO: Need to check which of these methods is best to use? :)
        private static bool IsValidEmail2(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
