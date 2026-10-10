using NextGenSoftware.OASIS.API.Providers.QdrantOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS.TestHarness
{
    public class QdrantOASISTestHarness
    {
        private static QdrantOASIS _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== QdrantOASIS Test Harness ===");
            string host = args.Length > 0 ? args[0] : (Environment.GetEnvironmentVariable("QDRANT_HOST") ?? "http://localhost:6333");
            Console.WriteLine($"Host: {host}\n");

            _provider = new QdrantOASIS(host);

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
            var result = _provider.ActivateProvider();
            Console.WriteLine($"Activation Result: {(result.IsError ? $"Failed - {result.Message}" : "Success")}");
            Console.WriteLine();
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("--- Testing Provider Information ---");
            Console.WriteLine($"Version: {_provider.GetProviderVersion()}");
            Console.WriteLine($"Type: {_provider.GetProviderType()}");
            Console.WriteLine($"Category: {_provider.GetProviderCategory()}");
            Console.WriteLine();
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("--- Testing Avatar Operations ---");
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "QdrantOASISTestUser",
                Email = "qdrantoasistest@example.com",
                FirstName = "Qdrant",
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
                Name = "QdrantOASISTestHolon",
                Description = "Test Holon for QdrantOASIS",
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
            Console.WriteLine();
        }
    }
}
