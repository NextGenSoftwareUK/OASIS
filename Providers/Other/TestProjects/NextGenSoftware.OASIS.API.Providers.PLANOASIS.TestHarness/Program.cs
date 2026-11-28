using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.PLANOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PLANOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 PLAN OASIS Provider Test Harness");
            Console.WriteLine("====================================");
            Console.WriteLine();

            try
            {
                // Initialize PLAN provider
                var planProvider = new global::NextGenSoftware.OASIS.API.Providers.PLANOASIS.PLANOASIS();
                
                Console.WriteLine("✅ PLAN Provider initialized successfully");
                Console.WriteLine($"Provider Name: {planProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {planProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {planProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {planProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating PLAN provider...");
                var activationResult = await planProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate PLAN provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ PLAN provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating PLAN provider...");
                var deactivationResult = await planProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate PLAN provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ PLAN provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 PLAN OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in PLAN Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
