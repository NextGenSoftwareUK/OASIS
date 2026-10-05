using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 ChainLink OASIS Provider Test Harness");
            Console.WriteLine("=======================================");
            Console.WriteLine();

            try
            {
                // Initialize ChainLink provider
                var chainLinkProvider = new ChainLinkOASIS();
                
                Console.WriteLine("✅ ChainLink Provider initialized successfully");
                Console.WriteLine($"Provider Name: {chainLinkProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {chainLinkProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {chainLinkProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {chainLinkProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating ChainLink provider...");
                var activationResult = await chainLinkProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate ChainLink provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ ChainLink provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating ChainLink provider...");
                var deactivationResult = await chainLinkProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate ChainLink provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ ChainLink provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 ChainLink OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ChainLink Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
