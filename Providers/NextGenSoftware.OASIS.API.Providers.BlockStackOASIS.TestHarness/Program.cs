using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.TestHarness
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 BlockStack OASIS Provider Test Harness");
            Console.WriteLine("=========================================");
            Console.WriteLine();

            try
            {
                var provider = new BlockStackOASIS();

                Console.WriteLine($"Provider: {provider.ProviderName} - {provider.ProviderDescription}");
                Console.WriteLine($"Type: {provider.ProviderType} | Category: {provider.ProviderCategory}");

                var activate = await provider.ActivateProviderAsync();
                Console.WriteLine($"Activate -> Success: {!activate.IsError}, Message: {activate.Message}");

                // Demonstrate username lookup (will gracefully warn if not resolvable)
                var username = "example.id"; // change to a valid Blockstack name for real run
                var byUser = await provider.LoadAvatarByUsernameAsync(username);
                Console.WriteLine($"LoadAvatarByUsername('{username}') -> IsError: {byUser.IsError}, Message: {byUser.Message}");

                var deactivate = await provider.DeActivateProviderAsync();
                Console.WriteLine($"DeActivate -> Success: {!deactivate.IsError}, Message: {deactivate.Message}");

                Console.WriteLine("\n✅ Test Harness finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}\n{ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

