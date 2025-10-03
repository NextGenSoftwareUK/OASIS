using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Elrond OASIS Provider Test Harness");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            try
            {
                // Initialize Elrond provider
                var elrondProvider = new ElrondOASIS();
                
                Console.WriteLine("✅ Elrond Provider initialized successfully");
                Console.WriteLine($"Provider Name: {elrondProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {elrondProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {elrondProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {elrondProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating Elrond provider...");
                var activationResult = await elrondProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate Elrond provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Elrond provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating Elrond provider...");
                var deactivationResult = await elrondProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate Elrond provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Elrond provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("🎉 Elrond OASIS Provider Test Harness completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Elrond Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
