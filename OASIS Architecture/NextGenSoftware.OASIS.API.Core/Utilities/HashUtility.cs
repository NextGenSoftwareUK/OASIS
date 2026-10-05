using System;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core.Utilities
{
    public static class HashUtility
    {
        public static int GetNumericHash(string value)
        {
            var md5Hasher = MD5.Create();
            var hashedValue = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Math.Abs(BitConverter.ToInt32(hashedValue, 0));
        }
        
        public static int GetNumericHash(Guid value)
        {
            var md5Hasher = MD5.Create();
            var hashedValue = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
            return Math.Abs(BitConverter.ToInt32(hashedValue, 0));
        }
    }
}