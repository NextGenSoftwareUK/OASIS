using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    /// <summary>
    /// Reads tables another application keeps in its own SQLite file (<see cref="NativeImportConnectionString"/>, opened
    /// read-only) so NativeDataImportManager can holonize them. The OASIS database itself is refused, so OASIS's own
    /// tables never look like foreign data. Rows page in rowid order; a table's declared primary key (as a JSON array of its
    /// values) is the native key when it has one, otherwise its rowid.
    /// </summary>
    public partial class SQLLiteDBOASIS : INativeDataImportProvider
    {
        private readonly string _ownConnectionString;

        /// <summary>The SQLite database holding the application data to import.</summary>
        public string NativeImportConnectionString { get; set; }

        public ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.SQLLiteDBOASIS;

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                using var connection = await OpenNativeAsync(cancellationToken);
                var tables = new List<string>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY name";
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken)) tables.Add(reader.GetString(0));
                }
                var sources = new List<NativeDataSource>();
                foreach (var table in tables)
                {
                    using var count = connection.CreateCommand();
                    count.CommandText = $"SELECT COUNT(*) FROM {Quote(table)}";
                    sources.Add(new NativeDataSource(table, "table", Convert.ToInt64(await count.ExecuteScalarAsync(cancellationToken))));
                }
                result.Result = sources;
                result.Message = $"{sources.Count} tables in the native SQLite database.";
            }
            catch (Exception cause) when (cause is SqliteException or InvalidOperationException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native SQLite tables failed: {cause.Message}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_LIST_FAILED";
            }
            return result;
        }

        public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<NativeRecordPage>();
            try
            {
                if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
                using var connection = await OpenNativeAsync(cancellationToken);
                var keyColumns = await PrimaryKeyAsync(connection, sourceName, cancellationToken);
                long after = continuationToken == null ? long.MinValue : long.Parse(continuationToken, System.Globalization.CultureInfo.InvariantCulture);

                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT rowid AS __oasis_rowid, * FROM {Quote(sourceName)} WHERE rowid > $after ORDER BY rowid LIMIT $take";
                command.Parameters.AddWithValue("$after", after);
                command.Parameters.AddWithValue("$take", pageSize + 1);
                var records = new List<NativeRecord>();
                long lastRowId = 0;
                var more = false;
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (records.Count == pageSize) { more = true; break; }
                        lastRowId = reader.GetInt64(0);
                        var fields = new Dictionary<string, object>(StringComparer.Ordinal);
                        for (var column = 1; column < reader.FieldCount; column++)
                            fields[reader.GetName(column)] = ToScalar(reader.IsDBNull(column) ? null : reader.GetValue(column));
                        var key = keyColumns.Count == 0
                            ? "rowid:" + lastRowId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : System.Text.Json.JsonSerializer.Serialize(keyColumns.Select(name => fields.TryGetValue(name, out var value) ? value : null));
                        records.Add(new NativeRecord(sourceName, key, fields));
                    }
                }
                result.Result = new NativeRecordPage(records, more ? lastRowId.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
            }
            catch (Exception cause) when (cause is SqliteException or InvalidOperationException or ArgumentException or FormatException)
            {
                OASISErrorHandling.HandleError(ref result, $"Reading native SQLite table '{sourceName}' failed: {cause.Message}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
            }
            return result;
        }

        internal static object ToScalar(object value) => value switch
        {
            null or DBNull => null,
            byte[] bytes => Convert.ToBase64String(bytes),
            long or double or string => value,
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };

        private static string Quote(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("A table name is required.");
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Declared primary-key columns in key order; empty when the table has none. Refuses unknown and WITHOUT ROWID tables.</summary>
        private static async Task<IReadOnlyList<string>> PrimaryKeyAsync(SqliteConnection connection, string table, CancellationToken cancellationToken)
        {
            using (var exists = connection.CreateCommand())
            {
                exists.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $name AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\'";
                exists.Parameters.AddWithValue("$name", table);
                var sql = await exists.ExecuteScalarAsync(cancellationToken) as string
                    ?? throw new ArgumentException($"'{table}' is not a table in the native SQLite database.");
                if (sql.IndexOf("WITHOUT ROWID", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new ArgumentException($"'{table}' is a WITHOUT ROWID table, which native import cannot page yet.");
            }
            var columns = new List<(int Position, string Name)>();
            using var info = connection.CreateCommand();
            info.CommandText = $"PRAGMA table_info({Quote(table)})";
            using var reader = await info.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                if (reader.GetInt32(5) > 0) columns.Add((reader.GetInt32(5), reader.GetString(1)));
            return columns.OrderBy(column => column.Position).Select(column => column.Name).ToList();
        }

        private async Task<SqliteConnection> OpenNativeAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(NativeImportConnectionString))
                throw new InvalidOperationException("Set NativeImportConnectionString to the SQLite database holding the data to import.");
            var native = new SqliteConnectionStringBuilder(NativeImportConnectionString);
            if (!string.IsNullOrWhiteSpace(_ownConnectionString) &&
                string.Equals(FullPath(native.DataSource), FullPath(new SqliteConnectionStringBuilder(_ownConnectionString).DataSource), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The native import database is the OASIS database; native import reads application data from a different file.");
            if (!File.Exists(FullPath(native.DataSource)))
                throw new InvalidOperationException($"The native SQLite database '{native.DataSource}' does not exist.");
            native.Mode = SqliteOpenMode.ReadOnly;
            var connection = new SqliteConnection(native.ToString());
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string FullPath(string dataSource) => Path.GetFullPath(dataSource ?? "");
    }
}
