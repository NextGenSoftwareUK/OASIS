using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmicCrossProviderFixtureReconciliationTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task InspectExactRejectedPortableContainerFixtureBeforeCleanup()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var workspace = new DirectoryInfo(AppContext.BaseDirectory);
        while (workspace != null && !File.Exists(Path.Combine(workspace.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) workspace = workspace.Parent;
        Assert.NotNull(workspace);
        var directory = Path.GetFullPath(Path.Combine(workspace!.FullName, "TestResults/HyperDriveMongoSqliteCurrent"));
        using var lease = File.Open(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var path = Path.GetFullPath(Path.Combine(directory, "replica-cross-provider-current.db")); Assert.True(File.Exists(path));
        var actualHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path)));
        if (actualHash == "9D7F790C9D4320B878DF7A920F8CE83A116DC77E6C196D6B2EEC3FF9D417245B")
        { await InspectExactRoleFailureFixture(uri, directory, path, actualHash); return; }
        if (actualHash == "5F7598CC64DC6D043A03FD543B505C24ACB2267700E35B3BECBF9C1B23997A40")
        { await InspectExactConflictCodeFixture(uri, directory, path, actualHash); return; }
        if (actualHash == "98531B3BE5A46165EA5D14F1BA8F34754F7D0DAD94637F7FDF91A621FD3A67D2")
        { await InspectCompletedSourceLedger(uri, directory, path, actualHash); return; }
        const string expectedHash = "694A5AFF031EA54BFEFC9769F2109607A6B320339B22E4F2863866715F4B7ECA";
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        var root = Guid.Parse("ca883abd-c984-49c7-9005-71bbf442ccb3"); var child = Guid.Parse("0a38c989-607c-4109-af3c-c77af6a2a61a");
        using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
        {
            await connection.OpenAsync(); using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check"; Assert.Equal("ok", (string)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT Id,Name,ParentHolonId,Version,VersionId,ProviderRevisionId FROM Holon";
            var seen = new HashSet<Guid>();
            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    var id = Guid.Parse(reader.GetString(0)); Assert.True(seen.Add(id)); Assert.True(id == root || id == child);
                    Assert.Equal(id == root ? "owned cross root" : "owned cross child", reader.GetString(1)); Assert.Equal(1, reader.GetInt32(3));
                    if (id == child) Assert.Equal(root, Guid.Parse(reader.GetString(2)));
                    Assert.NotEqual(Guid.Empty, Guid.Parse(reader.GetString(4))); Assert.NotEqual(Guid.Empty, Guid.Parse(reader.GetString(5)));
                }
            Assert.Equal(2, seen.Count);
            command.CommandText = "SELECT OperationId,RequestHash,ResultJson FROM CosmicWriteReceipt";
            using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync()); Assert.Equal("facebc01-d9a7-427d-b22d-c9454467c0d5", reader.GetString(0));
                Assert.Equal("9A4518FA90FD8AADC0D9FE3E822DC1636AAA19640909D11428DF8465958C0ABB", reader.GetString(1));
                var receipt = JObject.Parse(reader.GetString(2)); Assert.Equal(1, receipt.Value<int>("format"));
                Assert.Equal(root.ToString("D"), Assert.Single(receipt["roots"]!).ToString());
                Assert.True(seen.SetEquals(receipt["nodes"]!.Select(node => Guid.Parse(node.Value<string>("id")!))));
                Assert.False(await reader.ReadAsync());
            }
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            var tables = new List<string>(); using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            foreach (var table in tables.Where(table => table != "Holon" && table != "CosmicWriteReceipt"))
            { command.CommandText = "SELECT COUNT(*) FROM \"" + table.Replace("\"", "\"\"") + "\""; Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!); }
        }
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        const string name = "cosmic_cross_provider_fixture";
        var client = new MongoClient(uri); var database = client.GetDatabase(name);
        using var cursor = await database.ListCollectionNamesAsync(); var collections = await cursor.ToListAsync();
        Assert.Contains("CosmicOwnedFixture", collections);
        foreach (var collection in collections)
            Assert.Equal(0, await database.GetCollection<BsonDocument>(collection).WithReadConcern(ReadConcern.Majority)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        output.WriteLine($"Verified exact source SHA256={expectedHash}, two source version-one rows, one bound source receipt; majority-visible target collections empty. No mutation replayed.");
        await client.DropDatabaseAsync(name);
        foreach (var candidate in new[] { path, path + "-wal", path + "-shm" })
        {
            var resolved = Path.GetFullPath(candidate);
            Assert.Equal(directory, Path.GetDirectoryName(resolved));
            if (File.Exists(resolved)) File.Delete(resolved);
        }
    }
    private async Task InspectExactRoleFailureFixture(string uri, string directory, string path, string expectedHash)
    {
        using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
        {
            await connection.OpenAsync(); using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check"; Assert.Equal("ok", (string)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM Holon WHERE Version=1"; Assert.Equal(2L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM CosmicWriteReceipt"; Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT OperationId,DeliveryState,AttemptCount,LastErrorCode FROM CosmicHolonOutbox";
            using var reader = await command.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync());
            Assert.Equal("7a663dfa-0751-49d9-a25f-d29c47659ed8", reader.GetString(0));
            Assert.Equal("Pending", reader.GetString(1)); Assert.Equal(1, reader.GetInt32(2));
            Assert.Equal("COSMIC_REPLICA_WRITE_FAILED", reader.GetString(3)); Assert.False(await reader.ReadAsync());
        }
        var client = new MongoClient(uri); const string name = "cosmic_cross_provider_fixture";
        var database = client.GetDatabase(name); using var cursor = await database.ListCollectionNamesAsync();
        var collections = await cursor.ToListAsync(); Assert.Contains("CosmicOwnedFixture", collections);
        foreach (var collection in collections)
            Assert.Equal(0L, await database.GetCollection<BsonDocument>(collection).WithReadConcern(ReadConcern.Majority)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        var archive = Path.GetFullPath(Path.Combine(directory, "../CosmicMongoCurrent/cross-provider-role-source.db"));
        Assert.False(File.Exists(archive)); File.Copy(path, archive);
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(archive))));
        var cleanup = new[] { path, path + "-wal", path + "-shm" }.Select(Path.GetFullPath).ToArray();
        Assert.All(cleanup, candidate => Assert.Equal(directory, Path.GetDirectoryName(candidate)));
        await client.DropDatabaseAsync(name);
        foreach (var candidate in cleanup) if (File.Exists(candidate)) File.Delete(candidate);
        output.WriteLine($"Exact source {expectedHash} archived; source receipt and pending obligation retained in archive, majority-visible target empty. No operation replayed.");
    }

    private async Task InspectCompletedSourceLedger(string uri, string directory, string path, string expectedHash)
    {
        var root = Guid.Parse("178e95c2-0a4e-4f10-9af5-9e384bee2ac9");
        var child = Guid.Parse("643b39eb-5685-4c5a-afb5-8f97870dea92");
        var operations = new HashSet<string> { "53ad311d-bb4c-449b-a19e-784350d24524", "84c07707-4d89-47eb-b55c-ce770a9af873",
            "9ae11844-7fec-4228-b0ab-a248f40f5ba3", "4c011651-c4b4-43a3-8fc0-1031899659ac", "5f35d9c2-414f-4937-b28c-e5049bfd7268" };
        using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
        {
            await connection.OpenAsync(); using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check"; Assert.Equal("ok", (string)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT Id,Name,ParentHolonId,Version FROM Holon";
            using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync()); Assert.Equal(child, Guid.Parse(reader.GetString(0)));
                Assert.Equal("owned cross child", reader.GetString(1)); Assert.Equal(root, Guid.Parse(reader.GetString(2)));
                Assert.Equal(1, reader.GetInt32(3)); Assert.False(await reader.ReadAsync());
            }
            command.CommandText = "SELECT OperationId,ResultJson FROM CosmicWriteReceipt";
            var ledger = new SortedDictionary<int, JObject>(); var seen = new HashSet<string>();
            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    var operation = reader.GetString(0); Assert.Contains(operation, operations); Assert.True(seen.Add(operation));
                    var receipt = JObject.Parse(reader.GetString(1)); Assert.Equal(1, receipt.Value<int>("format"));
                    Assert.Equal(root.ToString("D"), Assert.Single(receipt["roots"]!).ToString());
                    var nodes = receipt["nodes"]!.ToArray(); var rootNode = nodes.Single(node => Guid.Parse(node.Value<string>("id")!) == root);
                    var record = JObject.Parse(rootNode.Value<string>("record")!); ledger.Add(record.Value<int>("Version"), record);
                    Assert.StartsWith("owned cross ", record.Value<string>("Name"));
                    Assert.Equal(record.Value<int>("Version") == 1 ? 2 : 1, nodes.Length);
                }
            Assert.True(seen.SetEquals(operations)); Assert.Equal(new[] { 1, 2, 3, 4, 5 }, ledger.Keys);
            for (var version = 1; version <= 5; version++)
            {
                Assert.Equal(root, Guid.Parse(ledger[version].Value<string>("Id")!));
                Assert.Equal(version == 1 ? Guid.Empty : Guid.Parse(ledger[version - 1].Value<string>("VersionId")!), Guid.Parse(ledger[version].Value<string>("PreviousVersionId")!));
                if (version >= 4) Assert.True(ledger[version].Value<DateTime>("DeletedDate") > DateTime.MinValue);
            }
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            var tables = new List<string>(); using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            foreach (var table in tables.Where(table => table != "Holon" && table != "CosmicWriteReceipt"))
            { command.CommandText = "SELECT COUNT(*) FROM \"" + table.Replace("\"", "\"\"") + "\""; Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!); }
        }
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        using var databases = await new MongoClient(uri).ListDatabaseNamesAsync();
        Assert.DoesNotContain("cosmic_cross_provider_fixture", await databases.ToListAsync());
        output.WriteLine($"Verified exact completed SQLite source SHA256={expectedHash}: one unchanged child, five coherent versioned receipts including soft/hard tombstones, target Mongo fixture already absent. No operation replayed.");
        var cleanup = new[] { path, path + "-wal", path + "-shm" }.Select(Path.GetFullPath).ToArray();
        Assert.All(cleanup, candidate => Assert.Equal(directory, Path.GetDirectoryName(candidate)));
        foreach (var candidate in cleanup) if (File.Exists(candidate)) File.Delete(candidate);
    }

    private async Task InspectExactConflictCodeFixture(string uri, string directory, string path, string expectedHash)
    {
        var root = Guid.Parse("43c09318-5893-41a4-9e6a-24d4f8536ef1"); var child = Guid.Parse("ff2e4fb2-db42-4ac5-9f2b-45f87ae49794");
        var operation = Guid.Parse("4e96a051-aac5-4ffe-9948-eb824a3dbb7f"); var ids = new HashSet<Guid> { root, child };
        IReadOnlyList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> targetAck;
        using (var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
        {
            await connection.OpenAsync(); using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check"; Assert.Equal("ok", (string)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT Id,Name,Version FROM Holon"; var seen = new HashSet<Guid>();
            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    var id = Guid.Parse(reader.GetString(0)); Assert.Contains(id, ids); Assert.True(seen.Add(id));
                    Assert.Equal(id == root ? "owned cross root" : "owned cross child", reader.GetString(1)); Assert.Equal(1, reader.GetInt32(2));
                }
            Assert.True(seen.SetEquals(ids)); command.CommandText = "SELECT OperationId,ResultJson FROM CosmicWriteReceipt";
            using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync()); Assert.Equal(operation, Guid.Parse(reader.GetString(0)));
                targetAck = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read(reader.GetString(1));
                Assert.Equal(root, Assert.Single(targetAck).Id); Assert.Equal(child, Assert.Single(targetAck[0].Children).Id);
                Assert.False(await reader.ReadAsync());
            }
            command.CommandText = "SELECT HolonId,SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,HardDeleted FROM CosmicReplicaHead";
            seen.Clear(); using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    var id = Guid.Parse(reader.GetString(0)); Assert.Contains(id, ids); Assert.True(seen.Add(id));
                    Assert.Equal("owned-cross-provider-MongoDBOASIS", reader.GetString(1)); Assert.Equal("MongoDBOASIS", reader.GetString(2));
                    Assert.Equal(Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(reader.GetString(3)))), reader.GetString(4));
                    Assert.Equal(0L, reader.GetInt64(5));
                }
            Assert.True(seen.SetEquals(ids));
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            var tables = new List<string>(); using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            foreach (var table in tables.Where(table => table != "Holon" && table != "CosmicWriteReceipt" && table != "CosmicReplicaHead"))
            { command.CommandText = "SELECT COUNT(*) FROM \"" + table.Replace("\"", "\"\"") + "\""; Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!); }
        }
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        const string name = "cosmic_cross_provider_fixture"; var client = new MongoClient(uri); var database = client.GetDatabase(name);
        var rows = await database.GetCollection<BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(); Assert.Equal(2, rows.Count);
        Assert.True(ids.SetEquals(rows.Select(row => Guid.Parse(row["HolonId"].AsString))));
        Assert.Equal(3, rows.Single(row => row["HolonId"].AsString == root.ToString("D"))["Version"].AsInt32);
        Assert.Equal(1, rows.Single(row => row["HolonId"].AsString == child.ToString("D"))["Version"].AsInt32);
        var receipts = await database.GetCollection<BsonDocument>("CosmicWriteReceipt").WithReadConcern(ReadConcern.Majority)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(); Assert.Equal(3, receipts.Count);
        var initial = Assert.Single(receipts.Where(row => Guid.Parse(row["_id"].AsString) == operation));
        var sourceAck = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read(initial["ResultJson"].AsString);
        var sourceRoot = Assert.Single(sourceAck);
        var request = new NextGenSoftware.OASIS.API.Core.Interfaces.CommittedHolonReplicaRequest(operation,
            "owned-cross-provider-MongoDBOASIS", NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS, sourceRoot.CreatedByAvatarId,
            NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save, sourceAck);
        var plan = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.CommittedHolonReplicaPlan.Create(request, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS);
        plan.ValidateAcknowledgedRecord(targetAck[0]); plan.ValidateAcknowledgedRecord(Assert.Single(targetAck[0].Children));
        using var collectionCursor = await database.ListCollectionNamesAsync();
        foreach (var collection in (await collectionCursor.ToListAsync()).Where(collection => collection != "Holon" && collection != "CosmicWriteReceipt"))
            Assert.Equal(0, await database.GetCollection<BsonDocument>(collection).WithReadConcern(ReadConcern.Majority).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        output.WriteLine($"Verified exact conflict-code fixture SHA256={expectedHash}: Mongo source root v3/child v1 and three receipts, SQLite target graph/head v1 and one exact receipt. No mutation replayed.");
        var cleanup = new[] { path, path + "-wal", path + "-shm" }.Select(Path.GetFullPath).ToArray();
        Assert.All(cleanup, candidate => Assert.Equal(directory, Path.GetDirectoryName(candidate)));
        await client.DropDatabaseAsync(name); foreach (var candidate in cleanup) if (File.Exists(candidate)) File.Delete(candidate);
    }

}
