using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NeoProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public interface IEnrolledPropertyBoundary : IPropertyOutcomeBoundary,
        IBoundAtomicHolonReplicationStorageProvider, IHolonReplicationOutboxSource { }

    public class EnrolledPropertyBoundary : OwnedPropertyBoundary
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IAtomicHolonReplicationStorageProvider.ExecuteReplicatingHolonWriteAsync))
                return Atomic((ConditionalHolonWriteRequest)args![0]!, (HolonReplicationEnrollment)args[1]!, (CancellationToken)args[2]!);
            return base.Invoke(method, args);
        }
        private async Task<OASISResult<IEnumerable<IHolon>>> Atomic(ConditionalHolonWriteRequest request,
            HolonReplicationEnrollment enrollment, CancellationToken token)
        {
            MutationIntents.Add(request.Write.OperationId);
            await BeforeResource(request.Write.OperationId, request.Write.Conditions.Select(row => row.HolonId));
            var before = StoredRecordJson.SerializeRuntime(Source);
            var result = await ((IAtomicHolonReplicationStorageProvider)Target).ExecuteReplicatingHolonWriteAsync(request, enrollment, token);
            Assert.AreEqual(before, StoredRecordJson.SerializeRuntime(Source));
            if (!result.IsError) SuccessfulMutations++;
            return result;
        }
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public async Task SelectedOwnedWritesDeliverBoundNativeReplicasWithoutWholeSaveDowngrade(string providerName)
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        const string neoUri = "bolt://127.0.0.1:17687";
        var sourceType = Enum.Parse<ProviderType>(providerName);
        var targetType = sourceType switch
        {
            ProviderType.MongoDBOASIS => ProviderType.SQLLiteDBOASIS,
            ProviderType.SQLLiteDBOASIS => ProviderType.Neo4jOASIS,
            ProviderType.Neo4jOASIS => ProviderType.MongoDBOASIS,
            _ => throw new InvalidOperationException("Only the three isolated native providers are admitted.")
        };
        var id = Guid.NewGuid(); var owner = Guid.NewGuid(); var writer = Guid.NewGuid();
        var databaseName = "cosmic_selected_enrolled_" + id.ToString("N");
        var workRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!);
        var work = Path.Combine(workRoot, databaseName);
        Assert.IsFalse(Directory.Exists(work)); Directory.CreateDirectory(work);
        var sourceSqlite = Path.Combine(work, "source.sqlite"); var targetSqlite = Path.Combine(work, "target.sqlite");
        var reportRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT")!);
        var report = Path.Combine(reportRoot, "TestResults/Neo4jCurrent/" + providerName + "-property-enrolled-native.json");
        var mongoUsed = sourceType == ProviderType.MongoDBOASIS || targetType == ProviderType.MongoDBOASIS;
        var neoUsed = sourceType == ProviderType.Neo4jOASIS || targetType == ProviderType.Neo4jOASIS;
        var mongo = new MongoClient(mongoUri);
        using var driver = GraphDatabase.Driver(neoUri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var operations = new HashSet<Guid>(); var complete = false; var checks = 0; var deliveries = 0;
        async Task Journal()
            => await File.WriteAllTextAsync(Path.Combine(work, "fixture.json"),
                JsonSerializer.Serialize(new { id, owner, writer, databaseName, sourceSqlite, targetSqlite, sourceType, targetType, operations }));
        await Journal();
        if (mongoUsed)
        {
            using var names = await mongo.ListDatabaseNamesAsync();
            Assert.IsFalse((await names.ToListAsync()).Contains(databaseName));
            // Provision the owned empty business namespace before capturing a durable store identity.
            await mongo.GetDatabase(databaseName).CreateCollectionAsync("CosmicOwnedFixture");
        }
        if (neoUsed)
        {
            var absent = await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS n",
                new { id = id.ToString("D") })).SingleAsync();
            Assert.AreEqual(0L, absent["n"].As<long>());
        }
        IOASISStorageProvider Make(ProviderType type, bool primary) => type switch
        {
            ProviderType.MongoDBOASIS => new MongoProvider(mongoUri, databaseName) { IsVersionControlEnabled = false },
            ProviderType.SQLLiteDBOASIS => new SqliteProvider("Data Source=" + (primary ? sourceSqlite : targetSqlite) + ";Pooling=False") { IsVersionControlEnabled = false },
            ProviderType.Neo4jOASIS => new NeoProvider(neoUri, "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false },
            _ => throw new InvalidOperationException("Invalid native authority.")
        };
        var sourceProvider = Make(sourceType, true); var targetProvider = Make(targetType, false);
        var sourceActive = false; var targetActive = false;
        try
        {
            var activation = await sourceProvider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message + activation.Exception); sourceActive = true;
            activation = await targetProvider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message + activation.Exception); targetActive = true;
            var sourceIdentity = await HolonReplicationRouteBinding.CapturePinnedAsync(sourceProvider);
            var targetIdentity = await HolonReplicationRouteBinding.CapturePinnedAsync(targetProvider);
            Assert.AreEqual(sourceType, sourceIdentity.Authority); Assert.AreEqual(targetType, targetIdentity.Authority);
            var grant = new HolonReplicationActorGrant(new[] { owner, writer }, DateTime.UtcNow.AddMinutes(10));
            var route = new HolonReplicationRouteBinding(sourceIdentity, targetIdentity, grant.BindingIdentity);
            var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, sourceIdentity, new[] { route });
            var captured = ((IHolonReplicationOutboxSource)sourceProvider).GetHolonReplicationOutbox();
            Assert.IsFalse(captured.IsError, captured.Message + captured.Exception);
            var outbox = captured.Result;
            var receiver = new CommittedHolonReplicaTarget(route.TargetId,
                (IBoundCommittedHolonReplicaStorageProvider)targetProvider, targetType, route.SourceId, sourceType,
                grant, "owned-selected-" + id.ToString("N"), Guid.NewGuid(), grant.BindingIdentity, targetIdentity);
            var worker = new HolonOutboxDispatcher(outbox, new[] { receiver }, "owned-selected-worker");
            Assert.IsNull((await outbox.PeekNextAsync(route.TargetId)).Result);
            checks++;

            var policy = new HolonMetadataEncryptionPolicy("selected-enrolled", "fixture-key-v1",
                new RecoveryPreparationKeys(), new[] { HolonPermissions.MetadataKey });
            var root = new Holon { Id = id, Name = "native original", Description = "initial", IsActive = true,
                MetadataEncryptionOverride = policy, MetaData = new()
                {
                    ["secret"] = "secret-before", ["private-sibling"] = "native-private",
                    ["big"] = 9007199254740993L,
                    [HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b =>
                        b.SetField(HolonPermissionSet.Key(writer), "description", HolonPermission.Write)
                         .SetField(HolonPermissionSet.Key(writer), "metadata.secret", HolonPermission.Write | HolonPermission.Delete)).Encode().Result
                } };
            var boundary = DispatchProxy.Create<IEnrolledPropertyBoundary, EnrolledPropertyBoundary>();
            var probe = (EnrolledPropertyBoundary)(object)boundary;
            probe.Target = sourceProvider; probe.Source = root;
            probe.BeforeResource = async (operation, ids) =>
            {
                CollectionAssert.AreEqual(new[] { id }, ids.ToArray()); operations.Add(operation); await Journal();
            };
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new() { LogSwitchingProviders = false } } };
            var registry = new ProviderManager(null, dna);
            Assert.IsTrue(registry.RegisterProvider(boundary));
            Assert.IsFalse(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(false, false, false), sourceType).IsError);
            var manager = new HolonManager(null, dna, registry)
                { MetadataEncryptionPolicy = policy, PreparationEncryptionPolicy = policy };
            CosmicWriteOptions Options(HolonReplicationEnrollment enrolled, Guid? op = null, Guid? revision = null)
            {
                var operation = op ?? Guid.NewGuid(); operations.Add(operation);
                return new(operation, revision.HasValue ? new Dictionary<Guid, Guid?> { [id] = revision } : null, enrolled);
            }
            Guid Revision(IHolon value, ProviderType type) => CosmicConcurrency.GetProviderRevisionId(value, type)!.Value;
            async Task<Holon> Read(ProviderType type, bool primary, int version)
            {
                var reader = Make(type, primary);
                try
                {
                    var active = await reader.ActivateProviderAsync(); Assert.IsFalse(active.IsError, active.Message + active.Exception);
                    var loaded = await reader.LoadHolonAsync(id, false, false, 0, true, false, 0);
                    Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
                    Assert.AreEqual(version, loaded.Result.Version);
                    var value = StoredRecordJson.Project<Holon>(loaded.Result);
                    Assert.IsTrue(value.MetaData.ContainsKey("__oasis_enc__"));
                    Assert.IsFalse(value.MetaData.ContainsKey("secret")); Assert.IsFalse(value.MetaData.ContainsKey("private-sibling"));
                    value.MetadataEncryptionOverride = policy;
                    var decrypted = manager.DecryptHolonMetaData(value);
                    Assert.IsFalse(decrypted.IsError, decrypted.Message + decrypted.Exception);
                    return value;
                }
                finally { await reader.DeActivateProviderAsync(); (reader as IDisposable)?.Dispose(); }
            }
            async Task<HolonReplicationObligation> Deliver(Guid operation)
            {
                var peek = await outbox.PeekNextAsync(route.TargetId);
                Assert.IsFalse(peek.IsError, peek.Message + peek.Exception); Assert.IsNotNull(peek.Result);
                Assert.AreEqual(operation, peek.Result.OperationId);
                var observed = await ((IHolonReplicationObligationReader)outbox).ReadObligationAsync(peek.Result.Sequence);
                Assert.IsFalse(observed.IsError, observed.Message + observed.Exception);
                var frozen = CommittedHolonReplicaPayload.ReadForTarget(observed.Result.PayloadJson, route.TargetId);
                Assert.AreEqual(operation, frozen.OperationId); Assert.AreEqual(route.SourceId, frozen.SourceId);
                Assert.AreEqual(id, frozen.Roots.Single().Id);
                var delivered = await worker.DispatchOnceAsync(route.TargetId, 1);
                Assert.IsFalse(delivered.IsError, delivered.Message + delivered.Exception);
                Assert.AreEqual(1, delivered.Result.CompletedCount); deliveries++;
                var state = await outbox.InspectAsync(peek.Result.Sequence);
                Assert.IsFalse(state.IsError, state.Message + state.Exception);
                Assert.AreEqual(HolonDeliveryState.Completed, state.Result.State);
                return observed.Result;
            }
            var initialOptions = Options(enrollment);
            var initial = await manager.SaveOwnedHolonAsync(root, owner, initialOptions, saveChildren: false, providerType: sourceType);
            Assert.IsFalse(initial.IsError, initial.Message + initial.Exception); Assert.AreEqual(1, root.Version);
            await Deliver(initialOptions.OperationId);
            var replica = await Read(targetType, false, 1);
            Assert.AreEqual("native original", replica.Name); Assert.AreEqual("secret-before", replica.MetaData["secret"].ToString());
            checks++;

            var initialSnapshot = StoredRecordJson.Project<Holon>(root);
            var caller = StoredRecordJson.Project<Holon>(root); caller.MetaData = new(); caller.MetadataEncryptionOverride = policy;
            HolonChangeTracking.AcceptChanges(caller, false);
            caller.Name = "excluded local name"; caller.Description = "selected description"; probe.Source = caller;
            var firstOptions = Options(enrollment, revision: Revision(caller, sourceType));
            var selected = await manager.SaveOwnedHolonAsync(caller, writer, firstOptions, new[] { "/Description" },
                saveChildren: false, providerType: sourceType);
            Assert.IsFalse(selected.IsError, selected.Message + selected.Exception); Assert.AreEqual(2, caller.Version);
            var firstDelta = selected.MetaData[HolonCommittedDelta.ResultMetadataKey];
            Assert.AreEqual("excluded local name", caller.Name);
            Assert.IsFalse(caller.MetaData.ContainsKey("private-sibling")); checks++;
            var frozenSecond = await Deliver(firstOptions.OperationId);
            replica = await Read(targetType, false, 2);
            Assert.AreEqual("native original", replica.Name); Assert.AreEqual("selected description", replica.Description);
            Assert.AreEqual("native-private", replica.MetaData["private-sibling"].ToString()); checks++;

            caller.MetaData["secret"] = "selected encrypted";
            var secretOptions = Options(enrollment);
            var secret = await manager.SaveOwnedHolonAsync(caller, writer, secretOptions, new[] { "/MetaData/secret" },
                saveChildren: false, providerType: sourceType);
            Assert.IsFalse(secret.IsError, secret.Message + secret.Exception);
            var frozenThird = await Deliver(secretOptions.OperationId);
            replica = await Read(targetType, false, 3);
            Assert.AreEqual("selected encrypted", replica.MetaData["secret"].ToString());
            Assert.AreEqual("native-private", replica.MetaData["private-sibling"].ToString()); checks++;

            caller.MetaData.Remove("secret"); var removalOptions = Options(enrollment);
            var removed = await manager.SaveOwnedHolonAsync(caller, writer, removalOptions, new[] { "/MetaData/secret" },
                saveChildren: false, providerType: sourceType);
            Assert.IsFalse(removed.IsError, removed.Message + removed.Exception);
            var frozenFourth = await Deliver(removalOptions.OperationId);
            replica = await Read(targetType, false, 4);
            Assert.IsFalse(replica.MetaData.ContainsKey("secret"));
            Assert.AreEqual("native-private", replica.MetaData["private-sibling"].ToString()); checks++;

            var replayCaller = StoredRecordJson.Project<Holon>(initialSnapshot);
            replayCaller.MetaData = new(); replayCaller.MetadataEncryptionOverride = policy;
            HolonChangeTracking.AcceptChanges(replayCaller, false);
            replayCaller.Name = "excluded replay name"; replayCaller.Description = "selected description"; probe.Source = replayCaller;
            var replay = await manager.SaveOwnedHolonAsync(replayCaller, writer, firstOptions, new[] { "/Description" },
                saveChildren: false, providerType: sourceType);
            Assert.IsFalse(replay.IsError, replay.Message + replay.Exception);
            Assert.AreEqual(2, replayCaller.Version); Assert.AreEqual(firstDelta, replay.MetaData[HolonCommittedDelta.ResultMetadataKey]);
            Assert.IsNull((await outbox.PeekNextAsync(route.TargetId)).Result);
            Assert.AreEqual(4, (await Read(sourceType, true, 4)).Version);
            Assert.AreEqual(4, (await Read(targetType, false, 4)).Version); checks++;

            var changedRoute = new HolonReplicationRouteBinding(sourceIdentity, targetIdentity, grant.BindingIdentity + "-changed");
            var changedEnrollment = new HolonReplicationEnrollment(changedRoute.SourceId, new[] { changedRoute.TargetId }, sourceIdentity, new[] { changedRoute });
            var changed = await manager.SaveOwnedHolonAsync(replayCaller, writer,
                Options(changedEnrollment, firstOptions.OperationId, Revision(initialSnapshot, sourceType)), new[] { "/Description" },
                saveChildren: false, providerType: sourceType);
            Assert.IsTrue(changed.IsError);
            Assert.IsNull((await outbox.PeekNextAsync(changedRoute.TargetId)).Result); checks++;

            var wrongIdentity = new CosmicStorageIdentity(sourceType, sourceIdentity.EndpointFingerprint, Guid.NewGuid());
            caller.Description = "wrong binding attempt"; probe.Source = caller;
            var wrong = await manager.SaveOwnedHolonAsync(caller, writer,
                Options(new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, wrongIdentity)),
                new[] { "/Description" }, saveChildren: false, providerType: sourceType);
            Assert.IsTrue(wrong.IsError);
            Assert.IsNull((await outbox.PeekNextAsync(route.TargetId)).Result);
            Assert.AreEqual(4, (await Read(sourceType, true, 4)).Version); checks++;

            caller.Description = "stale enrolled attempt";
            var stale = await manager.SaveOwnedHolonAsync(caller, writer,
                Options(enrollment, revision: Revision(initialSnapshot, sourceType)), new[] { "/Description" },
                saveChildren: false, providerType: sourceType);
            Assert.IsTrue(stale.IsError); Assert.AreEqual(CosmicConcurrencyException.Code, stale.ErrorCode);
            Assert.IsNull((await outbox.PeekNextAsync(route.TargetId)).Result);
            Assert.AreEqual(4, (await Read(sourceType, true, 4)).Version);
            Assert.AreEqual(4, (await Read(targetType, false, 4)).Version); checks++;

            var inspected = await ((IHolonReplicationObligationReader)outbox).ReadObligationAsync(frozenSecond.Inspection.Sequence);
            Assert.IsFalse(inspected.IsError, inspected.Message + inspected.Exception);
            Assert.AreEqual(frozenSecond.PayloadJson, inspected.Result.PayloadJson);
            Assert.AreEqual(frozenSecond.PayloadDigest, inspected.Result.PayloadDigest);
            Assert.AreEqual(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(frozenSecond.PayloadJson))), frozenSecond.PayloadDigest);
            Assert.IsTrue(frozenSecond.Inspection.Sequence < frozenThird.Inspection.Sequence);
            Assert.IsTrue(frozenThird.Inspection.Sequence < frozenFourth.Inspection.Sequence); checks++;

            var finalSource = await Read(sourceType, true, 4); replica = await Read(targetType, false, 4);
            Assert.AreEqual(finalSource.VersionId, replica.VersionId);
            Assert.AreNotEqual(Revision(finalSource, sourceType), Revision(replica, targetType));
            Assert.AreEqual(9007199254740993L, Convert.ToInt64(replica.MetaData["big"].ToString())); checks++;

            var rights = new HolonAccessResolver(writer, includePublic: false).WithGroups(Array.Empty<Guid>()).Resolve(finalSource);
            Assert.IsFalse(rights.CanSee); Assert.AreEqual(1, probe.MutationIntents.Count);
            Assert.AreEqual(3, probe.SelectedCommittedOperations.Count); Assert.AreEqual(4, deliveries);
            Assert.IsFalse(caller.MetaData.ContainsKey("private-sibling"));
            Assert.AreEqual("excluded local name", caller.Name); checks++;
            Assert.AreEqual(13, checks); complete = true;
        }
        finally
        {
            if (sourceActive) Assert.IsFalse((await sourceProvider.DeActivateProviderAsync()).IsError);
            if (targetActive) Assert.IsFalse((await targetProvider.DeActivateProviderAsync()).IsError);
            (sourceProvider as IDisposable)?.Dispose(); (targetProvider as IDisposable)?.Dispose();
            if (complete)
            {
                if (mongoUsed)
                {
                    await mongo.DropDatabaseAsync(databaseName);
                    using var names = await mongo.ListDatabaseNamesAsync();
                    Assert.IsFalse((await names.ToListAsync()).Contains(databaseName));
                }
                if (neoUsed)
                {
                    var ops = operations.Select(value => value.ToString("D")).ToArray();
                    const string owned = "MATCH (n) WHERE (n:Holon AND n.Id=$id) OR " +
                        "(n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) OR " +
                        "(n:CosmicReplicaHead AND n.HolonId=$id) OR " +
                        "((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $ops) ";
                    await (await inspection.RunAsync(owned + "DETACH DELETE n", new { id = id.ToString("D"), ops })).ConsumeAsync();
                    var remaining = await (await inspection.RunAsync(owned + "RETURN count(n) AS n",
                        new { id = id.ToString("D"), ops })).SingleAsync();
                    Assert.AreEqual(0L, remaining["n"].As<long>());
                }
                Assert.AreEqual(workRoot, Path.GetDirectoryName(Path.GetFullPath(work)));
                Directory.Delete(work, true); Assert.IsFalse(Directory.Exists(work)); checks++;
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new {
                    provider = providerName, target = targetType.ToString(), checks, successfulHolonWrites = 4, deliveredVersions = 4,
                    boundSourceVerified = true, boundTargetVerified = true, firstCreateDeliveredVerified = true,
                    selectedScalarDeliveredVerified = true, encryptedUpdateDeliveredVerified = true, encryptedRemovalDeliveredVerified = true,
                    unselectedValuesPreservedVerified = true, callerPrivacyVerified = true, fieldOnlyWriteVerified = true,
                    receiptReplayVerified = true, noExtraObligationVerified = true, changedTargetRefused = true,
                    wrongSourceBindingRefused = true, staleEnrollmentRefused = true, frozenPayloadVerified = true, nativeReplicaRevisionVerified = true,
                    noOrdinaryWriteDowngradeVerified = true, ownedBusinessCleanupVerified = true, exactCleanupVerified = true }));
            }
        }
    }
}
