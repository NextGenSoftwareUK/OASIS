using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.CelestiaOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.TestHarness
{
    /// <summary>
    /// Console harness that walks the CRUD surface against a celestia-node, e.g. the devnet from
    /// IntegrationTests/fixture/start-celestia.sh. Configure with OASIS_CELESTIA_URL, OASIS_CELESTIA_TOKEN and optionally
    /// OASIS_CELESTIA_NAMESPACE (20 hex chars), OASIS_CELESTIA_START_HEIGHT and OASIS_CELESTIA_KEY (64 hex chars; a random
    /// key is used otherwise, so records are only readable by this run). Writes pay fees from the node's account.
    /// </summary>
    public static class CelestiaOASISTestHarness
    {
        public static async Task<int> Main()
        {
            Console.WriteLine("CelestiaOASIS Test Harness");
            Console.WriteLine(new string('-', 60));

            var url = Environment.GetEnvironmentVariable("OASIS_CELESTIA_URL");
            var token = Environment.GetEnvironmentVariable("OASIS_CELESTIA_TOKEN");
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Set OASIS_CELESTIA_URL and OASIS_CELESTIA_TOKEN (see fixture/start-celestia.sh) and try again.");
                return 2;
            }
            var ns = Environment.GetEnvironmentVariable("OASIS_CELESTIA_NAMESPACE") ?? "6f617369732d6861726e"; // "oasis-harn"
            var start = ulong.Parse(Environment.GetEnvironmentVariable("OASIS_CELESTIA_START_HEIGHT") ?? "1");
            var keyHex = Environment.GetEnvironmentVariable("OASIS_CELESTIA_KEY");
            var key = keyHex == null ? RandomNumberGenerator.GetBytes(32) : Convert.FromHexString(keyHex);

            using var provider = new CelestiaOASIS(url, token, ns, start, key);
            var failures = 0;
            void Report(string step, bool isError, string? message)
            {
                if (isError) failures++;
                Console.WriteLine(isError ? $"FAIL  {step}: {message}" : $"PASS  {step}");
            }

            var activated = await provider.ActivateProviderAsync();
            Report("activate", activated.IsError, activated.Message);
            if (activated.IsError) return 1;

            var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"oasis-harness-{Guid.NewGuid():N}", Email = $"oasis-harness-{Guid.NewGuid():N}@test.local" };
            var savedAvatar = await provider.SaveAvatarAsync(avatar);
            Report("save avatar", savedAvatar.IsError, savedAvatar.Message);
            var loadedAvatar = await provider.LoadAvatarAsync(avatar.Id);
            Report("load avatar", loadedAvatar.IsError || loadedAvatar.Result?.Id != avatar.Id, loadedAvatar.Message);
            var byUsername = await provider.LoadAvatarByUsernameAsync(avatar.Username);
            Report("load by username", byUsername.IsError || byUsername.Result?.Id != avatar.Id, byUsername.Message);

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS Harness Holon {Guid.NewGuid():N}" };
            var savedHolon = await provider.SaveHolonAsync(holon);
            Report("save holon", savedHolon.IsError, savedHolon.Message);
            var loadedHolon = await provider.LoadHolonAsync(holon.Id);
            Report("load holon", loadedHolon.IsError || loadedHolon.Result?.Name != holon.Name, loadedHolon.Message);
            var deletedHolon = await provider.DeleteHolonAsync(holon.Id);
            Report("delete holon", deletedHolon.IsError, deletedHolon.Message);
            var deletedAvatar = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Report("delete avatar", deletedAvatar.IsError, deletedAvatar.Message);

            var deactivated = await provider.DeActivateProviderAsync();
            Report("deactivate", deactivated.IsError, deactivated.Message);

            Console.WriteLine(new string('-', 60));
            Console.WriteLine(failures == 0 ? "Done." : $"{failures} step(s) failed.");
            return failures == 0 ? 0 : 1;
        }
    }
}
