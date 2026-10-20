using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    private sealed class PreparationKeys : IHolonMetadataKeyResolver
    {
        internal bool DenyOld;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            if (DenyOld && request.KeyId == "key-a" && request.Purpose == HolonMetadataKeyPurpose.DecryptPreparation)
                return new() { IsError = true, ErrorCode = "OLD_PREPARATION_DENIED" };
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId == "key-a" ? (byte)17 : (byte)34, 32).ToArray()));
        }
    }
    private static HolonMetadataEncryptionPolicy PreparationPolicy(PreparationKeys resolver, string key = "key-a", string context = "owned-mongo-root")
        => new(context, key, resolver, new[] { "search-tag" });
    private static HolonManager PreparationManager(Provider provider, PreparationKeys keys, string key = "key-a")
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2,
            StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        var registry = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        return new HolonManager(provider, dna, registry) { MetadataEncryptionPolicy = PreparationPolicy(keys, key) };
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativePreparationRecoversEncryptedGraphsAndBindsReceiptCutover(bool synchronous)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string name = "cosmic_conditional_fixture"; var client = new MongoClient(uri);
        using var names = await client.ListDatabaseNamesAsync(); Assert.DoesNotContain(name, await names.ToListAsync());
        var database = client.GetDatabase(name); await database.CreateCollectionAsync("CosmicOwnedFixture");
        var first = new Provider(uri, name); var second = new Provider(uri, name); var complete = false;
        var actor = Guid.NewGuid(); var op = Guid.NewGuid(); const decimal exact = 1.000000000000000000000000001m;
        Holon Input() => new() { Name = "owned encrypted preparation root", MetaData = new() { ["private-note"] = exact, ["search-tag"] = "coffee" },
            Children = new List<IHolon> { new Holon { Name = "owned encrypted preparation child",
                MetadataEncryptionOverride = PreparationPolicy(new PreparationKeys(), "key-b", "owned-mongo-child"), MetaData = new() { ["private-note"] = "child secret" } } } };
        Task<OASISResult<IHolon>> Save(HolonManager manager, Holon value, Guid? by = null) => synchronous
            ? Task.Run(() => manager.SaveHolon(value, by ?? actor, new CosmicWriteOptions(op), providerType: ProviderType.MongoDBOASIS))
            : manager.SaveHolonAsync(value, by ?? actor, new CosmicWriteOptions(op), providerType: ProviderType.MongoDBOASIS);
        var rows = database.GetCollection<BsonDocument>("Holon"); var preparations = database.GetCollection<BsonDocument>("CosmicWritePreparation");
        var receipts = database.GetCollection<BsonDocument>("CosmicWriteReceipt"); var fences = database.GetCollection<BsonDocument>("CosmicOperationFence");
        var lookup = new BsonDocument("_id", op.ToString("D"));
        async Task<byte[][]> Bodies() => (await rows.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("HolonId", 1)).ToListAsync()).Select(row => row.ToBson()).ToArray();
        try
        {
            Assert.False((await first.ActivateProviderAsync()).IsError); Assert.False((await second.ActivateProviderAsync()).IsError);
            var original = Input(); var created = await Save(PreparationManager(first, new PreparationKeys()), original);
            Assert.False(created.IsError, created.Message + created.Exception);
            var revision = CosmicConcurrency.GetProviderRevisionId(original, ProviderType.MongoDBOASIS); var version = original.VersionId;
            var childRevision = CosmicConcurrency.GetProviderRevisionId(original.Children.Single(), ProviderType.MongoDBOASIS);
            var originalBodies = await Bodies(); var journal = (await preparations.Find(lookup).SingleAsync()).ToBson();
            foreach (var native in await rows.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
            { Assert.DoesNotContain("child secret", native.ToJson()); Assert.DoesNotContain(exact.ToString(System.Globalization.CultureInfo.InvariantCulture), native.ToJson()); }
            var raw = await first.LoadHolonAsync(original.Id, loadChildren: false);
            Assert.False(raw.IsError, raw.Message + raw.Exception); Assert.Equal("coffee", raw.Result.MetaData["search-tag"].ToString());
            Assert.False(raw.Result.MetaData.ContainsKey("private-note")); Assert.StartsWith("oasis2:", raw.Result.MetaData["__oasis_enc__"].ToString());
            var receipt = (await receipts.Find(lookup).SingleAsync()).ToBson(); var fence = (await fences.Find(lookup).SingleAsync()).ToBson();
            Assert.DoesNotContain("child secret", (await preparations.Find(lookup).SingleAsync()).ToJson());
            var fresh = Input(); var replay = await Save(PreparationManager(second, new PreparationKeys(), "key-b"), fresh);
            Assert.False(replay.IsError, replay.Message + replay.Exception); Assert.Equal(original.Id, fresh.Id); Assert.Equal(version, fresh.VersionId);
            Assert.Equal(revision, CosmicConcurrency.GetProviderRevisionId(fresh, ProviderType.MongoDBOASIS));
            Assert.Equal(childRevision, CosmicConcurrency.GetProviderRevisionId(fresh.Children.Single(), ProviderType.MongoDBOASIS));
            Assert.Contains(exact.ToString(System.Globalization.CultureInfo.InvariantCulture), StoredRecordJson.Serialize(fresh.MetaData)); Assert.False(fresh.HasHolonChanged());
            Assert.Equal(journal, (await preparations.Find(lookup).SingleAsync()).ToBson()); Assert.Equal(receipt, (await receipts.Find(lookup).SingleAsync()).ToBson());
            Assert.Equal(fence, (await fences.Find(lookup).SingleAsync()).ToBson());
            var changed = Input(); changed.MetaData["private-note"] = 1.000000000000000000000000002m;
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", (await Save(PreparationManager(second, new PreparationKeys(), "key-b"), changed)).ErrorCode);
            Assert.Equal(Guid.Empty, changed.Id);
            Assert.Equal("COSMIC_PREPARATION_ACTOR_MISMATCH", (await Save(PreparationManager(second, new PreparationKeys(), "key-b"), Input(), Guid.NewGuid())).ErrorCode);
            Assert.Equal("OLD_PREPARATION_DENIED", (await Save(PreparationManager(second, new PreparationKeys { DenyOld = true }, "key-b"), Input())).ErrorCode);
            var after = await Bodies(); Assert.Equal(originalBodies.Length, after.Length);
            for (var index = 0; index < after.Length; index++) Assert.Equal(originalBodies[index], after[index]);

            var priorId = Guid.NewGuid(); var priorOp = Guid.NewGuid(); var prior = new Holon { Id = priorId, Name = "owned pre-preparation receipt" };
            var priorWrite = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(priorOp, actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(priorId, null) }), ConditionalHolonOperation.Save, new[] { prior }, saveChildren: false);
            var priorSaved = synchronous ? first.ExecuteConditionalHolonWrite(priorWrite) : await first.ExecuteConditionalHolonWriteAsync(priorWrite);
            Assert.False(priorSaved.IsError, priorSaved.Message + priorSaved.Exception);
            var opaque = (await preparations.Find(lookup).SingleAsync())["Records"].AsBsonArray[0].AsBsonDocument["Envelope"].AsString;
            var candidate = new HolonWritePreparation(priorOp, actor, ProviderType.MongoDBOASIS, new[] { new ProtectedHolonPreparation(priorId, opaque) });
            var refused = synchronous ? second.ClaimHolonWritePreparation(candidate) : await second.ClaimHolonWritePreparationAsync(candidate);
            Assert.Equal("COSMIC_PREPARATION_RECONCILIATION_REQUIRED", refused.ErrorCode); Assert.Null(refused.Result);
            Assert.Equal(0, await preparations.CountDocumentsAsync(new BsonDocument("_id", priorOp.ToString("D"))));
            complete = true;
        }
        finally
        {
            await first.DeActivateProviderAsync(); await second.DeActivateProviderAsync();
            if (complete) await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_conditional_fixture preparation/receipt/fence state for independent reconciliation.");
        }
    }
}
