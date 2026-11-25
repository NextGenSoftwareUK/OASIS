using System;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS.TestHarness
{
    /// <summary>
    /// Console harness that walks the CRUD surface against an SVM endpoint.
    /// ECLIPSEOASIS_RPC_URL is the endpoint; ECLIPSEOASIS_KEYPAIR is a solana-keygen JSON keypair file
    /// for a funded signer. Use a local solana-test-validator or a test network account only.
    /// </summary>
    public static class EclipseOASISTestHarness
    {
        public static async Task Main()
        {
            Console.WriteLine("EclipseOASIS Test Harness");
            Console.WriteLine(new string('-', 60));

            var rpcUrl = Environment.GetEnvironmentVariable("ECLIPSEOASIS_RPC_URL");
            var keypairPath = Environment.GetEnvironmentVariable("ECLIPSEOASIS_KEYPAIR");
            if (string.IsNullOrWhiteSpace(rpcUrl) || string.IsNullOrWhiteSpace(keypairPath))
            {
                Console.WriteLine("Set ECLIPSEOASIS_RPC_URL and ECLIPSEOASIS_KEYPAIR and try again.");
                return;
            }

            var secret = Array.ConvertAll(JsonSerializer.Deserialize<int[]>(await System.IO.File.ReadAllTextAsync(keypairPath))
                ?? throw new InvalidOperationException("The keypair file is empty."), value => checked((byte)value));
            if (secret.Length != 64)
                throw new InvalidOperationException("The keypair file must hold a 64-byte keypair.");
            var provider = new EclipseOASIS(rpcUrl, Encoders.Base58.EncodeData(secret), Encoders.Base58.EncodeData(secret[32..]));
            Array.Clear(secret);

            var activated = await provider.ActivateProviderAsync();
            Console.WriteLine(activated.IsError ? $"FAIL  activate: {activated.Message}" : "PASS  activate");
            if (activated.IsError)
                return;

            Console.WriteLine($"      name    : {provider.ProviderName}");

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

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS Harness Holon {Guid.NewGuid():N}", CreatedByAvatarId = avatar.Id };

            var savedHolon = await provider.SaveHolonAsync(holon);
            Console.WriteLine(savedHolon.IsError ? $"FAIL  save holon: {savedHolon.Message}" : "PASS  save holon");

            var loadedHolon = await provider.LoadHolonAsync(holon.Id);
            Console.WriteLine(loadedHolon.IsError ? $"FAIL  load holon: {loadedHolon.Message}" : "PASS  load holon");

            var deletedHolon = await provider.DeleteHolonAsync(holon.Id);
            Console.WriteLine(deletedHolon.IsError ? $"FAIL  delete holon: {deletedHolon.Message}" : "PASS  delete holon");

            var deletedAvatar = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Console.WriteLine(deletedAvatar.IsError ? $"FAIL  delete avatar: {deletedAvatar.Message}" : "PASS  delete avatar");

            var deactivated = await provider.DeActivateProviderAsync();
            Console.WriteLine(deactivated.IsError ? $"FAIL  deactivate: {deactivated.Message}" : "PASS  deactivate");

            Console.WriteLine(new string('-', 60));
            Console.WriteLine("Done.");
        }
    }
}
