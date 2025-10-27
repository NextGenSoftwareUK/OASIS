using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 ThreeFold OASIS Provider Test Harness");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // Initialize ThreeFold provider
                var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");
                
                Console.WriteLine("✅ ThreeFold Provider initialized successfully");
                Console.WriteLine($"Provider Name: {threeFoldProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {threeFoldProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {threeFoldProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {threeFoldProvider.ProviderCategory}");
                Console.WriteLine($"Host URI: {threeFoldProvider.HostUri}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating ThreeFold provider...");
                var activationResult = await threeFoldProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate ThreeFold provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ ThreeFold provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating ThreeFold provider...");
                var deactivationResult = await threeFoldProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate ThreeFold provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ ThreeFold provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 ThreeFold OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ThreeFold Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
