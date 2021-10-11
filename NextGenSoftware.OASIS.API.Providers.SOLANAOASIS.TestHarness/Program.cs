using System;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string strText = "GsguXojeGATpZGW8VNfW8qQCBVodbW2qGS8bUEbdGZfv";
            var testData = Encoding.UTF8.GetBytes(strText);

            using var rsa = new RSACryptoServiceProvider(1024);
            try
            {                    
                var base64Encrypted = strText;

                var resultBytes = Convert.FromBase64String(base64Encrypted);
                var decryptedBytes = rsa.Decrypt(resultBytes, true);
                var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                Console.WriteLine(decryptedData.ToString());
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
    }
}