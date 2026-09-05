using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.DruidOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DruidOASIS.TestHarness
{
    public class DruidOASISTestHarness
    {
        private static DruidOASIS _provider = null!;

        public static async Task Main(string[] args)
        {
            var connStr = Environment.GetEnvironmentVariable("DRUID_URL") ?? "http://localhost:8888";
            Console.WriteLine($"DruidOASIS Test Harness - {connStr}");
            _provider = new DruidOASIS(connStr);

            await TestProviderActivation();
            TestProviderInformation();
            await TestAvatarOperations();
            await TestHolonOperations();
            await TestProviderDeactivation();

            Console.WriteLine("\nAll tests complete. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task TestProviderActivation()
        {
            Console.WriteLine("\n--- Provider Activation ---");
            var r = await _provider.ActivateProviderAsync();
            Console.WriteLine(r.IsError ? $"FAIL: {r.Message}" : "PASS: activated");
        }

        private static void TestProviderInformation()
        {
            Console.WriteLine("\n--- Provider Information ---");
            Console.WriteLine($"Name:    {_provider.ProviderName}");
            Console.WriteLine($"Version: {_provider.GetProviderVersion()}");
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("\n--- Avatar Operations ---");
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "druid-harness", Email = "druid-harness@test.com" };
            var s = await _provider.SaveAvatarAsync(avatar);
            Console.WriteLine(s.IsError ? $"FAIL Save: {s.Message}" : "PASS Save");
            var l = await _provider.LoadAvatarAsync(avatar.Id);
            Console.WriteLine(l.IsError ? $"FAIL Load: {l.Message}" : "PASS Load");
            var d = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Console.WriteLine(d.IsError ? $"FAIL Delete: {d.Message}" : "PASS Delete");
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("\n--- Holon Operations ---");
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Druid Harness Holon" };
            var s = await _provider.SaveHolonAsync(holon);
            Console.WriteLine(s.IsError ? $"FAIL Save: {s.Message}" : "PASS Save");
            var l = await _provider.LoadHolonAsync(holon.Id);
            Console.WriteLine(l.IsError ? $"FAIL Load: {l.Message}" : "PASS Load");
            var d = await _provider.DeleteHolonAsync(holon.Id);
            Console.WriteLine(d.IsError ? $"FAIL Delete: {d.Message}" : "PASS Delete");
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("\n--- Provider Deactivation ---");
            var r = await _provider.DeActivateProviderAsync();
            Console.WriteLine(r.IsError ? $"FAIL: {r.Message}" : "PASS: deactivated");
        }
    }
}
