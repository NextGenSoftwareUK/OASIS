using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Holo OASIS Provider Test Harness");
            Console.WriteLine("====================================");
            Console.WriteLine();

            try
            {
                // Initialize Holo provider
                var holoProvider = new HoloOASIS();
                
                Console.WriteLine("✅ Holo Provider initialized successfully");
                Console.WriteLine($"Provider Name: {holoProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {holoProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {holoProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {holoProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating Holo provider...");
                var activationResult = await holoProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate Holo provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Holo provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating Holo provider...");
                var deactivationResult = await holoProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate Holo provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Holo provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 Holo OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Holo Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}