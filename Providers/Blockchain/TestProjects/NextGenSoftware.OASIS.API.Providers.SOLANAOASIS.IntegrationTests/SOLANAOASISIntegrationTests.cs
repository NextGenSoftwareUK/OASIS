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
[DoNotParallelize]
public sealed partial class SOLANAOASISIntegrationTests
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
        Assert.IsFalse(save.IsError, $"{save.Message} {save.Exception}");
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
        var deletion = await _provider.DeleteHolonAsync(updatedSignature);
        Assert.IsFalse(deletion.IsError, deletion.Message);
        Assert.IsTrue(deletion.IsDeleted);
        Assert.IsNotNull(deletion.Result);
        var tombstoneSignature = deletion.Result.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];
        Assert.AreNotEqual(updatedSignature, tombstoneSignature);
        var tombstone = await _provider.LoadHolonAsync(tombstoneSignature,loadChildren:false);
        Assert.IsFalse(tombstone.IsError,tombstone.Message);
        Assert.AreEqual(holon.Id,tombstone.Result.Id);
        Assert.AreEqual(holon.Name,tombstone.Result.Name);
        Assert.AreEqual(holon.Description,tombstone.Result.Description);
        Assert.AreEqual(updated.Result.Version + 1,tombstone.Result.Version);
        Assert.IsFalse(tombstone.Result.IsActive);
        var preDeletion = await _provider.LoadHolonAsync(updatedSignature,loadChildren:false);
        Assert.IsFalse(preDeletion.IsError,preDeletion.Message);
        Assert.AreEqual(updated.Result.Version,preDeletion.Result.Version);
        await VerifyConcurrentSdkAsync(rpc);
    }

    private static async Task VerifyConcurrentSdkAsync(IRpcClient rpc)
    {
        var evidence=Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE") ?? throw new InvalidOperationException("Native evidence directory required.");
        Assert.IsTrue(Path.IsPathFullyQualified(evidence));Assert.IsTrue(Directory.Exists(evidence));
        var journal=Path.Combine(evidence,"sdk-concurrent.pending.json");Assert.IsFalse(File.Exists(journal),"Inspect original submissions before any new broadcast.");
        var signer=new Solnet.Wallet.Account(Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!,Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!);
        var block=await rpc.GetLatestBlockHashAsync();Assert.IsTrue(block.WasSuccessful);
        var markers=new[]{"owned-concurrent-first-"+Guid.NewGuid().ToString("N"),"owned-concurrent-second-"+Guid.NewGuid().ToString("N")};
        var transactions=markers.Select(marker=>new Solnet.Rpc.Builders.TransactionBuilder().SetRecentBlockHash(block.Result.Value.Blockhash)
            .SetFeePayer(signer).AddInstruction(Solnet.Programs.MemoProgram.NewMemoV2(marker)).Build(signer)).ToArray();
        foreach(var bytes in transactions)Assert.AreEqual((byte)1,bytes[0],"Exactly one native signer is required.");
        var signatures=transactions.Select(bytes=>Encoders.Base58.EncodeData(bytes.AsSpan(1,64).ToArray())).ToArray();
        Assert.AreNotEqual(signatures[0],signatures[1]);
        var intent=System.Text.Json.JsonSerializer.Serialize(new{Genesis="F22uw23o1HuahQiQVfd7Lfi6Q51fv4KNoYAvDnDDpEvj",Signatures=signatures,
            SignedPayloadSha256=transactions.Select(bytes=>Convert.ToHexString(SHA256.HashData(bytes))).ToArray()});
        File.WriteAllText(journal,intent);
        var wrapped=((IRpcClient Client,NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana.FinalizedSolanaRpc State))
            typeof(NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana.FinalizedSolanaRpc)
                .GetMethod("Wrap",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.Invoke(null,new object[]{rpc,signer})!;
        var first=wrapped.Client.SendTransactionAsync(transactions[0]);
        var second=wrapped.Client.SendTransactionAsync(transactions[1]);
        var results=await Task.WhenAll(first,second);
        for(var index=0;index<2;index++){
            Assert.IsTrue(results[index].WasSuccessful);Assert.AreEqual(signatures[index],results[index].Result);
            var native=await rpc.GetTransactionAsync(signatures[index],Commitment.Finalized);Assert.IsTrue(native.WasSuccessful);Assert.IsNotNull(native.Result);
            Assert.AreEqual(markers[index],Encoding.UTF8.GetString(Encoders.Base58.DecodeData(native.Result.Transaction.Message.Instructions.Single().Data)));
        }
        CollectionAssert.AreEqual(signatures,wrapped.State.FinalizedSignatures.ToArray());Assert.IsNull(wrapped.State.SubmissionFailure);
        Assert.AreEqual(intent,File.ReadAllText(journal));File.Delete(journal);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_provider?.IsProviderActivated == true)
            await _provider.DeActivateProviderAsync();
    }
}
