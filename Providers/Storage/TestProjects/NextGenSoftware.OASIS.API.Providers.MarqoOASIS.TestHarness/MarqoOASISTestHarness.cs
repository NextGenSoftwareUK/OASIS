using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MarqoOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MarqoOASIS.TestHarness
{
    public class MarqoOASISTestHarness
    {
        private static MarqoOASIS _provider = null!;

        public static async Task Main(string[] args)
        {
            var connStr = Environment.GetEnvironmentVariable("MARQO_URL") ?? "http://localhost:8882";
            Console.WriteLine($"MarqoOASIS Test Harness - {connStr}");
            _provider = new MarqoOASIS(connStr);

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
            Console.WriteLine(result.IsError ? $"FAIL: {result.Message}" : "PASS: Provider activated.");
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("\n--- Testing Provider Information ---");
            Console.WriteLine($"Provider Name: {_provider.ProviderName}");
            Console.WriteLine($"Provider Version: {_provider.GetProviderVersion()}");
            await Task.CompletedTask;
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("\n--- Testing Avatar Operations ---");
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "marqo-harness-user", Email = "marqo-harness@test.com" };
            var saveResult = await _provider.SaveAvatarAsync(avatar);
            Console.WriteLine(saveResult.IsError ? $"FAIL Save: {saveResult.Message}" : "PASS Save");
            var loadResult = await _provider.LoadAvatarAsync(avatar.Id);
            Console.WriteLine(loadResult.IsError ? $"FAIL Load: {loadResult.Message}" : "PASS Load");
            var deleteResult = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Console.WriteLine(deleteResult.IsError ? $"FAIL Delete: {deleteResult.Message}" : "PASS Delete");
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("\n--- Testing Holon Operations ---");
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Marqo Harness Holon" };
            var saveResult = await _provider.SaveHolonAsync(holon);
            Console.WriteLine(saveResult.IsError ? $"FAIL Save: {saveResult.Message}" : "PASS Save");
            var loadResult = await _provider.LoadHolonAsync(holon.Id);
            Console.WriteLine(loadResult.IsError ? $"FAIL Load: {loadResult.Message}" : "PASS Load");
            var deleteResult = await _provider.DeleteHolonAsync(holon.Id, softDelete: true);
            Console.WriteLine(deleteResult.IsError ? $"FAIL Delete: {deleteResult.Message}" : "PASS Delete");
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("\n--- Testing Provider Deactivation ---");
            var result = await _provider.DeActivateProviderAsync();
            Console.WriteLine(result.IsError ? $"FAIL: {result.Message}" : "PASS: Provider deactivated.");
        }
    }
}
