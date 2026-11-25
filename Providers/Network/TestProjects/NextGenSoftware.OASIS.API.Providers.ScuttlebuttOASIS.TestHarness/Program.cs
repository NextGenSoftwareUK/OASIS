using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Scuttlebutt OASIS Provider Test Harness");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            try
            {
                // Initialize Scuttlebutt provider
                // Requires a running StorageService/storage-service.cjs; see the provider README.
                var scuttlebuttProvider = new ScuttlebuttOASIS(
                    Environment.GetEnvironmentVariable("OASIS_SSB_SERVICE") ?? "http://127.0.0.1:18904",
                    Environment.GetEnvironmentVariable("OASIS_SSB_STORAGE_TOKEN") ?? throw new InvalidOperationException("Set OASIS_SSB_STORAGE_TOKEN."),
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
                
                Console.WriteLine("✅ Scuttlebutt Provider initialized successfully");
                Console.WriteLine($"Provider Name: {scuttlebuttProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {scuttlebuttProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {scuttlebuttProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {scuttlebuttProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating Scuttlebutt provider...");
                var activationResult = await scuttlebuttProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate Scuttlebutt provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Scuttlebutt provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating Scuttlebutt provider...");
                var deactivationResult = await scuttlebuttProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate Scuttlebutt provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Scuttlebutt provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 Scuttlebutt OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Scuttlebutt Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
