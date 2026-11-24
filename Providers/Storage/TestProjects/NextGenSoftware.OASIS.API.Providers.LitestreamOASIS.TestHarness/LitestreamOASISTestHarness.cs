using System;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.LitestreamOASIS;

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.TestHarness
{
    public class LitestreamOASISTestHarness
    {
        private static LitestreamOASIS _provider = null!;

        public static async Task Main(string[] args)
        {
            var dbPath = Environment.GetEnvironmentVariable("LITESTREAM_DB_PATH") ?? (args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "oasis-harness.db"));
            Console.WriteLine($"LitestreamOASIS Test Harness — database: {dbPath}");
            _provider = new LitestreamOASIS(dbPath);

            await TestProviderActivation();
            await TestProviderInformation();
            await TestAvatarOperations();
            await TestHolonOperations();
            await TestProviderDeactivation();

            Console.WriteLine("\nAll tests complete. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task TestProviderActivation()
        {
            Console.WriteLine("\n--- Testing Provider Activation ---");
            var result = await _provider.ActivateProviderAsync();
            Console.WriteLine(result.IsError ? $"FAIL: {result.Message}" : $"PASS: {result.Message}");
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("\n--- Testing Provider Information ---");
            Console.WriteLine($"Provider Name: {_provider.ProviderName}");
            Console.WriteLine($"Provider Description: {_provider.ProviderDescription}");
            Console.WriteLine($"Provider Version: {_provider.GetProviderVersion()}");
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("\n--- Testing Avatar Operations ---");
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "litestream-harness-user", Email = "litestream-harness@test.com" };
            var saveResult = await _provider.SaveAvatarAsync(avatar);
            Console.WriteLine(saveResult.IsError ? $"FAIL Save: {saveResult.Message}" : $"PASS Save: {saveResult.Message}");
            var loadResult = await _provider.LoadAvatarAsync(avatar.Id);
            Console.WriteLine(loadResult.IsError ? $"FAIL Load: {loadResult.Message}" : $"PASS Load: {loadResult.Message}");
            var deleteResult = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Console.WriteLine(deleteResult.IsError ? $"FAIL Delete: {deleteResult.Message}" : $"PASS Delete: {deleteResult.Message}");
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("\n--- Testing Holon Operations ---");
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Litestream Harness Holon" };
            var saveResult = await _provider.SaveHolonAsync(holon);
            Console.WriteLine(saveResult.IsError ? $"FAIL Save: {saveResult.Message}" : $"PASS Save: {saveResult.Message}");
            var loadResult = await _provider.LoadHolonAsync(holon.Id);
            Console.WriteLine(loadResult.IsError ? $"FAIL Load: {loadResult.Message}" : $"PASS Load: {loadResult.Message}");
            var deleteResult = await _provider.DeleteHolonAsync(holon.Id, softDelete: true);
            Console.WriteLine(deleteResult.IsError ? $"FAIL Delete: {deleteResult.Message}" : $"PASS Delete: {deleteResult.Message}");
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("\n--- Testing Provider Deactivation ---");
            var result = await _provider.DeActivateProviderAsync();
            Console.WriteLine(result.IsError ? $"FAIL: {result.Message}" : $"PASS: {result.Message}");
        }
    }
}
