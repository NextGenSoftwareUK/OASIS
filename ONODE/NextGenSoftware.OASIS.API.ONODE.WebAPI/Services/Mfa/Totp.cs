using System;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa
{
    /// <summary>RFC 6238 time-based one-time passwords (HMAC-SHA1) and RFC 4648 base32 for authenticator apps.</summary>
    public static class Totp
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20);

        public static string Base32Encode(byte[] data)
        {
            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b; bits += 8;
                while (bits >= 5) { sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
            }
            if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static byte[] Base32Decode(string text)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            int buffer = 0, bits = 0;
            foreach (var c in text.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant())
            {
                var v = Alphabet.IndexOf(c);
                if (v < 0) throw new FormatException("Invalid base32 character.");
                buffer = (buffer << 5) | v; bits += 5;
                if (bits >= 8) { bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF)); bits -= 8; }
            }
            return bytes.ToArray();
        }

        public static long Step(DateTime utc, int stepSeconds) => (long)Math.Floor((utc.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds / stepSeconds);

        public static string Code(byte[] secret, long step, int digits = 6)
        {
            var counter = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(counter);
            using var hmac = new HMACSHA1(secret);
            var hash = hmac.ComputeHash(counter);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            var mod = 1; for (var i = 0; i < digits; i++) mod *= 10;
            return (binary % mod).ToString(new string('0', digits));
        }

        /// <summary>Returns the matching time-step (within drift) or -1. Constant-time comparison.</summary>
        public static long Match(byte[] secret, string submitted, DateTime utc, int stepSeconds, int drift, int digits = 6)
        {
            if (string.IsNullOrWhiteSpace(submitted)) return -1;
            submitted = submitted.Replace(" ", "").Replace("-", "");
            var now = Step(utc, stepSeconds);
            long matched = -1;
            for (var d = -drift; d <= drift; d++)
            {
                var candidate = Code(secret, now + d, digits);
                if (submitted.Length == candidate.Length && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), Encoding.ASCII.GetBytes(submitted)))
                    matched = now + d;
            }
            return matched;
        }

        public static string OtpAuthUri(string issuer, string account, byte[] secret, int digits, int stepSeconds)
            => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={Base32Encode(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={digits}&period={stepSeconds}";
    }
}
