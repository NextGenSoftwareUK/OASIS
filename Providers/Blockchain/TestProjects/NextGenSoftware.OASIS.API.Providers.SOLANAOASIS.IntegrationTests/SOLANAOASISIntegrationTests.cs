using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet.Utilities;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass]
public sealed class SOLANAOASISIntegrationTests
{
    private SolanaOASIS _provider = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // Refuse to reach a validator without the separately persisted private-storage key.
        var storageKey = PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_SOLANA_STORAGE_ENCRYPTION_KEY");
        CryptographicOperations.ZeroMemory(storageKey);
        string rpc = Environment.GetEnvironmentVariable("SOLANA_RPC_URL")
            ?? throw new InvalidOperationException("SOLANA_RPC_URL must identify a running Solana validator.");
        string privateKey = Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")
            ?? throw new InvalidOperationException("SOLANA_PRIVATE_KEY is required.");
        string publicKey = Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")
            ?? throw new InvalidOperationException("SOLANA_PUBLIC_KEY is required.");

        _provider = new SolanaOASIS(rpc, privateKey, publicKey);
        var activation = await _provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);
    }

    [TestMethod]
    public async Task MemoTransactionHolon_RoundTripsThroughRealValidator()
    {
        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            Name = $"Solana holon {Guid.NewGuid():N}",
            Description = "Agave validator integration",
            HolonType = HolonType.Holon
        };

        var save = await _provider.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsFalse(save.IsError, save.Message);
        Assert.IsNotNull(save.Result);
        Assert.IsTrue(save.Result.ProviderUniqueStorageKey.TryGetValue(ProviderType.SolanaOASIS, out string? signature));
        Assert.IsFalse(string.IsNullOrWhiteSpace(signature));

        var rpc = ClientFactory.GetClient(Environment.GetEnvironmentVariable("SOLANA_RPC_URL")!);
        var statuses = await rpc.GetSignatureStatusesAsync(new List<string> { signature! }, true);
        Assert.IsTrue(statuses.WasSuccessful);
        Assert.AreEqual(1, statuses.Result.Value.Count);
        Assert.IsNotNull(statuses.Result.Value[0]);
        Assert.IsNull(statuses.Result.Value[0].Error);
        Assert.AreEqual("finalized", statuses.Result.Value[0].ConfirmationStatus);
        var native = await rpc.GetTransactionAsync(signature!, Commitment.Finalized);
        Assert.IsTrue(native.WasSuccessful);
        Assert.IsNotNull(native.Result);
        var memo = Encoding.UTF8.GetString(Encoders.Base58.DecodeData(native.Result.Transaction.Message.Instructions[^1].Data));
        StringAssert.StartsWith(memo, "oasis-aes256gcm-gzip-v2:");
        Assert.IsFalse(memo.Contains(holon.Name));

        var load = await _provider.LoadHolonAsync(signature!, loadChildren: false);
        Assert.IsFalse(load.IsError, load.Message);
        Assert.AreEqual(holon.Id, load.Result.Id);
        Assert.AreEqual(holon.Name, load.Result.Name);

        holon.Name = "Updated synthetic Solana holon";
        holon.Description = "Updated payload must replace old DTO fields";
        var update = await _provider.SaveHolonAsync(holon, saveChildren:false);
        Assert.IsFalse(update.IsError, update.Message);
        var updatedSignature = holon.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];
        Assert.AreNotEqual(signature, updatedSignature);
        var updated = await _provider.LoadHolonAsync(updatedSignature,loadChildren:false);
        Assert.IsFalse(updated.IsError, updated.Message);
        Assert.AreEqual(holon.Name, updated.Result.Name);
        Assert.AreEqual(holon.Description, updated.Result.Description);
        Assert.AreEqual(load.Result.Version + 1, updated.Result.Version);
        Assert.AreEqual(holon.Id, updated.Result.Id);
        // Ledger history is immutable: an old signature must still identify the old version.
        var historical = await _provider.LoadHolonAsync(signature!,loadChildren:false);
        Assert.IsFalse(historical.IsError, historical.Message);
        Assert.AreEqual(load.Result.Name, historical.Result.Name);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_provider?.IsProviderActivated == true)
            await _provider.DeActivateProviderAsync();
    }
}
