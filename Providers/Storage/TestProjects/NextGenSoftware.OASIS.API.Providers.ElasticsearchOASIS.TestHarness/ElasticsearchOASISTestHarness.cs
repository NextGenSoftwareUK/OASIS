using NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.TestHarness
{
    public class ElasticsearchOASISTestHarness
    {
        private static ElasticsearchOASIS _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== ElasticsearchOASIS Test Harness ===");
            string host = args.Length > 0 ? args[0] : (Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST") ?? "http://localhost:9200");
            Console.WriteLine($"Host: {host}\n");

            _provider = new ElasticsearchOASIS(host);

            try
            {
                await TestProviderActivation();
                await TestProviderInformation();
                await TestAvatarOperations();
                await TestHolonOperations();
                await TestProviderDeactivation();
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

        private static async Task TestProviderActivation()
        {
            Console.WriteLine("--- Testing Provider Activation ---");
            Console.WriteLine($"Provider Name: {_provider.ProviderName}");
            Console.WriteLine($"Is Activated: {_provider.IsProviderActivated}");
            var result = _provider.ActivateProvider();
            Console.WriteLine($"Activation Result: {(result.IsError ? $"Failed - {result.Message}" : "Success")}");
            Console.WriteLine();
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("--- Testing Provider Information ---");
            Console.WriteLine($"Provider Version: {_provider.GetProviderVersion()}");
            Console.WriteLine($"Provider Type: {_provider.GetProviderType()}");
            Console.WriteLine($"Provider Category: {_provider.GetProviderCategory()}");
            Console.WriteLine($"Description: {_provider.ProviderDescription}");
            Console.WriteLine();
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("--- Testing Avatar Operations ---");
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "ElasticsearchOASISTestUser",
                Email = "elasticsearchoasistest@example.com",
                FirstName = "Elasticsearch",
                LastName = "Tester",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Saving Avatar: {avatar.Username}");
            var saveResult = await _provider.SaveAvatarAsync(avatar);
            Console.WriteLine($"Save Result: {(saveResult.IsError ? $"Failed - {saveResult.Message}" : "Success")}");

            Console.WriteLine($"Loading Avatar by ID: {avatar.Id}");
            var loadResult = await _provider.LoadAvatarAsync(avatar.Id);
            Console.WriteLine($"Load Result: {(loadResult.IsError ? $"Failed - {loadResult.Message}" : "Success")}");
            Console.WriteLine();
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("--- Testing Holon Operations ---");
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "ElasticsearchOASISTestHolon",
                Description = "Test Holon for ElasticsearchOASIS",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Saving Holon: {holon.Name}");
            var saveResult = await _provider.SaveHolonAsync(holon);
            Console.WriteLine($"Save Result: {(saveResult.IsError ? $"Failed - {saveResult.Message}" : "Success")}");

            Console.WriteLine($"Loading Holon by ID: {holon.Id}");
            var loadResult = await _provider.LoadHolonAsync(holon.Id);
            Console.WriteLine($"Load Result: {(loadResult.IsError ? $"Failed - {loadResult.Message}" : "Success")}");
            Console.WriteLine();
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("--- Testing Provider Deactivation ---");
            var result = _provider.DeActivateProvider();
            Console.WriteLine($"Deactivation Result: {(result.IsError ? $"Failed - {result.Message}" : "Success")}");
            Console.WriteLine($"Is Activated: {_provider.IsProviderActivated}");
            Console.WriteLine();
        }
    }
}
