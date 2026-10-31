using System.Text.Json;
using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmicTransferFixtureReconciliationTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task InspectAndRemoveOnlyAcknowledgedNumericAssertionFixture()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var workspace = new DirectoryInfo(AppContext.BaseDirectory);
        while (workspace != null && !File.Exists(Path.Combine(workspace.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) workspace = workspace.Parent;
        Assert.NotNull(workspace);
        var directory = Path.GetFullPath(Path.Combine(workspace.FullName, "TestResults/HyperDriveMongoSqliteCurrent"));
        using var lease = File.Open(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var sqlitePath = Path.Combine(directory, "owned.db");
        Assert.True(File.Exists(sqlitePath), "The exact preserved fixture must exist; no database may be created here.");
        const string databaseName = "hyperdrive_mongo_sqlite_fixture";
        var root = Guid.Parse("63076020-a13b-4c21-a3ea-5cdce86c9662");
        var child = Guid.Parse("6acf58e8-4e71-4019-97a8-11721465e4c0");
        var expected = new HashSet<Guid> { root, child };
        var provider = new MongoProvider(uri, databaseName); // Reuse canonical BSON registration; never submit a mutation.
        var client = new MongoClient(uri); var database = client.GetDatabase(databaseName);
        try
        {
            using var databases = await client.ListDatabaseNamesAsync();
            Assert.Contains(databaseName, await databases.ToListAsync());
            var records = await database.GetCollection<NativeHolon>("Holon").Find(FilterDefinition<NativeHolon>.Empty).ToListAsync();
            Assert.Equal(2, records.Count); Assert.True(expected.SetEquals(records.Select(row => row.HolonId)));
            Assert.Equal("owned transfer root", records.Single(row => row.HolonId == root).Name);
            Assert.Equal("owned transfer child", records.Single(row => row.HolonId == child).Name);
            Assert.Equal(root, records.Single(row => row.HolonId == child).ParentHolonId);
            using (var collections = await database.ListCollectionNamesAsync())
                foreach (var collection in await collections.ToListAsync())
                    if (collection != "Holon")
                        Assert.Equal(0L, await database.GetCollection<MongoDB.Bson.BsonDocument>(collection)
                            .CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty));
            using (var connection = new SqliteConnection($"Data Source={sqlitePath};Mode=ReadOnly;Pooling=False"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, ParentHolonId, MetaDataJson, VersionId FROM Holon";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var seen = new HashSet<Guid>();
                    while (await reader.ReadAsync())
                    {
                        var id = Guid.Parse(reader.GetString(0)); Assert.True(expected.Contains(id)); Assert.True(seen.Add(id));
                        using var metadata = JsonDocument.Parse(reader.GetString(3));
                        if (id == root)
                        {
                            Assert.Equal("owned transfer root", reader.GetString(1));
                            Assert.Equal(2.5m, metadata.RootElement.GetProperty("amount").GetDecimal());
                        }
                        else
                        {
                            Assert.Equal("owned transfer child", reader.GetString(1));
                            Assert.Equal(root, Guid.Parse(reader.GetString(2)));
                            Assert.Equal("child transfer evidence", metadata.RootElement.GetProperty("purpose").GetString());
                        }
                        output.WriteLine($"Inspected original SQLite row {id:D}; name={reader.GetString(1)}; logical version={reader.GetString(4)}.");
                    }
                    Assert.True(expected.SetEquals(seen));
                }
                foreach (var table in new[] { "Avatar", "AvatarDetail", "CosmicWriteReceipt" })
                {
                    command.CommandText = "SELECT COUNT(*) FROM \"" + table + "\"";
                    Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
                }
            }
            foreach (var record in records)
                output.WriteLine($"Inspected original Mongo row {record.HolonId:D}; name={record.Name}; logical version={record.VersionId:D}.");
            // Both stores independently confirm the exact owned originals. No save/replay or broad reset.
            await client.DropDatabaseAsync(databaseName);
            foreach (var path in new[] { sqlitePath, sqlitePath + "-wal", sqlitePath + "-shm" })
            {
                var resolved = Path.GetFullPath(path);
                Assert.Equal(directory, Path.GetDirectoryName(resolved));
                if (File.Exists(resolved)) File.Delete(resolved);
            }
            using var remaining = await client.ListDatabaseNamesAsync();
            Assert.DoesNotContain(databaseName, await remaining.ToListAsync()); Assert.False(File.Exists(sqlitePath));
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
