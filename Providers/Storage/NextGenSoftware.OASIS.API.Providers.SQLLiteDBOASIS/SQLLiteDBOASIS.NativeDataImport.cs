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
    /// Reads tables another application keeps in its own SQLite file so NativeDataImportManager can holonize them. Given a
    /// <see cref="NativeConnection"/> (<c>fileName</c> and <c>fileBase64</c>, the database file itself) it reads the caller's
    /// file from a private temporary copy that is deleted afterwards; with none it reads <see cref="NativeImportConnectionString"/>.
    /// Either way the file is opened read-only and the OASIS database is refused. Rows page in rowid order; a table's declared
    /// primary key (as a JSON array of its values) is the native key when it has one, otherwise its rowid.
    /// </summary>
    public partial class SQLLiteDBOASIS : INativeDataImportProvider
    {
        public const int MaximumUploadBytes = 8 * 1024 * 1024;
        private static readonly string[] ConnectionSettings = { "fileName", "fileBase64" };
        private static readonly byte[] SqliteHeader = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");

        private readonly string _ownConnectionString;

        /// <summary>The SQLite database holding the application data to import.</summary>
        public string NativeImportConnectionString { get; set; }

        public ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.SQLLiteDBOASIS;

        public string DescribeNativeConnection(NativeConnection connection)
        {
            if (connection == null) return null;
            connection.RequireOnly(ConnectionSettings);
            return "sqlite-file:" + FileName(connection);
        }

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                using var lease = await OpenNativeAsync(connection, cancellationToken);
                var tables = new List<string>();
                using (var command = lease.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY name";
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken)) tables.Add(reader.GetString(0));
                }
                var sources = new List<NativeDataSource>();
                foreach (var table in tables)
                {
                    using var count = lease.Connection.CreateCommand();
                    count.CommandText = $"SELECT COUNT(*) FROM {Quote(table)}";
                    sources.Add(new NativeDataSource(table, "table", Convert.ToInt64(await count.ExecuteScalarAsync(cancellationToken))));
                }
                result.Result = sources;
                result.Message = $"{sources.Count} tables in the native SQLite database.";
            }
            catch (NativeConnectionRefusedException cause)
            {
                OASISErrorHandling.HandleError(ref result, cause.Message);
                result.ErrorCode = "NATIVE_IMPORT_CONNECTION_REFUSED";
            }
            catch (Exception cause) when (cause is SqliteException or InvalidOperationException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native SQLite tables failed: {cause.Message}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_LIST_FAILED";
            }
            return result;
        }

        public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
            NativeConnection connection = null, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<NativeRecordPage>();
            try
            {
                if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
                using var lease = await OpenNativeAsync(connection, cancellationToken);
                var keyColumns = await PrimaryKeyAsync(lease.Connection, sourceName, cancellationToken);
                long after = continuationToken == null ? long.MinValue : long.Parse(continuationToken, System.Globalization.CultureInfo.InvariantCulture);

                using var command = lease.Connection.CreateCommand();
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
            catch (NativeConnectionRefusedException cause)
            {
                OASISErrorHandling.HandleError(ref result, cause.Message);
                result.ErrorCode = "NATIVE_IMPORT_CONNECTION_REFUSED";
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

        /// <summary>An open read-only connection; a caller's uploaded file lives in a private temporary copy that is deleted on dispose.</summary>
        private sealed class NativeLease : IDisposable
        {
            private readonly string _temporaryFile;
            public SqliteConnection Connection { get; }

            public NativeLease(SqliteConnection connection, string temporaryFile) { Connection = connection; _temporaryFile = temporaryFile; }

            public void Dispose()
            {
                Connection.Dispose();
                if (_temporaryFile == null) return;
                SqliteConnection.ClearAllPools();
                try { File.Delete(_temporaryFile); } catch (IOException) { }
            }
        }

        private async Task<NativeLease> OpenNativeAsync(NativeConnection supplied, CancellationToken cancellationToken)
        {
            if (supplied != null) return await OpenUploadedAsync(supplied, cancellationToken);

            if (string.IsNullOrWhiteSpace(NativeImportConnectionString))
                throw new InvalidOperationException("Set NativeImportConnectionString to the SQLite database holding the data to import, or send a connection.");
            var native = new SqliteConnectionStringBuilder(NativeImportConnectionString);
            if (!string.IsNullOrWhiteSpace(_ownConnectionString) &&
                string.Equals(FullPath(native.DataSource), FullPath(new SqliteConnectionStringBuilder(_ownConnectionString).DataSource), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The native import database is the OASIS database; native import reads application data from a different file.");
            if (!File.Exists(FullPath(native.DataSource)))
                throw new InvalidOperationException($"The native SQLite database '{native.DataSource}' does not exist.");
            native.Mode = SqliteOpenMode.ReadOnly;
            return new NativeLease(await OpenReadOnlyAsync(native.ToString(), cancellationToken), null);
        }

        private async Task<NativeLease> OpenUploadedAsync(NativeConnection supplied, CancellationToken cancellationToken)
        {
            supplied.RequireOnly(ConnectionSettings);
            FileName(supplied);
            byte[] bytes;
            try { bytes = Convert.FromBase64String(supplied.Require("fileBase64")); }
            catch (FormatException) { throw new NativeConnectionRefusedException("'fileBase64' is not valid base64."); }
            if (bytes.Length > MaximumUploadBytes)
                throw new NativeConnectionRefusedException($"The SQLite file is {bytes.Length} bytes; the limit is {MaximumUploadBytes}.");
            if (bytes.Length < SqliteHeader.Length || !bytes.Take(SqliteHeader.Length).SequenceEqual(SqliteHeader))
                throw new NativeConnectionRefusedException("The upload is not a SQLite 3 database file.");

            var path = Path.Combine(Path.GetTempPath(), "oasis-native-" + Guid.NewGuid().ToString("N") + ".db");
            File.WriteAllBytes(path, bytes);
            try
            {
                var builder = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly };
                return new NativeLease(await OpenReadOnlyAsync(builder.ToString(), cancellationToken), path);
            }
            catch
            {
                try { File.Delete(path); } catch (IOException) { }
                throw;
            }
        }

        private static async Task<SqliteConnection> OpenReadOnlyAsync(string connectionString, CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection(connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                // Read-only already refuses writes; these also stop a crafted file's views and triggers from running code.
                using var pragmas = connection.CreateCommand();
                pragmas.CommandText = "PRAGMA query_only = ON; PRAGMA trusted_schema = OFF;";
                await pragmas.ExecuteNonQueryAsync(cancellationToken);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private static string FileName(NativeConnection supplied)
        {
            var name = supplied.Require("fileName");
            if (name.Length > 128 || name.Any(c => char.IsControl(c) || c == '/' || c == '\\'))
                throw new NativeConnectionRefusedException("'fileName' must be a plain file name of at most 128 characters.");
            return name;
        }

        private static string FullPath(string dataSource) => Path.GetFullPath(dataSource ?? "");
    }
}
