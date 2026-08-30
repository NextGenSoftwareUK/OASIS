using NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.TestHarness
{
    public class CouchbaseOASISTestHarness
    {
        private static CouchbaseOASIS _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== CouchbaseOASIS Test Harness ===");
            string conn = Environment.GetEnvironmentVariable("COUCHBASE_CONN") ?? "couchbase://localhost";
            string user = Environment.GetEnvironmentVariable("COUCHBASE_USER") ?? "Administrator";
            string pass = Environment.GetEnvironmentVariable("COUCHBASE_PASS") ?? "password";
            Console.WriteLine($"Connection: {conn}  User: {user}\n");

            _provider = new CouchbaseOASIS(conn, user, pass);

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
                Username = "CouchbaseOASISTestUser",
                Email = "couchbaseoasistest@example.com",
                FirstName = "Couchbase",
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
                Name = "CouchbaseOASISTestHolon",
                Description = "Test Holon for CouchbaseOASIS",
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
