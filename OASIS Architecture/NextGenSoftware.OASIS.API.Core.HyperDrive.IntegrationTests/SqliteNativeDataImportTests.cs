using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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
        public async Task RefusesTheOasisDatabaseUnknownTablesAndMissingFiles()
        {
            var same = new SQLLiteDBOASIS($"Data Source={NativePath}") { NativeImportConnectionString = $"Data Source={NativePath}" };
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await same.ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider().ReadNativeRecordsAsync("sqlite_master", null, 5)).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider().ReadNativeRecordsAsync("missing", null, 5)).ErrorCode);
            var absent = new SQLLiteDBOASIS($"Data Source={OasisPath}") { NativeImportConnectionString = $"Data Source={Path.Combine(_folder, "none.db")}" };
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await absent.ListNativeDataSourcesAsync()).ErrorCode);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }
    }
}
