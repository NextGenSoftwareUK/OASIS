using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.TypesenseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.TestHarness
{
    public class TypesenseOASISTestHarness
    {
        private static TypesenseOASIS _provider = null!;

        public static async Task Main(string[] args)
        {
            var host = Environment.GetEnvironmentVariable("TYPESENSE_HOST") ?? (args.Length > 0 ? args[0] : "http://localhost:8108");
            var apiKey = Environment.GetEnvironmentVariable("TYPESENSE_API_KEY") ?? (args.Length > 1 ? args[1] : "xyz");
            Console.WriteLine($"TypesenseOASIS Test Harness — host: {host}");
            _provider = new TypesenseOASIS(host, apiKey);

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
            Console.WriteLine($"Provider Version: {_provider.GetProviderVersion()}");
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("\n--- Testing Avatar Operations ---");
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "typesense-harness-user", Email = "typesense-harness@test.com" };
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
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Typesense Harness Holon" };
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
