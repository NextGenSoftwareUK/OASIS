using NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.TestHarness
{
    public class InfluxDBOASISTestHarness
    {
        private static InfluxDBOASIS _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== InfluxDBOASIS Test Harness ===");
            string url = Environment.GetEnvironmentVariable("INFLUXDB_URL") ?? "http://localhost:8086";
            string token = Environment.GetEnvironmentVariable("INFLUXDB_TOKEN") ?? "test-token";
            string org = Environment.GetEnvironmentVariable("INFLUXDB_ORG") ?? "oasis-org";
            string bucket = Environment.GetEnvironmentVariable("INFLUXDB_BUCKET") ?? "oasis-bucket";
            Console.WriteLine($"URL: {url}  Org: {org}  Bucket: {bucket}\n");

            _provider = new InfluxDBOASIS(url, token, org, bucket);

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
            Console.WriteLine($"Description: {_provider.ProviderDescription}");
            Console.WriteLine();
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("--- Testing Avatar Operations ---");
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "InfluxDBOASISTestUser",
                Email = "influxdboasistest@example.com",
                FirstName = "InfluxDB",
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
                Name = "InfluxDBOASISTestHolon",
                Description = "Test Holon for InfluxDBOASIS",
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
