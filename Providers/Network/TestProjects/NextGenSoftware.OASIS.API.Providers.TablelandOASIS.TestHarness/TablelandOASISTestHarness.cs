using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.TablelandOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TablelandOASIS.TestHarness
{
    /// <summary>
    /// Console harness that walks the CRUD surface against a real Tableland network.
    /// Required environment: TABLELAND_RPC_URL, TABLELAND_VALIDATOR_URL, TABLELAND_REGISTRY, TABLELAND_CHAIN_ID,
    /// TABLELAND_PRIVATE_KEY and TABLELAND_ENCRYPTION_KEY (base64, 32 bytes). Optional TABLELAND_TABLE names an existing
    /// table owned by the wallet; without it the harness creates a new one (a real registry transaction).
    /// </summary>
    public static class TablelandOASISTestHarness
    {
        private static string Env(string name)
            => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException($"{name} is required.");

        public static async Task Main()
        {
            Console.WriteLine("TablelandOASIS Test Harness");
            Console.WriteLine(new string('-', 60));

            var connection = new TablelandConnection(new Uri(Env("TABLELAND_RPC_URL")), new Uri(Env("TABLELAND_VALIDATOR_URL")),
                Env("TABLELAND_REGISTRY"), long.Parse(Env("TABLELAND_CHAIN_ID")), Env("TABLELAND_PRIVATE_KEY"));
            var table = Environment.GetEnvironmentVariable("TABLELAND_TABLE");
            if (string.IsNullOrEmpty(table))
            {
                table = await TablelandKeyValueBackend.CreateTableAsync(connection);
                Console.WriteLine($"PASS  create table {table}");
            }
            var provider = new TablelandOASIS(connection, table, Convert.FromBase64String(Env("TABLELAND_ENCRYPTION_KEY")));

            var activated = await provider.ActivateProviderAsync();
            Console.WriteLine(activated.IsError ? $"FAIL  activate: {activated.Message}" : $"PASS  activate ({provider.TableName}, wallet {provider.WalletAddress})");
            if (activated.IsError) return;

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
