using System;
using System.Text;
using Solnet.Programs.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var publicKey = "GsguXojeGATpZGW8VNfW8qQCBVodbW2qGS8bUEbdGZfv";
            
            byte[] bytes = Encoding.ASCII.GetBytes(publicKey);
            ReadOnlySpan<byte> binData = new(bytes);
            Console.WriteLine(binData.Length);
            Console.WriteLine(binData.IsEmpty);

            Console.WriteLine(binData.GetU32(1));
            Console.WriteLine(binData.GetS32(3));
        }
    }
}