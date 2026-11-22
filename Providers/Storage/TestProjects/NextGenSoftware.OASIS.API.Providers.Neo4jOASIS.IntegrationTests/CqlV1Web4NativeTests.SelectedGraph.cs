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
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public class GraphPropertyBoundary : OwnedPropertyBoundary
    {
        public Guid ParentIdentity;
        public int ParentReads;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IOASISStorageProvider.LoadHolonAsync)
                && args![0] is Guid id && id == ParentIdentity) ParentReads++;
            return base.Invoke(method, args);
        }
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SelectedGraphSavesEnforceParentPermissionsAndAtomicNativeConditions(string providerName)
        => RunSelectedGraphNative(providerName, false);

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task ExplicitChildIdentityGraphsRequireVerifiedParentsAndBoundedReads(string providerName)
        => RunSelectedGraphNative(providerName, true);

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task TypedSelectedScalarsComposeWithStoredRolesAndPartialViews(string providerName)
        => RunSelectedGraphNative(providerName, false, true);

    [DataTestMethod]
    [DataRow("MongoDBOASIS", false)] [DataRow("MongoDBOASIS", true)]
    [DataRow("SQLLiteDBOASIS", false)] [DataRow("SQLLiteDBOASIS", true)]
    [DataRow("Neo4jOASIS", false)] [DataRow("Neo4jOASIS", true)]
    public Task SelectedCustomScalarsUseNativeStorageMapping(string providerName, bool protectedMetadata)
        => RunSelectedGraphNative(providerName, false, false, true, protectedMetadata);

    private async Task RunSelectedGraphNative(string providerName, bool explicitChildIdentity, bool roleComposition = false,
        bool customProperties = false, bool protectedMetadata = false)
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var authority = Enum.Parse<ProviderType>(providerName);
        var owner = Guid.NewGuid(); var writer = Guid.NewGuid(); var id = Guid.NewGuid();
        var database = "cosmic_selected_graph_" + id.ToString("N");
        var temp = Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!);
        var work = Path.Combine(temp, database);
        Assert.IsFalse(Directory.Exists(work)); Directory.CreateDirectory(work);
        var sqlite = Path.Combine(work, "graph.sqlite");
        var reportRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT")!);
        var report = Path.Combine(reportRoot, "TestResults/Neo4jCurrent/" + providerName
            + (customProperties ? "-property-custom-" + (protectedMetadata ? "protected" : "plain") + "-native.json"
                : roleComposition ? "-property-role-native.json" : explicitChildIdentity ? "-property-parent-native.json" : "-property-graph-native.json"));
        var mongo = new MongoClient(mongoUri);
        using (var names = await mongo.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS n",
            new { id = id.ToString("D") })).SingleAsync())["n"].As<long>());
        IOASISStorageProvider provider = authority switch
        {
            ProviderType.MongoDBOASIS => new MongoProvider(mongoUri, database) { IsVersionControlEnabled = false },
            ProviderType.SQLLiteDBOASIS => new SqliteProvider("Data Source=" + sqlite + ";Pooling=False") { IsVersionControlEnabled = false },
            ProviderType.Neo4jOASIS => new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false },
            _ => throw new InvalidOperationException("Only isolated native providers are admitted.")
        };
        var operations = new HashSet<Guid>(); var ids = new HashSet<Guid> { id };
        var active = false; var complete = false; var checks = 0;
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message + activation.Exception); active = true;
            var root = new Holon { Id = id, Name = "native parent", Description = "initial", IsActive = true };
            root.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b =>
                b.SetGrant(HolonPermissionSet.Key(writer), HolonPermission.Write)).Encode().Result;
            var boundary = explicitChildIdentity
                ? DispatchProxy.Create<IPropertyOutcomeBoundary, GraphPropertyBoundary>()
                : DispatchProxy.Create<IPropertyOutcomeBoundary, OwnedPropertyBoundary>();
            var probe = (OwnedPropertyBoundary)(object)boundary;
            var parentProbe = probe as GraphPropertyBoundary;
            if (parentProbe != null) parentProbe.ParentIdentity = id;
            probe.Target = provider; probe.Source = root;
            probe.BeforeResource = async (operation, records) =>
            {
                operations.Add(operation); ids.UnionWith(records);
                await File.WriteAllTextAsync(Path.Combine(work, "fixture.json"),
                    JsonSerializer.Serialize(new { database, sqlite, ids, operations }));
            };
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new() { LogSwitchingProviders = false } } };
            var registry = new ProviderManager(null, dna);
            Assert.IsTrue(registry.RegisterProvider(boundary));
            Assert.IsFalse(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(false, false, false), authority).IsError);
            var manager = new HolonManager(null, dna, registry);
            if (customProperties)
            {
                checks = await VerifySelectedCustomNative(provider, manager, probe, authority, owner, id, protectedMetadata);
                Assert.AreEqual(7, checks); complete = true; return;
            }
            if (roleComposition)
            {
                checks = await VerifyTypedRoleSelectedNative(provider, manager, probe, authority, owner, id);
                Assert.AreEqual(6, checks); complete = true; return;
            }
            Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)!.Value;
            async Task<Holon> Read(Guid record)
            {
                var loaded = await provider.LoadHolonAsync(record, false, false, 0, true, false, 0);
                Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
                Assert.IsNotNull(loaded.Result); return StoredRecordJson.Project<Holon>(loaded.Result);
            }
            var created = await manager.SaveOwnedHolonAsync(root, owner, new CosmicWriteOptions(Guid.NewGuid()),
                saveChildren: false, providerType: authority);
            Assert.IsFalse(created.IsError, created.Message + created.Exception);
            Assert.AreEqual(1, (await Read(id)).Version); checks++;

            foreach (var nested in new[] { false, true })
            foreach (var selected in new[] { false, true })
            {
                var edit = await Read(id); HolonChangeTracking.AcceptChanges(edit, false);
                edit.Description = "forbidden graph edit";
                var child = new Holon { ParentHolonId = id, Name = "denied child" };
                if (explicitChildIdentity) child.Id = Guid.NewGuid();
                var submittedId = child.Id;
                if (nested) edit.Children.Add(child);
                probe.Source = edit;
                var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Revision(edit) });
                var roots = nested ? new IHolon[] { edit } : new IHolon[] { edit, child };
                var beforeCalls = operations.Count;
                if (parentProbe != null) parentProbe.ParentReads = 0;
                var denied = selected
                    ? await manager.SaveOwnedHolonsAsync(roots, writer, options,
                        new Dictionary<Guid, IReadOnlyList<string>> { [id] = new[] { "/Description" } }, providerType: authority)
                    : await manager.SaveOwnedHolonsAsync(roots, writer, options, providerType: authority);
                Assert.IsTrue(denied.IsError); Assert.AreEqual("COSMIC_ADD_CHILDREN_FORBIDDEN", denied.ErrorCode);
                Assert.AreEqual(beforeCalls, operations.Count); Assert.AreEqual(submittedId, child.Id);
                if (parentProbe != null) Assert.AreEqual(1, parentProbe.ParentReads,
                    "An in-plan parent observation must be reused for AddChildren authorization.");
                var unchanged = await Read(id); Assert.AreEqual(1, unchanged.Version);
                Assert.AreEqual("initial", unchanged.Description); checks++;
            }

            if (explicitChildIdentity)
            {
                var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = id, Name = "external denied child" };
                var suppliedId = child.Id; var beforeCalls = operations.Count;
                probe.Source = child; parentProbe!.ParentReads = 0;
                var denied = await manager.SaveOwnedHolonAsync(child, writer, new CosmicWriteOptions(Guid.NewGuid()),
                    saveChildren: false, providerType: authority);
                Assert.IsTrue(denied.IsError); Assert.AreEqual("COSMIC_ADD_CHILDREN_FORBIDDEN", denied.ErrorCode);
                Assert.AreEqual(suppliedId, child.Id); Assert.AreEqual(0, child.Version);
                Assert.AreEqual(beforeCalls, operations.Count); Assert.AreEqual(1, parentProbe.ParentReads);
                Assert.AreEqual(1, (await Read(id)).Version); checks++;
            }

            root.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b =>
                b.SetGrant(HolonPermissionSet.Key(writer), HolonPermission.Write | HolonPermission.AddChildren)).Encode().Result;
            probe.Source = root;
            var grant = await manager.SaveOwnedHolonAsync(root, owner, new CosmicWriteOptions(Guid.NewGuid()),
                saveChildren: false, providerType: authority);
            Assert.IsFalse(grant.IsError, grant.Message + grant.Exception);
            Assert.AreEqual(2, (await Read(id)).Version); checks++;
            if (explicitChildIdentity)
            {
                var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = id, Name = "external permitted child", IsActive = true };
                var suppliedId = child.Id; probe.Source = child; parentProbe!.ParentReads = 0;
                var saved = await manager.SaveOwnedHolonAsync(child, writer, new CosmicWriteOptions(Guid.NewGuid()),
                    saveChildren: false, providerType: authority);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception); Assert.IsTrue(saved.IsSaved);
                Assert.AreEqual(suppliedId, child.Id); ids.Add(child.Id);
                Assert.AreEqual(1, parentProbe.ParentReads);
                var native = await Read(child.Id);
                Assert.AreEqual(1, native.Version); Assert.AreEqual(id, native.ParentHolonId);
                Assert.AreEqual(writer, native.CreatedByAvatarId); Assert.AreEqual(child.Name, native.Name);
                Assert.AreEqual(2, (await Read(id)).Version); checks++;
            }
            Holon? staleParent = null; Holon? lastChild = null;
            foreach (var selected in new[] { false, true })
            {
                var edit = await Read(id); HolonChangeTracking.AcceptChanges(edit, false);
                staleParent = StoredRecordJson.Project<Holon>(edit);
                var version = edit.Version; edit.Description = selected ? "selected graph" : "full graph";
                if (selected) edit.Name = "excluded local parent name";
                var child = new Holon { ParentHolonId = id, Name = selected ? "selected child" : "full child", IsActive = true };
                if (explicitChildIdentity) child.Id = Guid.NewGuid();
                var submittedId = child.Id;
                if (selected) edit.Children.Add(child);
                probe.Source = edit;
                var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Revision(edit) });
                if (parentProbe != null) parentProbe.ParentReads = 0;
                var saved = selected
                    ? await manager.SaveOwnedHolonsAsync(new IHolon[] { edit }, writer, options,
                        new Dictionary<Guid, IReadOnlyList<string>> { [id] = new[] { "/Description" } },
                        saveChildren: true, providerType: authority)
                    : await manager.SaveOwnedHolonsAsync(new IHolon[] { edit, child }, writer, options,
                        saveChildren: false, providerType: authority);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception); Assert.IsTrue(saved.IsSaved);
                Assert.AreNotEqual(Guid.Empty, child.Id); ids.Add(child.Id);
                if (explicitChildIdentity) Assert.AreEqual(submittedId, child.Id);
                if (parentProbe != null) Assert.AreEqual(1, parentProbe.ParentReads,
                    "A stored parent included in a permitted graph is read only once for authorization.");
                var nativeParent = await Read(id); var nativeChild = await Read(child.Id);
                Assert.AreEqual(version + 1, nativeParent.Version); Assert.AreEqual(edit.Description, nativeParent.Description);
                Assert.AreEqual("native parent", nativeParent.Name); Assert.AreEqual(id, nativeChild.ParentHolonId);
                Assert.AreEqual(1, nativeChild.Version); Assert.AreEqual(writer, nativeChild.CreatedByAvatarId);
                Assert.AreEqual(child.Name, nativeChild.Name);
                if (selected) Assert.AreEqual("excluded local parent name", edit.Name);
                lastChild = nativeChild; checks++;
            }

            var beforeParent = await Read(id); var beforeChild = await Read(lastChild!.Id);
            staleParent!.Description = "stale graph must roll back"; lastChild.Name = "child must not change";
            probe.Source = staleParent;
            var stale = await manager.SaveOwnedHolonsAsync(new IHolon[] { staleParent, lastChild }, writer,
                new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?>
                    { [id] = Revision(staleParent), [lastChild.Id] = Revision(lastChild) }),
                new Dictionary<Guid, IReadOnlyList<string>> { [id] = new[] { "/Description" } },
                saveChildren: false, providerType: authority);
            Assert.IsTrue(stale.IsError); Assert.AreEqual(CosmicConcurrencyException.Code, stale.ErrorCode);
            Assert.AreEqual(StoredRecordJson.SerializeRuntime(beforeParent), StoredRecordJson.SerializeRuntime(await Read(id)));
            Assert.AreEqual(StoredRecordJson.SerializeRuntime(beforeChild), StoredRecordJson.SerializeRuntime(await Read(lastChild.Id)));
            checks++; Assert.AreEqual(explicitChildIdentity ? 11 : 9, checks); complete = true;
        }
        finally
        {
            if (active) Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            (provider as IDisposable)?.Dispose();
            if (complete)
            {
                if (authority == ProviderType.MongoDBOASIS)
                {
                    await mongo.DropDatabaseAsync(database);
                    using var names = await mongo.ListDatabaseNamesAsync();
                    Assert.IsFalse((await names.ToListAsync()).Contains(database));
                }
                if (authority == ProviderType.Neo4jOASIS)
                {
                    var ownedIds = ids.Select(value => value.ToString("D")).ToArray();
                    var ops = operations.Select(value => value.ToString("D")).ToArray();
                    const string owned = "MATCH (n) WHERE (n:Holon AND n.Id IN $ownedIds) OR " +
                        "(n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId IN $ownedIds) OR " +
                        "(n:CosmicReplicaHead AND n.HolonId IN $ownedIds) OR " +
                        "((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $ops) ";
                    await (await inspection.RunAsync(owned + "DETACH DELETE n", new { ownedIds, ops })).ConsumeAsync();
                    Assert.AreEqual(0L, (await (await inspection.RunAsync(owned + "RETURN count(n) AS n", new { ownedIds, ops })).SingleAsync())["n"].As<long>());
                }
                Assert.AreEqual(temp, Path.GetDirectoryName(Path.GetFullPath(work)));
                Directory.Delete(work, true); Assert.IsFalse(Directory.Exists(work)); checks++;
                if (customProperties)
                {
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName,
                        protectedMetadata, checks, successfulHolonWrites = 5, nativeStorageMappingVerified = true,
                        exactIntegerVerified = true, storeAsJsonStringVerified = true, nullableScalarVerified = true,
                        wholeCollectionPropertyVerified = true, unselectedCustomPreservedVerified = true,
                        historicalReceiptVerified = true, staleConditionVerified = true,
                        noOrdinaryWriteDowngradeVerified = true, exactCleanupVerified = true }));
                }
                else if (roleComposition)
                {
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 9, typedNativeGraphCreatedVerified = true,
                        scalarSelectionPreservesRolesVerified = true, partialGraphRefusedVerified = true,
                        partialScalarSavePreservesRolesVerified = true, explicitGraphRoleUpdateVerified = true,
                        unselectedScalarPreservedVerified = true, comparisonAndFlagsPreservedVerified = true,
                        historicalReplayPreservesNativeRolesVerified = true, noOrdinarySelectedDowngradeVerified = true,
                        exactCleanupVerified = true }));
                }
                else
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                    successfulHolonWrites = explicitChildIdentity ? 7 : 6,
                    existingParentPermissionVerified = true, deniedBeforeMutationVerified = true,
                    fullGraphVerified = true, mixedSelectedGraphVerified = true, unselectedParentPreservedVerified = true,
                    nativeChildVerified = true, staleGraphAtomicityVerified = true, exactCleanupVerified = true,
                    explicitChildIdentityVerified = explicitChildIdentity,
                    boundedParentReadsVerified = explicitChildIdentity }));
            }
        }
    }
}
