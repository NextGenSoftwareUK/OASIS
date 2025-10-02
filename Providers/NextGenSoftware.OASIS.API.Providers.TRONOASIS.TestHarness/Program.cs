using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 TRON OASIS Provider Test Harness");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            try
            {
                // Initialize TRON provider
                var tronProvider = new TRONOASIS();
                
                Console.WriteLine("✅ TRON Provider initialized successfully");
                Console.WriteLine($"Provider Name: {tronProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {tronProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {tronProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {tronProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating TRON provider...");
                var activationResult = await tronProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate TRON provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ TRON provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating TRON provider...");
                var deactivationResult = await tronProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate TRON provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ TRON provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 TRON OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in TRON Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
