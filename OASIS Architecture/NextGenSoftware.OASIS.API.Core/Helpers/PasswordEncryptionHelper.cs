using System;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.DNA;
using BC = BCrypt.Net.BCrypt;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// Implements the configurable 3-layer password encryption stack defined in OASISDNA.Security.AvatarPassword:
    ///   Layer 1 (base): BCrypt — adaptive cost hash with embedded salt
    ///   Layer 2: Rijndael-256 (AES-256-CBC) — symmetric encryption of the BCrypt output
    ///   Layer 3: Quantum-resistant AES-256-GCM — AEAD wrap of the Rijndael-256 output
    ///
    /// Stored format prefixes allow VerifyPassword to identify which layers are present:
    ///   "$2..."   = BCrypt only
    ///   "r256:…"  = Rijndael-256 wrapping BCrypt
    ///   "qpq:…"   = AES-256-GCM wrapping Rijndael-256 wrapping BCrypt
    ///
    /// Keys are derived from the OASISDNA passphrase strings via SHA-256.
    /// Layer 3 is considered post-quantum resistant for symmetric encryption
    /// (AES-256-GCM provides 128-bit security even under Grover's algorithm).
    /// </summary>
    public static class PasswordEncryptionHelper
    {
        private const string R256Prefix = "r256:";
        private const string QpqPrefix  = "qpq:";

        // ──────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────

        public static string HashPassword(string plaintext, EncryptionSettings settings)
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentNullException(nameof(plaintext));

            string value = plaintext;

            if (settings?.BCryptEncryptionEnabled == true)
                value = BC.HashPassword(value);

            if (settings?.Rijndael256EncryptionEnabled == true && !string.IsNullOrEmpty(settings.Rijndael256Key))
                value = R256Prefix + AesCbcEncrypt(value, DeriveKey(settings.Rijndael256Key));

            if (settings?.QuantumEncryptionEnabled == true && !string.IsNullOrEmpty(settings.QuantumEncryptionKey))
                value = QpqPrefix + AesGcmEncrypt(value, DeriveKey(settings.QuantumEncryptionKey));

            return value;
        }

        public static bool VerifyPassword(string plaintext, string storedHash, EncryptionSettings settings)
        {
            if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                string inner = storedHash;

                // Peel layers outward → inward
                if (inner.StartsWith(QpqPrefix) && settings?.QuantumEncryptionEnabled == true && !string.IsNullOrEmpty(settings.QuantumEncryptionKey))
                    inner = AesGcmDecrypt(inner[QpqPrefix.Length..], DeriveKey(settings.QuantumEncryptionKey));

                if (inner.StartsWith(R256Prefix) && settings?.Rijndael256EncryptionEnabled == true && !string.IsNullOrEmpty(settings.Rijndael256Key))
                    inner = AesCbcDecrypt(inner[R256Prefix.Length..], DeriveKey(settings.Rijndael256Key));

                // Bottom layer: BCrypt or plain
                if (inner.StartsWith("$2"))
                    return BC.Verify(plaintext, inner);

                // Fallback: BCrypt the plaintext and compare (shouldn't happen in normal flow)
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Returns true when the value has already been processed by HashPassword.</summary>
        public static bool IsAlreadyHashed(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.StartsWith(QpqPrefix) || value.StartsWith(R256Prefix) || value.StartsWith("$2");
        }

        // ──────────────────────────────────────────────────────────────
        // AES-256-CBC (Rijndael-256 Layer)
        //   Output: Base64(IV[16] | ciphertext)
        // ──────────────────────────────────────────────────────────────

        private static string AesCbcEncrypt(string plaintext, byte[] key)
        {
            using var aes = Aes.Create();
            aes.KeySize  = 256;
            aes.Mode     = CipherMode.CBC;
            aes.Padding  = PaddingMode.PKCS7;
            aes.Key      = key;
            aes.GenerateIV();

            byte[] iv        = aes.IV;
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);

            using var enc = aes.CreateEncryptor();
            byte[] cipher  = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] blob = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv,     0, blob, 0,          iv.Length);
            Buffer.BlockCopy(cipher, 0, blob, iv.Length,  cipher.Length);
            return Convert.ToBase64String(blob);
        }

        private static string AesCbcDecrypt(string base64, byte[] key)
        {
            byte[] blob = Convert.FromBase64String(base64);
            byte[] iv     = new byte[16];
            byte[] cipher = new byte[blob.Length - 16];
            Buffer.BlockCopy(blob, 0,  iv,     0, 16);
            Buffer.BlockCopy(blob, 16, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key     = key;
            aes.IV      = iv;

            using var dec = aes.CreateDecryptor();
            byte[] plain  = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }

        // ──────────────────────────────────────────────────────────────
        // AES-256-GCM (Quantum-Resistant Layer)
        //   Output: Base64(nonce[12] | tag[16] | ciphertext)
        //   AES-256-GCM provides ~128-bit post-quantum security (Grover).
        // ──────────────────────────────────────────────────────────────

        private static string AesGcmEncrypt(string plaintext, byte[] key)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] nonce      = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12
            byte[] tag        = new byte[AesGcm.TagByteSizes.MaxSize];   // 16
            byte[] cipher     = new byte[plainBytes.Length];

            RandomNumberGenerator.Fill(nonce);

            using var gcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            gcm.Encrypt(nonce, plainBytes, cipher, tag);

            byte[] blob = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce,  0, blob, 0,                            nonce.Length);
            Buffer.BlockCopy(tag,    0, blob, nonce.Length,                 tag.Length);
            Buffer.BlockCopy(cipher, 0, blob, nonce.Length + tag.Length,    cipher.Length);
            return Convert.ToBase64String(blob);
        }

        private static string AesGcmDecrypt(string base64, byte[] key)
        {
            byte[] blob   = Convert.FromBase64String(base64);
            int nonceLen  = AesGcm.NonceByteSizes.MaxSize;  // 12
            int tagLen    = AesGcm.TagByteSizes.MaxSize;     // 16
            int cipherLen = blob.Length - nonceLen - tagLen;

            byte[] nonce  = new byte[nonceLen];
            byte[] tag    = new byte[tagLen];
            byte[] cipher = new byte[cipherLen];
            byte[] plain  = new byte[cipherLen];

            Buffer.BlockCopy(blob, 0,                         nonce,  0, nonceLen);
            Buffer.BlockCopy(blob, nonceLen,                  tag,    0, tagLen);
            Buffer.BlockCopy(blob, nonceLen + tagLen,         cipher, 0, cipherLen);

            using var gcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            gcm.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }

        // ──────────────────────────────────────────────────────────────
        // Key derivation: SHA-256 of the passphrase string → 32 bytes
        // ──────────────────────────────────────────────────────────────

        private static byte[] DeriveKey(string passphrase)
            => SHA256.HashData(Encoding.UTF8.GetBytes(passphrase));
    }
}
