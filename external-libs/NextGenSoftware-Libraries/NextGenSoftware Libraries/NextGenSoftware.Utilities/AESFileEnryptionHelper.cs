using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.Utilities
{
    public static class FileEncryption
    {
        public static void EncryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using (FileStream fileStream = new FileStream(outputFile, FileMode.Create))
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (CryptoStream cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (FileStream inputStream = new FileStream(inputFile, FileMode.Open))
                {
                    inputStream.CopyTo(cryptoStream);
                }
            }
        }

        public static void DecryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using (FileStream inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(outputStream);
                }
            }
        }

        //public static MemoryStream DecryptFile(string inputFile, byte[] key, byte[] iv)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    using (FileStream inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
        //    using (Aes aes = Aes.Create())
        //    {
        //        aes.Key = key;
        //        aes.IV = iv;

        //        using (CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
        //        {
        //            cryptoStream.CopyTo(stream);
        //        }
        //    }

        //    return stream;
        //}

        public static string DecryptFile(string inputFile, byte[] key, byte[] iv)
        {
            MemoryStream stream = new MemoryStream();
            using (FileStream inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(stream);
                }
            }

            return new StreamReader(stream).ReadToEnd().ToString();
        }

        public static string Encrypt(this string stringToEncrypt, string key)
        {
            if (string.IsNullOrEmpty(stringToEncrypt))
            {
                throw new ArgumentException("An empty string value cannot be encrypted.");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cannot encrypt using an empty key. Please supply an encryption key.");
            }

            CspParameters cspp = new CspParameters();
            cspp.KeyContainerName = key;

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;

            byte[] bytes = rsa.Encrypt(UTF8Encoding.UTF8.GetBytes(stringToEncrypt), true);

            return BitConverter.ToString(bytes);
        }

        public static string Decrypt(this string stringToDecrypt, string key)
        {
            string result = null;

            if (string.IsNullOrEmpty(stringToDecrypt))
            {
                throw new ArgumentException("An empty string value cannot be encrypted.");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cannot decrypt using an empty key. Please supply a decryption key.");
            }

            try
            {
                CspParameters cspp = new CspParameters();
                cspp.KeyContainerName = key;

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cspp);
                rsa.PersistKeyInCsp = true;

                string[] decryptArray = stringToDecrypt.Split(new string[] { "-" }, StringSplitOptions.None);
                byte[] decryptByteArray = Array.ConvertAll<string, byte>(decryptArray, (s => Convert.ToByte(byte.Parse(s, System.Globalization.NumberStyles.HexNumber))));

                byte[] bytes = rsa.Decrypt(decryptByteArray, true);

                result = UTF8Encoding.UTF8.GetString(bytes);
            }
            finally
            {
                // no need for further processing
            }

            return result;
        }
    }
}