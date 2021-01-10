using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cryptography.ECDSA;

namespace EOSNewYork.EOSCore.Utilities
{
    public class WifUtility : Base58
    {
        public static string GetPrivateWif(byte[] source)
        {
            var updatedSource = AddFirstBytes(source, 1);
            updatedSource[0] = 0x80;
            var doubleHash = DoubleHash(updatedSource);
            updatedSource = AddLastBytes(updatedSource, CheckSumSizeInBytes);
            Array.Copy(doubleHash, 0, updatedSource, updatedSource.Length - CheckSumSizeInBytes, CheckSumSizeInBytes);
            return Encode(updatedSource);
        }
        public static string GetPublicWif(byte[] publicKey, string prefix)
        {
            var hash = Ripemd160Manager.GetHash(publicKey);
            var updatedPublicKey = AddLastBytes(publicKey, CheckSumSizeInBytes);
            Array.Copy(hash, 0, updatedPublicKey, updatedPublicKey.Length - CheckSumSizeInBytes, CheckSumSizeInBytes);
            var encodedHash = Encode(updatedPublicKey);
            return prefix + encodedHash;
        }
        public static byte[] DecodePrivateWif(string data)
        {
            if (data.All(Hexdigits.Contains))
                return Hex.HexToBytes(data);

            switch (data[0])
            {
                case '5':
                case '6':
                    return Base58CheckDecode(data);
                case 'K':
                case 'L':
                    return CutLastBytes(Base58CheckDecode(data), 1);
                default:
                    throw new NotImplementedException();
            }
        }
        public static byte[] Base58CheckDecode(string data)
        {
            var s = Decode(data);
            var dec = CutLastBytes(s, CheckSumSizeInBytes);

            var checksum = DoubleHash(dec);
            for (var i = 0; i < CheckSumSizeInBytes; i++)
            {
                if (checksum[i] != s[s.Length - CheckSumSizeInBytes + i])
                    throw new ArithmeticException("Invalide data");
            }

            return CutFirstBytes(dec, 1);
        }
        public static string EncodeSignature(byte[] source)
        {
            var buf = AddLastBytes(source, 2);
            buf[source.Length] = 0x4b; //K
            buf[source.Length + 1] = 0x31; //1
            var checksum = Ripemd160Manager.GetHash(buf);

            buf = AddLastBytes(source, CheckSumSizeInBytes);
            Array.Copy(checksum, 0, buf, source.Length, CheckSumSizeInBytes);
            return "SIG_K1_" + Encode(buf);
        }
    }
}