using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.CeramicOASIS.TestHarness
{
    /// <summary>
    /// Console harness that walks the CRUD surface against a running Ceramic storage service.
    /// Requires CERAMICOASIS_SERVICE_URL, CERAMICOASIS_SERVICE_TOKEN and CERAMICOASIS_ENCRYPTION_KEY (64 hex chars).
    /// </summary>
    public static class CeramicOASISTestHarness
    {
        private static string Env(string name) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException(name + " is required.");

        public static async Task Main()
        {
            Console.WriteLine("CeramicOASIS Test Harness");
            Console.WriteLine(new string('-', 60));

            var provider = new CeramicOASIS(Env("CERAMICOASIS_SERVICE_URL"), Env("CERAMICOASIS_SERVICE_TOKEN"),
                Convert.FromHexString(Env("CERAMICOASIS_ENCRYPTION_KEY")));

            var activated = await provider.ActivateProviderAsync();
            Console.WriteLine(activated.IsError ? $"FAIL  activate: {activated.Message}" : "PASS  activate");
            if (activated.IsError)
                return;

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-harness-{Guid.NewGuid():N}",
                Email = $"oasis-harness-{Guid.NewGuid():N}@test.local"
            };

            var savedAvatar = await provider.SaveAvatarAsync(avatar);
            Console.WriteLine(savedAvatar.IsError ? $"FAIL  save avatar: {savedAvatar.Message}" : "PASS  save avatar");

            var loadedAvatar = await provider.LoadAvatarAsync(avatar.Id);
            Console.WriteLine(loadedAvatar.IsError ? $"FAIL  load avatar: {loadedAvatar.Message}" : "PASS  load avatar");

            var byUsername = await provider.LoadAvatarByUsernameAsync(avatar.Username);
            Console.WriteLine(byUsername.IsError ? $"FAIL  load by username: {byUsername.Message}" : "PASS  load by username");

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS Harness Holon {Guid.NewGuid():N}" };

            var savedHolon = await provider.SaveHolonAsync(holon);
            Console.WriteLine(savedHolon.IsError ? $"FAIL  save holon: {savedHolon.Message}" : "PASS  save holon");

            var loadedHolon = await provider.LoadHolonAsync(holon.Id);
            Console.WriteLine(loadedHolon.IsError ? $"FAIL  load holon: {loadedHolon.Message}" : "PASS  load holon");

            var deletedHolon = await provider.DeleteHolonAsync(holon.Id);
            Console.WriteLine(deletedHolon.IsError ? $"FAIL  delete holon: {deletedHolon.Message}" : "PASS  delete holon");

            var deletedAvatar = await provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Console.WriteLine(deletedAvatar.IsError ? $"FAIL  delete avatar: {deletedAvatar.Message}" : "PASS  delete avatar");

            var deactivated = await provider.DeActivateProviderAsync();
            Console.WriteLine(deactivated.IsError ? $"FAIL  deactivate: {deactivated.Message}" : "PASS  deactivate");

            Console.WriteLine(new string('-', 60));
            Console.WriteLine("Done.");
        }
    }
}
