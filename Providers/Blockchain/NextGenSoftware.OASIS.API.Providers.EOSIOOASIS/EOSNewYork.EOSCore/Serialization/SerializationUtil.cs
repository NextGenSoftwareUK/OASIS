using System;
using System.IO;

namespace EOSNewYork.EOSCore.Serialization
{
    public class SerializationUtil
    {
        public static ulong StringToULong(string str)
        {
            var length = str.Length;
            ulong value = 0;

            for (var i = 0; i <= 12; ++i)
            {
                ulong c = 0;
                if (i < length && i <= 12)
                    c = CharToULong(str[i]);

                if (i < 12)
                {
                    c &= 0x1f;
                    c <<= 64 - 5 * (i + 1);
                }
                else
                {
                    c &= 0x0f;
                }

                value |= c;
            }

            return value;
        }

        public static ulong CharToULong(char c)
        {
            if (c >= 'a' && c <= 'z')
                return (ulong)(c - 'a') + 6;
            if (c >= '1' && c <= '5')
                return (ulong)(c - '1') + 1;
            return 0;
        }
    }
}