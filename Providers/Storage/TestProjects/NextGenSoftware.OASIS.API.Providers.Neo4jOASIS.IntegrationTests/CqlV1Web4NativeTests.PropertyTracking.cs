using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Bson;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.DNA;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task PropertyTrackingUsesNativeAcknowledgementWithoutLosingInflightIntent(string providerName)
        => RunPropertyNativeFixture(providerName, false);

    private async Task RunPropertyNativeFixture(string providerName, bool propertySelection, bool propertyStatus = false)
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var authority = Enum.Parse<ProviderType>(providerName);
        var actor = Guid.NewGuid(); var id = Guid.NewGuid();
        var database = "cosmic_property_" + id.ToString("N");
        var workRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!);
        var work = Path.Combine(workRoot, database);
        Assert.IsFalse(Directory.Exists(work));
        Directory.CreateDirectory(work);
        var sqlite = Path.Combine(work, "property.sqlite");
        var reportRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT")!);
        var report = Path.Combine(reportRoot, "TestResults/Neo4jCurrent/" + providerName + (propertyStatus ? "-property-status-native.json" : propertySelection ? "-property-selection-native.json" : "-property-tracking-native.json"));
        var operations = new HashSet<Guid>(); var complete = false; var checks = 0;
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database), "Never reset an existing database.");
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var absent = await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS n",
            new { id = id.ToString("D") })).SingleAsync();
        Assert.AreEqual(0L, absent["n"].As<long>());
        IOASISStorageProvider provider = authority switch
        {
            ProviderType.MongoDBOASIS => new MongoProvider(mongoUri, database) { IsVersionControlEnabled = false },
            ProviderType.SQLLiteDBOASIS => new SqliteProvider("Data Source=" + sqlite + ";Pooling=False") { IsVersionControlEnabled = false },
            ProviderType.Neo4jOASIS => new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false },
            _ => throw new InvalidOperationException("Only the three isolated native stores are admitted.")
        };
        var active = false;
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message); active = true;
            var source = new Holon { Id = id, Name = "original", Description = "description", IsActive = true,
                MetaData = new() { ["original"] = 9007199254740993L } };
            var boundary = DispatchProxy.Create<IRecoveryMutationBoundary, SaveDemoBoundary>();
            var probe = (SaveDemoBoundary)(object)boundary;
            probe.Target = provider; probe.Source = source;
            probe.BeforeResource = async (operation, recordIds) =>
            {
                CollectionAssert.AreEqual(new[] { id }, recordIds.ToArray());
                operations.Add(operation);
                await File.WriteAllTextAsync(Path.Combine(work, "fixture.json"),
                    JsonSerializer.Serialize(new { database, sqlite, id, actor, operations, stage = "before-native-resource" }));
            };
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = HyperDriveModes.V2, StorageProviders = new() { LogSwitchingProviders = false } } };
            var registry = new ProviderManager(null, dna);
            Assert.IsTrue(registry.RegisterProvider(boundary));
            Assert.IsFalse(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(false, false, false), authority).IsError);
            var manager = new HolonManager(null, dna, registry);
            manager.PreparationEncryptionPolicy = new HolonMetadataEncryptionPolicy(
                "property-tracking", "fixture-key-v1", new RecoveryPreparationKeys());

            async Task Save()
            {
                var version = source.Version;
                var revision = CosmicConcurrency.GetProviderRevisionId(source, authority);
                var saved = await manager.SaveOwnedHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()),
                    saveChildren: false, providerType: authority);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
                Assert.AreEqual(version + 1, source.Version);
                Assert.AreNotEqual(revision, CosmicConcurrency.GetProviderRevisionId(source, authority));
                checks++;
            }
            HolonPropertyValues Property(string path)
            {
                var result = HolonChangeTracking.GetPropertyValues(source, path);
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                return result.Result;
            }
            void Mark(string path) => Assert.IsFalse(HolonChangeTracking.SetPropertyModified(source, path).IsError);

            if (propertySelection)
            {
                source.MetaData["nullable"] = null!;
                source.MetaData["branch"] = new Dictionary<string, object> { ["selected"] = 1L, ["sibling"] = 2L };
            }
            await Save();
            if (propertyStatus)
            {
                async Task<string> InspectReceipt(Guid operation)
                {
                    if (authority == ProviderType.MongoDBOASIS)
                    {
                        var row = await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicWriteReceipt")
                            .Find(new BsonDocument("_id", operation.ToString("D"))).SingleOrDefaultAsync();
                        return row == null ? null! : row.ToJson();
                    }
                    if (authority == ProviderType.Neo4jOASIS)
                    {
                        var rows = await (await inspection.RunAsync(
                            "MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN properties(r) AS values",
                            new { operation = operation.ToString("D") })).ToListAsync();
                        Assert.IsTrue(rows.Count <= 1);
                        return rows.Count == 0 ? null! : JsonSerializer.Serialize(
                            new SortedDictionary<string, object>(rows[0]["values"].As<Dictionary<string, object>>(), StringComparer.Ordinal));
                    }
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + sqlite + ";Mode=ReadOnly;Pooling=False");
                    await connection.OpenAsync();
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT RequestHash,ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id";
                    command.Parameters.AddWithValue("$id", operation.ToString("D"));
                    using var reader = await command.ExecuteReaderAsync();
                    if (!await reader.ReadAsync()) return null!;
                    var value = reader.GetString(0) + "\n" + reader.GetString(1);
                    Assert.IsFalse(await reader.ReadAsync());
                    return value;
                }
                checks += await VerifySelectedNativeStatus(provider, authority, source, actor, operations, work, InspectReceipt);
                Assert.AreEqual(8, checks);
            }
            else if (propertySelection)
            {
                checks += await VerifySelectedNativeWrites(provider, manager, authority, source, actor, operations, work);
                Assert.AreEqual(9, checks);
            }
            else
            {
            Assert.IsFalse(HolonChangeTracking.HasChanges(source));
            Assert.AreEqual("\"original\"", Property("/Name").OriginalJson);
            Assert.IsFalse(HolonChangeTracking.SetCurrentPropertyValue(source, "/Name", "discard this").IsError);
            Assert.IsFalse(HolonChangeTracking.SetCurrentPropertyValue(source, "/Description", "keep this").IsError);
            Assert.IsFalse(HolonChangeTracking.SetCurrentPropertyValue(source, "/MetaData/temporary", 9007199254740993L).IsError);
            Assert.IsFalse(HolonChangeTracking.SetPropertyModified(source, "/Name", false).IsError);
            Assert.AreEqual("original", source.Name); Assert.AreEqual("keep this", source.Description);
            Assert.IsTrue(HolonChangeTracking.HasChanges(source));
            Assert.IsFalse(HolonChangeTracking.ResetPropertyValue(source, "/Description").IsError);
            Assert.IsFalse(HolonChangeTracking.ResetPropertyValue(source, "/MetaData/temporary").IsError);
            Assert.IsFalse(source.MetaData.ContainsKey("temporary"));
            Assert.IsFalse(HolonChangeTracking.HasChanges(source));
            var beforeResetNoOp = probe.SuccessfulMutations;
            var beforeResetPreparation = probe.PreparationIntents.Count;
            var resetNoOp = await manager.SaveOwnedHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()),
                saveChildren: false, providerType: authority);
            Assert.IsFalse(resetNoOp.IsError, resetNoOp.Message + resetNoOp.Exception);
            Assert.IsFalse(resetNoOp.IsSaved);
            Assert.AreEqual(beforeResetNoOp, probe.SuccessfulMutations);
            Assert.AreEqual(beforeResetPreparation, probe.PreparationIntents.Count); checks++;
            Mark("/Name");
            Assert.IsTrue(Property("/Name").IsExplicitlyModified);
            Assert.IsFalse(Property("/Name").IsValueModified);
            Assert.AreEqual(0, HolonChangeTracking.PreviewValueChanges(source).Result.Count);
            await Save();
            Assert.IsFalse(Property("/Name").IsModified); Assert.IsFalse(HolonChangeTracking.HasChanges(source)); checks++;

            Mark("/Name");
            probe.AfterSuccessfulMutation = () =>
            {
                Mark("/Name"); Mark("/Description");
                return Task.CompletedTask;
            };
            await Save();
            Assert.IsTrue(Property("/Name").IsExplicitlyModified);
            Assert.IsTrue(Property("/Description").IsExplicitlyModified);
            Assert.IsTrue(HolonChangeTracking.HasChanges(source)); checks++;

            probe.AfterSuccessfulMutation = () =>
            {
                Assert.IsFalse(HolonChangeTracking.SetCurrentPropertyValue(source, "/Name", "late edit").IsError);
                Mark("/Name");
                return Task.CompletedTask;
            };
            await Save();
            probe.AfterSuccessfulMutation = null;
            Assert.AreEqual("\"original\"", Property("/Name").OriginalJson);
            Assert.AreEqual("\"late edit\"", Property("/Name").CurrentJson);
            Assert.IsTrue(Property("/Name").IsExplicitlyModified);
            Assert.IsFalse(Property("/Description").IsExplicitlyModified);
            var stored = await provider.LoadHolonAsync(id, false, false, 0, true, false, 0);
            Assert.IsFalse(stored.IsError, stored.Message + stored.Exception);
            Assert.AreEqual("original", stored.Result.Name, "The in-flight edit must not alter the dispatched native write."); checks++;

            var mutations = probe.SuccessfulMutations;
            var forbidden = await manager.SaveOwnedHolonAsync(source, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
                saveChildren: false, providerType: authority);
            Assert.IsTrue(forbidden.IsError);
            Assert.AreEqual("COSMIC_WRITE_FORBIDDEN", forbidden.ErrorCode);
            Assert.AreEqual(mutations, probe.SuccessfulMutations);
            Assert.IsTrue(Property("/Name").IsExplicitlyModified); checks++;

            Assert.IsFalse(HolonChangeTracking.SetCurrentPropertyValue<object>(source, "/MetaData/added", null!).IsError);
            source.MetaData.Remove("original");
            Assert.IsFalse(Property("/MetaData/added").OriginalExists);
            Assert.IsTrue(Property("/MetaData/added").CurrentExists);
            Assert.AreEqual("null", Property("/MetaData/added").CurrentJson);
            Assert.IsTrue(Property("/MetaData/original").OriginalExists);
            Assert.IsFalse(Property("/MetaData/original").CurrentExists);
            await Save();
            Assert.IsFalse(HolonChangeTracking.HasChanges(source));
            Assert.IsFalse(Property("/Name").IsModified);
            Assert.AreEqual("\"late edit\"", Property("/Name").OriginalJson);
            stored = await provider.LoadHolonAsync(id, false, false, 0, true, false, 0);
            Assert.IsFalse(stored.IsError, stored.Message + stored.Exception);
            Assert.AreEqual("late edit", stored.Result.Name);
            Assert.IsTrue(stored.Result.MetaData.ContainsKey("added"));
            Assert.IsNull(stored.Result.MetaData["added"]);
            Assert.IsFalse(stored.Result.MetaData.ContainsKey("original"));
            HolonChangeTracking.AcceptChanges(stored.Result, false);
            Assert.IsFalse(HolonChangeTracking.GetPropertyValues(stored.Result, "/Name").Result.IsExplicitlyModified);
            Assert.IsFalse(HolonChangeTracking.HasChanges(stored.Result)); checks++;

            var preparations = probe.PreparationIntents.Count;
            var noOp = await manager.SaveOwnedHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()),
                saveChildren: false, providerType: authority);
            Assert.IsFalse(noOp.IsError, noOp.Message + noOp.Exception); Assert.IsFalse(noOp.IsSaved);
            Assert.AreEqual(5, probe.SuccessfulMutations);
            Assert.AreEqual(5, probe.MutationIntents.Distinct().Count());
            Assert.AreEqual(preparations, probe.PreparationIntents.Count); checks++;
            Assert.AreEqual(12, checks);
            }
            complete = true;
        }
        finally
        {
            if (active) Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            (provider as IDisposable)?.Dispose();
            if (complete)
            {
                if (authority == ProviderType.MongoDBOASIS)
                {
                    await client.DropDatabaseAsync(database);
                    using var names = await client.ListDatabaseNamesAsync();
                    Assert.IsFalse((await names.ToListAsync()).Contains(database));
                }
                if (authority == ProviderType.Neo4jOASIS)
                {
                    await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id = id.ToString("D") })).ConsumeAsync();
                    await (await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) DETACH DELETE v",
                        new { id = id.ToString("D") })).ConsumeAsync();
                    var ops = operations.Select(value => value.ToString("D")).ToArray();
                    await (await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $ops DETACH DELETE r", new { ops })).ConsumeAsync();
                    var remaining = await (await inspection.RunAsync(
                        "MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicWriteReceipt AND n.OperationId IN $ops) OR " +
                        "(n:OasisRecordVersion AND n.RecordId=$id AND n.Kind='Holon') RETURN count(n) AS n",
                        new { id = id.ToString("D"), ops })).SingleAsync();
                    Assert.AreEqual(0L, remaining["n"].As<long>());
                }
                Assert.AreEqual(workRoot, Path.GetDirectoryName(Path.GetFullPath(work)));
                Directory.Delete(work, true);
                Assert.IsFalse(Directory.Exists(work));
                if (propertyStatus)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new {
                        provider = providerName, checks, successfulHolonWrites = 3,
                        absentOutcomeVerified = true, committedOutcomeVerified = true,
                        historicalOutcomeVerified = true, missingValueOutcomeVerified = true,
                        changedMaskStatusRefused = true, wholeStatusRefused = true, actorMismatchRefused = true,
                        receiptsUnchangedVerified = true, holonsUnchangedByReadsVerified = true,
                        callerUnchangedVerified = true, exactCleanupVerified = true }));
                else if (propertySelection)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new {
                        provider = providerName, checks, successfulHolonWrites = 4,
                        selectedScalarVerified = true, selectedMetadataVerified = true,
                        unselectedNativeValuesVerified = true, receiptReplayVerified = true,
                        changedMaskRefused = true, staleRevisionRefused = true, invalidFieldRefused = true,
                        callerUnchangedVerified = true, foreignHostWriteRefused = true, exactCleanupVerified = true }));
                else
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new {
                    provider = providerName, checks, successfulNativeMutations = 5,
                    currentValueEditingVerified = true, propertyResetVerified = true,
                    explicitEqualValueWriteVerified = true, inflightIntentVerified = true,
                    foreignWriteRefused = true, originalCurrentVerified = true,
                    noOpVerified = true, exactCleanupVerified = true }));
            }
        }
    }
}
