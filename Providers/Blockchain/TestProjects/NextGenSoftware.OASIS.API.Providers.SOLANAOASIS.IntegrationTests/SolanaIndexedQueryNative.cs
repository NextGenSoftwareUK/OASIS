using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public partial class SolanaIndexedQueryNative
{
    // Persist this acknowledgement outside disposable TestResults. A lost outcome
    // must be reconciled from the original intent, never replaced with a new seed.
    public sealed record Seed(Guid Id, Guid VersionId, Guid Actor, Guid IndexId,
        string Writer, string Genesis, long CoverageStart, string StartBlock,
        string? Signature, string Phase);

    [TestMethod, TestCategory("SolanaIndexedQueryNative")]
    public async Task DeclaredRangePublishesDurableIndexAndExecutesManagerLinqRows()
    {
        const string endpoint = "http://127.0.0.1:18899";
        Assert.AreEqual(endpoint, Environment.GetEnvironmentVariable("SOLANA_RPC_URL"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState", "COSMIC", "SolanaCqlCurrent");
        Directory.CreateDirectory(state);
        var httpMode = Environment.GetEnvironmentVariable("OASIS_SOLANA_INDEXED_HTTP") == "1";
        var pending = Path.Combine(state, "seed.pending.json");
        var complete = Path.Combine(state, "seed.acknowledged.json");
        if (httpMode) Assert.IsTrue(File.Exists(complete), "Run the explicit SDK seed gate first; HTTP never creates a seed.");
        Assert.IsFalse(File.Exists(pending), "Reconcile the original seed outcome; do not submit another seed.");
        Assert.IsFalse(File.Exists(complete + ".next"), "Preserve an interrupted acknowledgement.");
        var secret = Encoders.Base58.DecodeData(Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!);
        var key = Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_SOLANA_STORAGE_ENCRYPTION_KEY")!);
        var rpc = ClientFactory.GetClient(endpoint);
        try
        {
            var account = new Account(secret, Encoders.Base58.DecodeData(Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!));
            var genesis = await rpc.GetGenesisHashAsync();
            Assert.IsTrue(genesis.WasSuccessful);
            Assert.AreEqual("F22uw23o1HuahQiQVfd7Lfi6Q51fv4KNoYAvDnDDpEvj", genesis.Result);
            var repository = new SolanaRepository(account, rpc, () => (byte[])key.Clone());
            var submitted = false;
            Seed seed;

            if (File.Exists(complete))
            {
                seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(complete))!;
                Assert.AreEqual("acknowledged", seed.Phase);
                Assert.IsFalse(string.IsNullOrEmpty(seed.Signature));
                Assert.AreEqual(account.PublicKey.Key, seed.Writer);
                Assert.AreEqual(genesis.Result, seed.Genesis);
            }
            else
            {
                var slot = await rpc.GetSlotAsync(Commitment.Finalized);
                Assert.IsTrue(slot.WasSuccessful);
                var block = await rpc.GetBlockAsync(slot.Result, Commitment.Finalized);
                Assert.IsTrue(block.WasSuccessful);
                Assert.IsNotNull(block.Result);
                seed = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), account.PublicKey.Key,
                    genesis.Result, checked((long)slot.Result), block.Result.Blockhash, null, "before-submission");
                await WriteNewAsync(pending, seed);

                // The only source mutation in this demo. No retry, funding, reset,
                // historical replay or fabricated checkpoint is allowed.
                try
                {
                    var signature = await repository.CreateAsync(new SolanaHolonDto {
                        Id = seed.Id, Version = 1, VersionId = seed.VersionId, CreatedByAvatarId = seed.Actor,
                        Name = "COSMIC native chain coffee", Description = "Explicit declared-range CQL demo",
                        HolonType = HolonType.Holon, IsActive = true });
                    seed = seed with { Signature = signature, Phase = "acknowledged" };
                    await WriteNewAsync(complete + ".next", seed);
                    File.Move(complete + ".next", complete);
                    File.Delete(pending); // Durable acknowledgement retains the original public intent.
                    submitted = true;
                }
                catch (Exception cause)
                {
                    // Preserve exact reconciliation identity without exposing secrets.
                    if (cause is INativeMutationReconciliationRequired unknown)
                        await WriteNewAsync(Path.Combine(state, "seed.uncertain.json"),
                            seed with { Signature = unknown.OriginalTransactionId, Phase = "uncertain" });
                    throw;
                }
            }

            var source = new CosmicChainQuerySource(ProviderType.SolanaOASIS, seed.Genesis,
                ProviderType.SQLLiteDBOASIS, seed.IndexId, 2,
                SolanaRepository.ChainHolonCatalogueHash(seed.Writer, seed.Genesis), seed.CoverageStart);
            var requirement = new CosmicChainQueryReadRequirement(source, new(seed.CoverageStart, seed.StartBlock),
                500, TimeSpan.FromMinutes(1));
            var database = Path.Combine(state, "index.db");
            CosmicChainQueryPosition? previous = null;
            using (var index = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true))
            {
                if (File.Exists(database))
                {
                    var prior = index.ReadSnapshot();
                    Assert.IsFalse(prior.IsError, prior.Exception?.ToString());
                    previous = prior.Result.Checkpoint.Position;
                }
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var published = await CosmicChainQueryIngestion.PublishAsync(repository, index, requirement, previous, timeout.Token);
                Assert.IsFalse(published.IsError, published.Message + published.Exception);
                Assert.IsTrue(published.IsSaved);
            }

            // Reopen the actual durable index before query acceptance.
            using var reopened = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
            var demo = await CosmicCqlDemo.ChainIndexedWalkthrough.RunAsync(manager, reopened, repository,
                requirement, CosmicQueryReadAccess.OwnerOrPublic(seed.Actor).Result, seed.Id, Console.Out);
            var rows = demo.Rows;
            Assert.AreEqual(1, rows.Rows.Count);
            var row = rows.Rows.Single();
            Assert.AreEqual(seed.Id, row.Id);
            Assert.AreEqual(seed.VersionId, row.VersionId);
            Assert.AreEqual("COSMIC native chain coffee", row.Name);
            Assert.IsFalse(HolonChangeTracking.HasChanges(row));
            Assert.AreEqual(seed.Signature, rows.Receipts.Single().CommitReference);
            Assert.AreEqual(seed.CoverageStart, rows.Checkpoint.CompleteFromSlot);
            Assert.IsTrue(rows.Checkpoint.Position.Slot >= rows.Receipts.Single().Position.Slot);
            Assert.AreEqual(0, demo.Empty.Rows.Count);

            var projected = await CosmicCqlDemo.ChainIndexedWalkthrough.RunProjectionAsync(manager, reopened, repository,
                requirement, CosmicQueryReadAccess.OwnerOrPublic(seed.Actor).Result, seed.Id, Console.Out);
            Assert.AreEqual(seed.Id, projected.Rows.Single().Id);
            Assert.AreEqual("COSMIC native chain coffee", projected.Rows.Single().Name);
            Assert.AreEqual(1, projected.Rows.Single().Version);
            Assert.AreEqual(seed.Signature, projected.Receipts.Single().CommitReference);
            Assert.AreEqual(seed.CoverageStart, projected.Checkpoint.CompleteFromSlot);

            object? http = httpMode ? await VerifyNativeHttpAsync(manager, reopened, repository, source, requirement, seed) : null;
            var output = Path.Combine(Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE")!, httpMode ? "solana-indexed-http-native.json" : "solana-indexed-native.json");
            await WriteNewAsync(output, new {
                scope = "Native Solana declared-range SQLite index and manager/LINQ SDK; not original-history recovery or production authentication/durable billing",
                sourceProvider = source.SourceProvider.ToString(), executionProvider = source.ExecutionProvider.ToString(),
                seed.Writer, seed.Genesis, seed.Id, seed.IndexId, seed.CoverageStart, seed.Signature,
                http, projectionRows = projected.Rows.Count, submittedNewSeed = submitted, originalRecordRecoveryVerified = false,
                checkpoint = rows.Checkpoint.Position.Slot, finalizedHead = rows.Head.Position.Slot,
                rows = rows.Rows.Count, emptyRows = demo.Empty.Rows.Count,
                indexBytes = new FileInfo(database).Length, indexPath = database, seedPath = complete });
        }
        finally { CryptographicOperations.ZeroMemory(secret); CryptographicOperations.ZeroMemory(key); }
    }

    private static async Task WriteNewAsync<T>(string path, T value)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(file, value);
        file.Flush(true);
    }
}
