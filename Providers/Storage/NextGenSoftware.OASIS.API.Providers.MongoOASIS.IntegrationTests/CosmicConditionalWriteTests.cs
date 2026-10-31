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
using NextGenSoftware.OASIS.Common;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeConditionalWritesBindGraphReceiptAndAuthorityAcrossClients(bool synchronous)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string name = "cosmic_conditional_fixture";
        var client = new MongoClient(uri); using var names = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name, await names.ToListAsync());
        var database = client.GetDatabase(name); await database.CreateCollectionAsync("CosmicOwnedFixture");
        var first = new Provider(uri, name); var second = new Provider(uri, name); var complete = false;
        var actor = Guid.NewGuid();
        var rows = database.GetCollection<BsonDocument>("Holon");
        var receipts = database.GetCollection<BsonDocument>("CosmicWriteReceipt");
        Task<OASISResult<IEnumerable<IHolon>>> Write(Provider provider, ConditionalHolonWriteRequest request)
            => synchronous ? Task.Run(() => provider.ExecuteConditionalHolonWrite(request)) : provider.ExecuteConditionalHolonWriteAsync(request);
        Guid Token(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, ProviderType.MongoDBOASIS)!.Value;
        ConditionalHolonWriteRequest Request(IHolon value, Guid? expected, ConditionalHolonOperation operation = ConditionalHolonOperation.Save, Guid? operationId = null)
            => new(new ConditionalHolonWrite(operationId ?? Guid.NewGuid(), actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(value.Id, expected) }), operation, new[] { value }, saveChildren: false);
        async Task<byte[]> Native(Guid id) => (await rows.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync()).ToBson();
        try
        {
            Assert.False((await first.ActivateProviderAsync()).IsError); Assert.False((await second.ActivateProviderAsync()).IsError);
            var child = new Holon { Id = Guid.NewGuid(), Name = "owned conditional child" };
            var root = new Holon { Id = Guid.NewGuid(), Name = "owned conditional root", Children = new List<IHolon> { child } };
            var create = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) }), ConditionalHolonOperation.Save, new[] { root });
            var saved = await Write(first, create); Assert.False(saved.IsError, saved.Message + saved.Exception);
            var created = Assert.Single(saved.Result); var createdChild = Assert.Single(created.Children);
            Assert.Equal(1, created.Version); Assert.NotEqual(Guid.Empty, Token(created)); Assert.NotEqual(Guid.Empty, Token(createdChild));
            Assert.Equal(actor, created.CreatedByAvatarId); Assert.Equal(0, root.Version);
            // Compare actual persisted BSON with the acknowledgement, including sub-millisecond ticks.
            var nativeCreated = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>(await Native(root.Id));
            Assert.Equal(created.CreatedDate.Ticks, nativeCreated.CreatedDate.Ticks);
            Assert.Equal(created.ModifiedDate.Ticks, nativeCreated.ModifiedDate.Ticks);
            Assert.Equal(created.DeletedDate.Ticks, nativeCreated.DeletedDate.Ticks);

            Assert.Null(CosmicConcurrency.GetProviderRevisionId(root, ProviderType.MongoDBOASIS));
            var later = new Holon { Id = root.Id, Name = "owned later raw root", Version = 8, VersionId = Guid.NewGuid() };
            var intervening = await first.SaveHolonAsync(later, saveChildren: false); Assert.False(intervening.IsError, intervening.Message + intervening.Exception);
            Assert.NotEqual(Token(created), Token(intervening.Result));
            // Reconstruct the provider after the commit: no in-memory receipt can supply this replay.
            await second.DeActivateProviderAsync(); second = new Provider(uri, name);
            Assert.False((await second.ActivateProviderAsync()).IsError);
            // A different provider instance replays the original receipt, not the newer record.
            var replay = await Write(second, create); Assert.False(replay.IsError, replay.Message + replay.Exception);
            Assert.Equal(created.VersionId, Assert.Single(replay.Result).VersionId); Assert.Equal("owned conditional root", replay.Result.Single().Name);
            root.Name = "changed intent";
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", (await Write(first, create)).ErrorCode); root.Name = "owned conditional root";
            var wrongActor = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(create.Write.OperationId, Guid.NewGuid(), ProviderType.MongoDBOASIS, create.Write.Conditions), create.Operation, create.Roots);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", (await Write(first, wrongActor)).ErrorCode);
            var rawChild = await first.SaveHolonAsync(new Holon { Id = child.Id, Name = "owned later raw child", Version = 4, VersionId = Guid.NewGuid() }, saveChildren: false);
            Assert.False(rawChild.IsError, rawChild.Message + rawChild.Exception);
            var rootBefore = await Native(root.Id);
            var stale = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(root.Id, Token(intervening.Result)), new HolonWriteCondition(child.Id, Token(createdChild)) }), ConditionalHolonOperation.Save, new[] { root });
            var rejected = await Write(first, stale); Assert.Equal(CosmicConcurrencyException.Code, rejected.ErrorCode);
            Assert.Equal(child.Id, Assert.Single(Assert.IsType<CosmicConcurrencyException>(rejected.Exception).Conflicts).HolonId);
            Assert.Equal(rootBefore, await Native(root.Id)); Assert.Equal(1, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var update = Request(new Holon { Id = root.Id, Name = "owned conditional update", Version = 999 }, Token(intervening.Result));
            var updated = await Write(first, update); Assert.False(updated.IsError, updated.Message + updated.Exception);
            var current = Assert.Single(updated.Result); Assert.Equal(9, current.Version); Assert.Equal(later.VersionId, current.PreviousVersionId);
            var competitors = await Task.WhenAll(
                Write(first, Request(new Holon { Id = root.Id, Name = "owned competitor A" }, Token(current))),
                Write(second, Request(new Holon { Id = root.Id, Name = "owned competitor B" }, Token(current))));
            Assert.Single(competitors.Where(result => !result.IsError));
            Assert.Contains(Assert.Single(competitors.Where(result => result.IsError)).ErrorCode,
                new[] { CosmicConcurrencyException.Code, "COSMIC_CONCURRENT_WRITE" });
            current = Assert.Single(competitors.Single(result => !result.IsError).Result);
            // Reject the receipt after native record mutation; the same transaction must roll both back.
            await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", "CosmicWriteReceipt" },
                { "validator", new BsonDocument("Fingerprint", "owned receipt rejection") }, { "validationLevel", "strict" } });
            var beforeFailure = await Native(root.Id); var receiptCount = await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            var retry = Request(new Holon { Id = root.Id, Name = "owned receipt rollback" }, Token(current));
            var failed = await Write(first, retry); Assert.True(failed.IsError); Assert.Equal("COSMIC_CONDITIONAL_WRITE_FAILED", failed.ErrorCode); Assert.Equal(beforeFailure, await Native(root.Id));
            Assert.Equal(receiptCount, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", "CosmicWriteReceipt" }, { "validator", new BsonDocument() } });
            var retried = await Write(second, retry); Assert.False(retried.IsError, retried.Message + retried.Exception); current = Assert.Single(retried.Result);
            var softRequest = Request(new Holon { Id = root.Id }, Token(current), ConditionalHolonOperation.SoftDelete);
            var soft = await Write(first, softRequest); Assert.False(soft.IsError, soft.Message + soft.Exception); Assert.True(soft.IsDeleted);
            var tombstone = Assert.Single(soft.Result); Assert.True(tombstone.IsDeleted); Assert.Equal(current.Name, tombstone.Name);
            Assert.Equal(current.VersionId, tombstone.PreviousVersionId); Assert.Equal(actor, tombstone.DeletedByAvatarId);
            Assert.Equal(tombstone.VersionId, Assert.Single((await Write(second, softRequest)).Result).VersionId);
            var hardRequest = Request(new Holon { Id = root.Id }, Token(tombstone), ConditionalHolonOperation.HardDelete);
            var hard = await Write(first, hardRequest); Assert.False(hard.IsError, hard.Message + hard.Exception); Assert.True(hard.IsDeleted);
            Assert.Equal(0, await rows.CountDocumentsAsync(new BsonDocument("HolonId", root.Id.ToString("D"))));
            Assert.Equal(1, await rows.CountDocumentsAsync(new BsonDocument("HolonId", child.Id.ToString("D"))));
            Assert.Equal(Assert.Single(hard.Result).VersionId, Assert.Single((await Write(second, hardRequest)).Result).VersionId);
            Assert.Equal(CosmicConcurrencyException.Code, (await Write(second, Request(new Holon { Id = root.Id }, Token(tombstone), ConditionalHolonOperation.HardDelete))).ErrorCode);
            var createRaceId = Guid.NewGuid();
            var createRace = await Task.WhenAll(Write(first, Request(new Holon { Id = createRaceId, Name = "owned create A" }, null)),
                Write(second, Request(new Holon { Id = createRaceId, Name = "owned create B" }, null)));
            Assert.Single(createRace.Where(result => !result.IsError));
            Assert.Contains(Assert.Single(createRace.Where(result => result.IsError)).ErrorCode,
                new[] { CosmicConcurrencyException.Code, "COSMIC_CONCURRENT_WRITE" });
            var exactRequest = Request(new Holon { Id = Guid.NewGuid(), Name = "owned concurrent replay" }, null);
            var exactRace = await Task.WhenAll(Write(first, exactRequest), Write(second, exactRequest));
            Assert.Contains(exactRace, result => !result.IsError);
            var exactReplay = await Write(second, exactRequest); Assert.False(exactReplay.IsError, exactReplay.Message + exactReplay.Exception);
            foreach (var acknowledgement in exactRace.Where(result => !result.IsError))
                Assert.Equal(Assert.Single(acknowledgement.Result).VersionId, Assert.Single(exactReplay.Result).VersionId);
            var exactFilter = new BsonDocument("_id", exactRequest.Write.OperationId.ToString("D"));
            var exactReceipt = await receipts.Find(exactFilter).SingleAsync();
            await receipts.UpdateOneAsync(exactFilter, new BsonDocument("$set", new BsonDocument("ResultJson", "invalid owned receipt")));
            Assert.Equal("COSMIC_RECEIPT_INVALID", (await Write(first, exactRequest)).ErrorCode);
            await receipts.UpdateOneAsync(exactFilter, new BsonDocument("$set", new BsonDocument("ResultJson", exactReceipt["ResultJson"])));
            var unchangedReceipts = await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            var wrongAuthority = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(Guid.NewGuid(), null) }), ConditionalHolonOperation.Save, new[] { new Holon { Id = Guid.NewGuid() } });
            Assert.True((await Write(first, wrongAuthority)).IsError);
            using (var cancelled = new System.Threading.CancellationTokenSource())
            {
                cancelled.Cancel();
                Assert.True((await first.ExecuteConditionalHolonWriteAsync(Request(new Holon { Id = Guid.NewGuid() }, null), cancelled.Token)).IsError);
            }
            Assert.Equal(unchangedReceipts, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var legacyId = Guid.NewGuid(); var legacyLogical = Guid.NewGuid();
            var legacySeed = await first.SaveHolonAsync(new Holon { Id = legacyId, Name = "owned legacy revision", Version = 4, VersionId = legacyLogical }, saveChildren: false);
            Assert.False(legacySeed.IsError, legacySeed.Message + legacySeed.Exception);
            await rows.UpdateOneAsync(new BsonDocument("HolonId", legacyId.ToString("D")), new BsonDocument("$unset", new BsonDocument("ProviderRevisionId", 1)));
            var legacyRead = await first.LoadHolonAsync(legacyId, loadChildren: false);
            Assert.False(legacyRead.IsError, legacyRead.Message + legacyRead.Exception); Assert.Equal(Guid.Empty, Token(legacyRead.Result));
            Assert.Equal(CosmicConcurrencyException.Code, (await Write(second, Request(new Holon { Id = legacyId }, null))).ErrorCode);
            var legacySaved = await Write(second, Request(new Holon { Id = legacyId, Name = "owned upgraded legacy revision" }, Guid.Empty));
            Assert.False(legacySaved.IsError, legacySaved.Message + legacySaved.Exception);
            var upgraded = Assert.Single(legacySaved.Result); Assert.Equal(5, upgraded.Version); Assert.Equal(legacyLogical, upgraded.PreviousVersionId);
            Assert.NotEqual(Guid.Empty, Token(upgraded));
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false } } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            NextGenSoftware.OASIS.API.Core.Managers.HolonManager Manager(Provider provider)
            {
                var providers = new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna) {
                    IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoFailOverEnabled = true };
                providers.RegisterProvider(provider);
                return new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna, providers);
            }
            var manager = Manager(first); var otherManager = Manager(second);
            var managedRoot = new Holon { Name = "owned managed conditional root" };
            var managed = synchronous
                ? manager.SaveHolon(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.MongoDBOASIS)
                : await manager.SaveHolonAsync(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Assert.False(managed.IsError, managed.Message + managed.Exception); Assert.Same(managedRoot, managed.Result);
            Assert.False(managedRoot.HasHolonChanged()); Assert.NotEqual(Guid.Empty, Token(managedRoot));
            var competing = await otherManager.LoadHolonAsync(managedRoot.Id, loadChildren: false, providerType: ProviderType.MongoDBOASIS);
            Assert.False(competing.IsError, competing.Message + competing.Exception);
            var priorVersion = competing.Result.VersionId;
            managedRoot.Name = "owned managed winner";
            var winning = synchronous
                ? manager.SaveHolon(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.MongoDBOASIS)
                : await manager.SaveHolonAsync(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Assert.False(winning.IsError, winning.Message + winning.Exception); Assert.False(managedRoot.HasHolonChanged());
            competing.Result.Name = "pending managed loser";
            var losing = synchronous
                ? otherManager.SaveHolon(competing.Result, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.MongoDBOASIS)
                : await otherManager.SaveHolonAsync(competing.Result, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Assert.Equal(CosmicConcurrencyException.Code, losing.ErrorCode);
            Assert.Equal(priorVersion, competing.Result.VersionId); Assert.True(competing.Result.HasHolonChanged());
            var managedDelete = synchronous
                ? manager.DeleteHolon(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), providerType: ProviderType.MongoDBOASIS)
                : await manager.DeleteHolonAsync(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), providerType: ProviderType.MongoDBOASIS);
            Assert.False(managedDelete.IsError, managedDelete.Message + managedDelete.Exception);
            Assert.True(managedRoot.IsDeleted); Assert.False(managedRoot.HasHolonChanged());
            var managedHard = synchronous
                ? manager.DeleteHolon(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), softDelete: false, providerType: ProviderType.MongoDBOASIS)
                : await manager.DeleteHolonAsync(managedRoot, actor, new CosmicWriteOptions(Guid.NewGuid()), softDelete: false, providerType: ProviderType.MongoDBOASIS);
            Assert.False(managedHard.IsError, managedHard.Message + managedHard.Exception);
            Assert.Equal(0, await rows.CountDocumentsAsync(new BsonDocument("HolonId", managedRoot.Id.ToString("D"))));
            await VerifyPostCommitAcknowledgementFailure(uri, name, actor, synchronous, database, Manager);
            complete = true;
        }
        finally
        {
            await first.DeActivateProviderAsync(); await second.DeActivateProviderAsync();
            if (complete) await client.DropDatabaseAsync(name);
        }
    }
    private async Task VerifyPostCommitAcknowledgementFailure(string uri, string name, Guid actor, bool synchronous,
        IMongoDatabase database, Func<Provider, NextGenSoftware.OASIS.API.Core.Managers.HolonManager> managerFactory)
    {
        const string app = "cosmic-conditional-original-commit";
        var settings = MongoClientSettings.FromConnectionString(uri + "&appName=" + app);
        var inserts = 0; var aborts = 0; var commits = 0;
        settings.ClusterConfigurator = cluster => cluster.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(command => {
            if (command.CommandName == "insert") System.Threading.Interlocked.Increment(ref inserts);
            if (command.CommandName == "abortTransaction") System.Threading.Interlocked.Increment(ref aborts);
            if (command.CommandName == "commitTransaction") System.Threading.Interlocked.Increment(ref commits);
        });
        var monitored = new MongoClient(settings);
        var fault = new Provider(uri + "&appName=" + app, name);
        Assert.False((await fault.ActivateProviderAsync()).IsError);
        // Inject the real command subscriber through the existing context, before any mutation.
        fault.Database.MongoClient = monitored; fault.Database.MongoDB = monitored.GetDatabase(name);
        var admin = new MongoClient(uri).GetDatabase("admin");
        var source = new Holon { Name = "owned uncertain root", Children = new List<IHolon> { new Holon { Name = "owned uncertain child" } } };
        var operation = Guid.NewGuid(); var options = new CosmicWriteOptions(operation); var manager = managerFactory(fault);
        try
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "alwaysOn" },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", app },
                    { "writeConcernError", new BsonDocument { { "code", 64 }, { "errmsg", "owned conditional post-commit acknowledgement fault" } } } } } });
            var failed = synchronous
                ? manager.SaveHolon(source, actor, options, providerType: ProviderType.MongoDBOASIS)
                : await manager.SaveHolonAsync(source, actor, options, providerType: ProviderType.MongoDBOASIS);
            Assert.True(failed.IsError); Assert.False(failed.IsSaved); Assert.Equal("COSMIC_WRITE_RECONCILIATION_REQUIRED", failed.ErrorCode);
            var evidence = Assert.IsType<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoTransactionReconciliationException>(failed.Exception);
            Assert.True(BsonDocument.Parse(evidence.SessionId).Contains("id")); Assert.True(evidence.TransactionNumber >= 0);
            var nativeCause = Assert.IsType<MongoWriteConcernException>(evidence.InnerException);
            output.WriteLine($"Original conditional operation={operation:D}, session={evidence.SessionId}, transaction={evidence.TransactionNumber}, commit reply={nativeCause.WriteConcernResult.Response.ToJson()}");
            Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
            Assert.Equal(0, aborts); Assert.True(commits >= 1); Assert.Equal(3, inserts);
            Assert.Equal(Guid.Empty, source.Id); Assert.Equal(Guid.Empty, Assert.Single(source.Children).Id); Assert.Equal(0, source.Version);
            // Read original evidence independently; never submit another save to discover the outcome.
            var commitReply = nativeCause.WriteConcernResult.Response;
            Assert.True(commitReply.Contains("operationTime"), "A causal majority read requires the original commit operation time.");
            var observed = await database.RunCommandAsync<BsonDocument>(new BsonDocument {
                { "find", "CosmicWriteReceipt" }, { "filter", new BsonDocument("_id", operation.ToString("D")) },
                { "readConcern", new BsonDocument { { "level", "majority" }, { "afterClusterTime", commitReply["operationTime"] } } },
                { "maxTimeMS", 5000 }, { "singleBatch", true }, { "batchSize", 2 } });
            var receipt = Assert.Single(observed["cursor"]["firstBatch"].AsBsonArray).AsBsonDocument;
            var original = Assert.Single(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString));
            Assert.Equal("owned uncertain root", original.Name); Assert.Equal(1, original.Version);
            var child = Assert.Single(original.Children); Assert.Equal("owned uncertain child", child.Name);
            var native = await database.GetCollection<BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
                .Find(new BsonDocument("HolonId", original.Id.ToString("D"))).SingleAsync();
            Assert.Equal(original.VersionId.ToString("D"), native["VersionId"].AsString);
            Assert.Equal(CosmicConcurrency.GetProviderRevisionId(original, ProviderType.MongoDBOASIS)!.Value.ToString("D"), native["ProviderRevisionId"].AsString);
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            var commitAttempts = commits;
            // Original commit is now proven. Exact receipt replay may publish it without another mutation.
            var reconciled = synchronous
                ? manager.SaveHolon(source, actor, options, providerType: ProviderType.MongoDBOASIS)
                : await manager.SaveHolonAsync(source, actor, options, providerType: ProviderType.MongoDBOASIS);
            Assert.False(reconciled.IsError, reconciled.Message + reconciled.Exception);
            Assert.Equal(original.Id, source.Id); Assert.Equal(original.VersionId, source.VersionId); Assert.Equal(child.Id, Assert.Single(source.Children).Id);
            Assert.False(source.HasHolonChanged()); Assert.Equal(3, inserts); Assert.Equal(commitAttempts, commits);

            // An exact receipt remains authoritative even if aborting its read-only lookup fails.
            var replaySource = new Holon { Id = Guid.NewGuid(), Name = "owned receipt cleanup root" };
            var replayRequest = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor,
                ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(replaySource.Id, null) }),
                ConditionalHolonOperation.Save, new[] { replaySource }, saveChildren: false);
            var receiptCreated = synchronous ? fault.ExecuteConditionalHolonWrite(replayRequest)
                : await fault.ExecuteConditionalHolonWriteAsync(replayRequest);
            Assert.False(receiptCreated.IsError, receiptCreated.Message + receiptCreated.Exception);
            var expectedReceipt = Assert.Single(receiptCreated.Result);
            var replayCommits = commits; var replayInserts = inserts; var priorAborts = aborts;
            fault.Database.MongoClient = MongoReceiptCleanupFault.Wrap(monitored, failDispose: synchronous);
            var cleanupReplay = synchronous ? fault.ExecuteConditionalHolonWrite(replayRequest)
                : await fault.ExecuteConditionalHolonWriteAsync(replayRequest);
            Assert.False(cleanupReplay.IsError, cleanupReplay.Message + cleanupReplay.Exception);
            Assert.True(cleanupReplay.IsSaved); Assert.True(cleanupReplay.IsWarning);
            Assert.NotNull(cleanupReplay.Exception); Assert.Equal(synchronous ? 2 : 1, cleanupReplay.WarningCount);
            if (synchronous)
            {
                var aggregate = Assert.IsType<AggregateException>(cleanupReplay.Exception);
                Assert.Equal(2, aggregate.InnerExceptions.Count);
                Assert.All(aggregate.InnerExceptions, cause => Assert.IsType<IOException>(cause));
            }
            var replayed = Assert.Single(cleanupReplay.Result);
            Assert.Equal(expectedReceipt.VersionId, replayed.VersionId);
            Assert.Equal(expectedReceipt.CreatedDate.Ticks, replayed.CreatedDate.Ticks);
            Assert.Equal(replayCommits, commits); Assert.Equal(replayInserts, inserts);
            Assert.Equal(priorAborts + 1, aborts);

        }
        finally
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            await fault.DeActivateProviderAsync();
        }
    }
}
