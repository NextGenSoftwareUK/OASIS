using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 SEEDS OASIS Provider Test Harness");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            try
            {
                // Initialize SEEDS provider
                var seedsProvider = new SEEDSOASIS();
                
                Console.WriteLine("✅ SEEDS Provider initialized successfully");
                Console.WriteLine($"Provider Name: {seedsProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {seedsProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {seedsProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {seedsProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating SEEDS provider...");
                var activationResult = await seedsProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate SEEDS provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ SEEDS provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating SEEDS provider...");
                var deactivationResult = await seedsProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate SEEDS provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ SEEDS provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 SEEDS OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SEEDS Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}