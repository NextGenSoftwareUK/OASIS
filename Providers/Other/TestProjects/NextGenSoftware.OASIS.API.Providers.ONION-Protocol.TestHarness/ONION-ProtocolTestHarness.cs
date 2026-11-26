using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol.TestHarness
{
    /// <summary>
    /// Console harness that walks avatar and holon CRUD through Tor against an onion-hosted OASIS storage service.
    /// Requires ONION_SERVICE_URL (http://...onion), ONION_SERVICE_TOKEN, and optionally ONION_SOCKS_HOST/ONION_SOCKS_PORT
    /// (default 127.0.0.1:9050). A fresh random record key is used, so records written by one run are unreadable later.
    /// </summary>
    public static class ONIONProtocolTestHarness
    {
        private static string Required(string name)
            => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException(name + " is required.");

        public static async Task<int> Main()
        {
            var provider = new OnionOASIS(Required("ONION_SERVICE_URL"), Required("ONION_SERVICE_TOKEN"), RandomNumberGenerator.GetBytes(32),
                Environment.GetEnvironmentVariable("ONION_SOCKS_HOST") ?? "127.0.0.1",
                int.Parse(Environment.GetEnvironmentVariable("ONION_SOCKS_PORT") ?? "9050"));
            var failures = 0;
            void Report(string step, bool isError, string message)
            {
                Console.WriteLine(isError ? $"FAIL  {step}: {message}" : $"PASS  {step}");
                if (isError) failures++;
            }

            var activated = await provider.ActivateProviderAsync();
            Report("activate", activated.IsError, activated.Message);
            if (activated.IsError) return 1;

            var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"onion-harness-{Guid.NewGuid():N}", Email = $"onion-harness-{Guid.NewGuid():N}@test.local" };
            var saved = await provider.SaveAvatarAsync(avatar);
            Report("save avatar", saved.IsError, saved.Message);
            var loaded = await provider.LoadAvatarByUsernameAsync(avatar.Username);
            Report("load avatar by username", loaded.IsError || loaded.Result?.Id != avatar.Id, loaded.Message);

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"Onion Harness Holon {Guid.NewGuid():N}" };
            var savedHolon = await provider.SaveHolonAsync(holon);
            Report("save holon", savedHolon.IsError, savedHolon.Message);
            var loadedHolon = await provider.LoadHolonAsync(holon.Id);
            Report("load holon", loadedHolon.IsError || loadedHolon.Result?.Name != holon.Name, loadedHolon.Message);
            var deletedHolon = await provider.DeleteHolonAsync(holon.Id);
            Report("delete holon", deletedHolon.IsError, deletedHolon.Message);
            var deletedAvatar = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Report("delete avatar", deletedAvatar.IsError, deletedAvatar.Message);

            await provider.DeActivateProviderAsync();
            Console.WriteLine(failures == 0 ? "All harness steps passed." : $"{failures} harness step(s) failed.");
            return failures == 0 ? 0 : 1;
        }
    }
}
