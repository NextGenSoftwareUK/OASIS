using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests
{
    /// <summary>The SQLite native reader against real files: tables listed, rows paged in rowid order, keys stable.</summary>
    public sealed class SqliteNativeDataImportTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "oasis-native-sqlite-" + Guid.NewGuid().ToString("N"));
        private string NativePath => Path.Combine(_folder, "app.db");
        private string OasisPath => Path.Combine(_folder, "oasis.db");

        public SqliteNativeDataImportTests()
        {
            Directory.CreateDirectory(_folder);
            using var connection = new SqliteConnection($"Data Source={NativePath}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE suppliers (id INTEGER PRIMARY KEY, name TEXT, rating REAL, logo BLOB);
                CREATE TABLE lines (orderId INTEGER, lineNo INTEGER, sku TEXT, PRIMARY KEY (orderId, lineNo));
                CREATE TABLE notes (body TEXT);
                INSERT INTO suppliers VALUES (3, 'Gamma', 4.5, x'0102'), (1, 'Alpha', NULL, NULL), (2, 'Beta', 5.0, NULL);
                INSERT INTO lines VALUES (10, 1, 'A'), (10, 2, 'B');
                INSERT INTO notes VALUES ('first'), ('second');";
            command.ExecuteNonQuery();
        }

        private SQLLiteDBOASIS Provider() => new SQLLiteDBOASIS($"Data Source={OasisPath}") { NativeImportConnectionString = $"Data Source={NativePath}" };

        [Fact]
        public async Task ListsTablesAndPagesEveryRow()
        {
            var provider = Provider();
            var sources = await provider.ListNativeDataSourcesAsync();
            Assert.False(sources.IsError, sources.Message);
            Assert.Equal(new[] { "lines", "notes", "suppliers" }, sources.Result.Select(source => source.Name));
            Assert.Equal(3, sources.Result.Single(source => source.Name == "suppliers").EstimatedRecordCount);

            var names = new System.Collections.Generic.List<string>();
            string token = null;
            var pages = 0;
            do
            {
                var page = await provider.ReadNativeRecordsAsync("suppliers", token, 2);
                Assert.False(page.IsError, page.Message);
                names.AddRange(page.Result.Records.Select(record => (string)record.Fields["name"]));
                token = page.Result.ContinuationToken;
                pages++;
            }
            while (token != null);
            Assert.Equal(2, pages);
            Assert.Equal(3, names.Count);
            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, names); // INTEGER PRIMARY KEY is the rowid, so rows page in id order.
        }

        [Fact]
        public async Task KeysValuesAndTypesAreStable()
        {
            var provider = Provider();
            var suppliers = (await provider.ReadNativeRecordsAsync("suppliers", null, 10)).Result.Records;
            var gamma = suppliers.Single(record => (string)record.Fields["name"] == "Gamma");
            Assert.Equal("[3]", gamma.NativeKey);
            Assert.Equal(4.5, gamma.Fields["rating"]);
            Assert.Equal("AQI=", gamma.Fields["logo"]);
            Assert.Null(suppliers.Single(record => (string)record.Fields["name"] == "Alpha").Fields["rating"]);

            var lines = (await provider.ReadNativeRecordsAsync("lines", null, 10)).Result.Records;
            Assert.Equal(new[] { "[10,1]", "[10,2]" }, lines.Select(record => record.NativeKey));
            var notes = (await provider.ReadNativeRecordsAsync("notes", null, 10)).Result.Records;
            Assert.Equal(new[] { "rowid:1", "rowid:2" }, notes.Select(record => record.NativeKey));
        }

        [Fact]
        public async Task VirtualProjectionUsesTheSameCompositeKeyAndExactNativeFilter()
        {
            var provider = Provider();
            var imported = (await provider.ReadNativeRecordsAsync("lines", null, 10)).Result.Records;
            var result = await provider.QueryNativeRecordsAsync("lines", "sku", "A", new[] { "sku" }, 1);
            Assert.False(result.IsError, result.Message);
            var row = Assert.Single(result.Result);
            Assert.Equal(imported[0].NativeKey, row.NativeKey);
            Assert.Equal("A", row.Fields["sku"]);
            Assert.Single(row.Fields);
            var conjunction = await provider.QueryNativeRecordsAsync("lines",
                new[] { new KeyValuePair<string, object>("orderId", 10L),
                    new KeyValuePair<string, object>("sku", "B") }, new[] { "sku" }, 1);
            Assert.False(conjunction.IsError, conjunction.Message);
            Assert.Equal(imported[1].NativeKey, Assert.Single(conjunction.Result).NativeKey);
            Assert.Empty((await provider.QueryNativeRecordsAsync("lines", "sku", "a", new[] { "sku" }, 2)).Result);
            Assert.Empty((await provider.QueryNativeRecordsAsync("lines", "orderId", "10", new[] { "sku" }, 2)).Result);
            Assert.Equal(new[] { "A", "B" }, (await provider.QueryNativeRecordsAsync("lines", "orderId",
                10L, new[] { "sku" }, 2)).Result.Select(item => item.Fields["sku"]));
            Assert.Equal("NATIVE_QUERY_INVALID",
                (await provider.QueryNativeRecordsAsync("lines", "sku", "A", new[] { "missing" }, 2)).ErrorCode);
            var actor = Guid.NewGuid();
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                "lines", VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["orderId"] = "orderId", ["sku"] = "sku" });
            var before = await mapping.QueryAsync(actor, provider, null, "orderId", 10L, new[] { "sku" }, 1);
            Assert.False(before.IsError, before.Message);
            var original = Assert.Single(before.Result);
            var snapshot = original.PrepareSnapshot(actor);
            Assert.Equal("A", snapshot.MetaData["sku"]);

            using (var connection = new SqliteConnection($"Data Source={NativePath}"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE lines SET sku = 'A-updated' WHERE orderId = 10 AND lineNo = 1";
                Assert.Equal(1, await command.ExecuteNonQueryAsync());
            }

            var after = await mapping.QueryAsync(actor, provider, null, "orderId", 10L, new[] { "sku" }, 1);
            Assert.False(after.IsError, after.Message);
            var refreshed = Assert.Single(after.Result);
            Assert.Equal(original.Id, refreshed.Id);
            Assert.Equal("A-updated", refreshed.MetaData["sku"]);
            Assert.Equal("A", snapshot.MetaData["sku"]);

        }

        [Fact]
        public async Task RefusesTheOasisDatabaseUnknownTablesAndMissingFiles()
        {
            var same = new SQLLiteDBOASIS($"Data Source={NativePath}") { NativeImportConnectionString = $"Data Source={NativePath}" };
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await same.ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider().ReadNativeRecordsAsync("sqlite_master", null, 5)).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider().ReadNativeRecordsAsync("missing", null, 5)).ErrorCode);
            var absent = new SQLLiteDBOASIS($"Data Source={OasisPath}") { NativeImportConnectionString = $"Data Source={Path.Combine(_folder, "none.db")}" };
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await absent.ListNativeDataSourcesAsync()).ErrorCode);
        }

        private byte[] NativeBytes()
        {
            SqliteConnection.ClearAllPools(); // the constructor's pooled connection still holds the file
            return File.ReadAllBytes(NativePath);
        }

        private NativeConnection Upload(byte[] bytes, string name = "shop.db") =>
            new NativeConnection { ["fileName"] = name, ["fileBase64"] = Convert.ToBase64String(bytes) };

        [Fact]
        public async Task ReadsAnUploadedDatabaseAndLeavesNoTemporaryFileBehind()
        {
            var before = Directory.GetFiles(Path.GetTempPath(), "oasis-native-*.db").Length;
            var provider = new SQLLiteDBOASIS($"Data Source={OasisPath}");
            var upload = Upload(NativeBytes());

            var sources = await provider.ListNativeDataSourcesAsync(upload);
            Assert.False(sources.IsError, sources.Message);
            Assert.Equal(new[] { "lines", "notes", "suppliers" }, sources.Result.Select(source => source.Name));

            var page = await provider.ReadNativeRecordsAsync("suppliers", null, 10, upload);
            Assert.False(page.IsError, page.Message);
            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, page.Result.Records.Select(record => (string)record.Fields["name"]));

            Assert.Equal("sqlite-file:shop.db", provider.DescribeNativeConnection(upload));
            Assert.Null(provider.DescribeNativeConnection(null));
            Assert.Equal(before, Directory.GetFiles(Path.GetTempPath(), "oasis-native-*.db").Length);
        }

        [Fact]
        public async Task BadUploadsAreRefusedAsConnectionProblems()
        {
            var provider = new SQLLiteDBOASIS($"Data Source={OasisPath}");

            async Task AssertRefused(NativeConnection connection, string expected)
            {
                var result = await provider.ListNativeDataSourcesAsync(connection);
                Assert.Equal("NATIVE_IMPORT_CONNECTION_REFUSED", result.ErrorCode);
                Assert.Contains(expected, result.Message);
            }

            await AssertRefused(Upload(System.Text.Encoding.ASCII.GetBytes("this is not a database, just text padding")), "not a SQLite 3 database");
            await AssertRefused(new NativeConnection { ["fileName"] = "x.db", ["fileBase64"] = "%%%not base64%%%" }, "not valid base64");
            await AssertRefused(Upload(NativeBytes(), "../../etc/passwd"), "plain file name");
            await AssertRefused(new NativeConnection { ["fileBase64"] = Convert.ToBase64String(NativeBytes()) }, "fileName");
            await AssertRefused(new NativeConnection { ["fileName"] = "x.db", ["fileBase64"] = "AA==", ["path"] = "/etc/passwd" }, "path");
            await AssertRefused(Upload(new byte[SQLLiteDBOASIS.MaximumUploadBytes + 1]), "limit");
        }

        [Fact]
        public async Task TheSameTableInTwoUploadedFilesGetsDifferentIdentity()
        {
            var provider = new SQLLiteDBOASIS($"Data Source={OasisPath}");
            var bytes = NativeBytes();
            Assert.NotEqual(provider.DescribeNativeConnection(Upload(bytes, "a.db")), provider.DescribeNativeConnection(Upload(bytes, "b.db")));
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }
    }
}
