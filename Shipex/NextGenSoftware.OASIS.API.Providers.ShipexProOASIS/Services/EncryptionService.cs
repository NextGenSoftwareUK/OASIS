using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// AES-256 encryption service for securing sensitive credentials
/// Uses encryption key from configuration or environment variable
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int Iterations = 10000;

    public EncryptionService(IConfiguration configuration)
    {
        // Get encryption key from configuration or environment variable
        _encryptionKey = configuration["ShipexPro:EncryptionKey"] 
                         ?? Environment.GetEnvironmentVariable("SHIPEXPRO_ENCRYPTION_KEY")
                         ?? throw new InvalidOperationException("Encryption key not configured. Set ShipexPro:EncryptionKey in appsettings.json or SHIPEXPRO_ENCRYPTION_KEY environment variable.");
        
        // Ensure key is at least 32 bytes (256 bits) for AES-256
        if (Encoding.UTF8.GetByteCount(_encryptionKey) < 32)
        {
            throw new InvalidOperationException("Encryption key must be at least 32 characters (256 bits) for AES-256 encryption.");
        }
    }

    /// <summary>
    /// Encrypts a plain text string using AES-256
    /// </summary>
    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            // Generate salt
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive key from password using PBKDF2
            var key = DeriveKey(_encryptionKey, salt, KeySize / 8);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;

                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Write salt and IV
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        await sw.WriteAsync(plainText);
                    }

                    // Return base64 encoded encrypted data
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to encrypt data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts an encrypted string using AES-256
    /// </summary>
    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        try
        {
            var fullCipher = Convert.FromBase64String(encryptedText);

            // Extract salt and IV from encrypted data
            var salt = new byte[16];
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 32];

            Array.Copy(fullCipher, 0, salt, 0, 16);
            Array.Copy(fullCipher, 16, iv, 0, 16);
            Array.Copy(fullCipher, 32, cipher, 0, cipher.Length);

            // Derive key from password using PBKDF2
            var key = DeriveKey(_encryptionKey, salt, KeySize / 8);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipher))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return await sr.ReadToEndAsync();
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to decrypt data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Derives a cryptographic key from a password using PBKDF2
    /// </summary>
    private byte[] DeriveKey(string password, byte[] salt, int keyLength)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(keyLength);
        }
    }
}




