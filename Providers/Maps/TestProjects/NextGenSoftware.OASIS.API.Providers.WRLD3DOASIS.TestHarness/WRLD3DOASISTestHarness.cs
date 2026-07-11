using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS;

namespace NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS.TestHarness
{
    public class WRLD3DOASISTestHarness
    {
        private static WRDLD3DOASIS _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== WRLD3DOASIS Test Harness ===");
            Console.WriteLine("Testing WRLD3DOASIS map provider...\n");

            _provider = new WRDLD3DOASIS();

            try
            {
                await TestProviderInformation();

                Console.WriteLine("\n=== All Tests Completed Successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Test Failed: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static Task TestProviderInformation()
        {
            Console.WriteLine("--- Map Provider Information ---");

            Console.WriteLine($"Map Provider Type: {_provider.MapProviderType}");
            Console.WriteLine($"Map Provider Name: {_provider.MapProviderName}");
            Console.WriteLine($"Map Provider Description: {_provider.MapProviderDescription}");

            if (_provider.MapProviderType != MapProviderType.WRLD3D)
                throw new Exception($"Expected MapProviderType.WRLD3D, got {_provider.MapProviderType}");

            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}
