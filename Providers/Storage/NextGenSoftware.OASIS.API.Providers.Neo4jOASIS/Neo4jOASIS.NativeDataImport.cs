using NextGenSoftware.OASIS.API.Core.Queries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    /// <summary>
    /// Reads nodes other applications keep in Neo4j so NativeDataImportManager can holonize them. Given a
    /// <see cref="NativeConnection"/> (<c>uri</c>, <c>username</c>, <c>password</c>, optional <c>database</c> and <c>labels</c>)
    /// it reads the caller's own database, discovering its labels unless <c>labels</c> narrows them. With none it reads the
    /// labels in <see cref="NativeImportLabels"/> from the OASIS database, since OASIS shares that database with other
    /// applications. Each label is a source, each node a record keyed by its element id, in element-id order, through a
    /// read-only session.
    /// </summary>
    public partial class Neo4jOASIS : INativeDataQueryProvider, INativeDataSnapshotQueryProvider
    {
        private static readonly Regex LabelPattern = new Regex("^[A-Za-z_][A-Za-z0-9_]{0,99}$", RegexOptions.CultureInvariant);
        private static readonly string[] ConnectionSettings = { "uri", "username", "password", "database", "labels" };
        private static readonly string[] AllowedSchemes = { "bolt", "bolt+s", "bolt+ssc", "neo4j", "neo4j+s", "neo4j+ssc" };

        /// <summary>Comma-separated labels other applications own that native import may read from the OASIS database (DNA Neo4jOASIS.NativeImportLabels).</summary>
        public string NativeImportLabels { get; set; }

        public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.Neo4jOASIS;

        public string DescribeNativeConnection(NativeConnection connection)
        {
            if (connection == null) return null;
            var (uri, _, _, database, _) = ParseConnection(connection);
            return $"{uri.Scheme}://{uri.Host.ToLowerInvariant()}:{(uri.Port < 0 ? 7687 : uri.Port)}/{database ?? "default"}";
        }

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                await using var lease = await OpenNativeAsync(connection, cancellationToken);
                var sources = new List<NativeDataSource>();
                foreach (var label in await LabelsAsync(lease, connection, cancellationToken))
                {
                    var cursor = await lease.Session.RunAsync($"MATCH (n:`{label}`) RETURN count(n) AS total");
                    var total = (await cursor.SingleAsync())["total"].As<long>();
                    sources.Add(new NativeDataSource(label, "label", total));
                }
                result.Result = sources;
                result.Message = $"{sources.Count} node labels in Neo4j.";
            }
            catch (NativeConnectionRefusedException cause)
            {
                OASISErrorHandling.HandleError(ref result, Redact(connection, cause.Message));
                result.ErrorCode = "NATIVE_IMPORT_CONNECTION_REFUSED";
            }
            catch (Exception cause) when (cause is Neo4jException or InvalidOperationException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native Neo4j labels failed: {Redact(connection, cause.Message)}");
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
                await using var lease = await OpenNativeAsync(connection, cancellationToken);
                if (!(await LabelsAsync(lease, connection, cancellationToken)).Contains(sourceName, StringComparer.Ordinal))
                    throw new ArgumentException($"'{sourceName}' is not an importable label of this connection.");
                var cursor = await lease.Session.RunAsync(
                    $"MATCH (n:`{sourceName}`) WHERE $after IS NULL OR elementId(n) > $after " +
                    "RETURN elementId(n) AS key, properties(n) AS fields ORDER BY key LIMIT $take",
                    new { after = continuationToken, take = pageSize + 1 });
                var rows = await cursor.ToListAsync();
                var page = rows.Take(pageSize).Select(row => new NativeRecord(sourceName, row["key"].As<string>(),
                    row["fields"].As<IDictionary<string, object>>().ToDictionary(field => field.Key, field => ToScalar(field.Value), StringComparer.Ordinal))).ToList();
                result.Result = new NativeRecordPage(page, rows.Count > pageSize ? page[^1].NativeKey : null);
            }
            catch (NativeConnectionRefusedException cause)
            {
                OASISErrorHandling.HandleError(ref result, Redact(connection, cause.Message));
                result.ErrorCode = "NATIVE_IMPORT_CONNECTION_REFUSED";
            }
            catch (Exception cause) when (cause is Neo4jException or InvalidOperationException or ArgumentException)
            {
                OASISErrorHandling.HandleError(ref result, $"Reading native Neo4j label '{sourceName}' failed: {Redact(connection, cause.Message)}");
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
            }
            return result;
        }

        public string DescribeVirtualQueryConnection(NativeConnection connection)
        {
            if (connection != null)
                return DescribeNativeConnection(connection);
            if (!IsProviderActivated || _driver == null)
                throw new NativeConnectionRefusedException("The configured Neo4j source is unavailable.");
            var uri = ConnectionUri(Host);
            return $"neo4j-configured://{uri.Host.ToLowerInvariant()}:{(uri.Port < 0 ? 7687 : uri.Port)}/default";
        }

        public Task<OASISResult<IReadOnlyList<NativeRecord>>> QueryNativeRecordsAsync(
            string sourceName, string matchField, object matchValue, IReadOnlyList<string> fields, int limit,
            NativeConnection connection = null, CancellationToken cancellationToken = default) =>
            QueryNativeRecordsAsync(sourceName, new[] { new KeyValuePair<string, object>(matchField, matchValue) },
                fields, limit, connection, cancellationToken);

        public Task<OASISResult<IReadOnlyList<NativeRecord>>> QueryNativeRecordsAsync(
            string sourceName, IReadOnlyList<KeyValuePair<string, object>> matches, IReadOnlyList<string> fields, int limit,
            NativeConnection connection = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return QueryNativePredicateRecordsAsync(sourceName, NativeRecordFilter.CaptureEqualities(matches),
                    fields, limit, connection, cancellationToken);
            }
            catch (ArgumentException)
            {
                var invalid = new OASISResult<IReadOnlyList<NativeRecord>>();
                OASISErrorHandling.HandleError(ref invalid, "One to eight typed scalar matches are required.");
                invalid.ErrorCode = "NATIVE_QUERY_INVALID";
                return Task.FromResult(invalid);
            }
        }

        public Task<OASISResult<IReadOnlyList<NativeRecord>>> QueryNativePredicateRecordsAsync(
            string sourceName, IReadOnlyList<NativeRecordFilter> matches, IReadOnlyList<string> fields, int limit,
            NativeConnection connection = null, CancellationToken cancellationToken = default)
            => QueryNativeRecordsCoreAsync(sourceName, matches, fields, limit, connection, cancellationToken, false);

        public Task<OASISResult<IReadOnlyList<NativeRecord>>> QueryNativeSnapshotRecordsAsync(
            string sourceName, IReadOnlyList<NativeRecordFilter> matches, IReadOnlyList<string> fields, int limit,
            NativeConnection connection = null, CancellationToken cancellationToken = default)
            => QueryNativeRecordsCoreAsync(sourceName, matches, fields, limit, connection, cancellationToken, true);

        private async Task<OASISResult<IReadOnlyList<NativeRecord>>> QueryNativeRecordsCoreAsync(
            string sourceName, IReadOnlyList<NativeRecordFilter> matches, IReadOnlyList<string> fields, int limit,
            NativeConnection connection, CancellationToken cancellationToken, bool snapshotRead)
        {
            var result = new OASISResult<IReadOnlyList<NativeRecord>>();
            if (sourceName == null || !LabelPattern.IsMatch(sourceName) || matches == null || (matches.Count < (snapshotRead ? 0 : 1) || matches.Count > 8) ||
                matches.Any(match => match == null || match.Field == null || !LabelPattern.IsMatch(match.Field)) ||
                fields == null || fields.Count is < 1 or > 32 ||
                fields.Any(field => field == null || !LabelPattern.IsMatch(field)) ||
                fields.Distinct(StringComparer.Ordinal).Count() != fields.Count || (limit < 1 || limit > (snapshotRead ? 200 : 100)))
                return Error("NATIVE_QUERY_INVALID", "Select a native label, exact scalar equality, fields and a row limit.");

            matches = snapshotRead ? NativeRecordFilter.CaptureForProjection(matches) : NativeRecordFilter.Capture(matches);
            fields = fields.ToArray();
            var parameters = new Dictionary<string, object> { ["take"] = limit };
            string predicateWhere;
            try { predicateWhere = matches.Count == 0 ? "true" : CompileNativePredicateWhere(matches, parameters); }
            catch (ArgumentException)
            { return Error("NATIVE_QUERY_INVALID", "The exact native predicate is unsupported."); }

            try
            {
                await using var lease = await OpenNativeAsync(connection, cancellationToken);
                if (!(await LabelsAsync(lease, connection, cancellationToken)).Contains(sourceName, StringComparer.Ordinal))
                    return Error("NATIVE_QUERY_INVALID", "The connection does not admit that source label.");

                using var transaction = await lease.Session.BeginTransactionAsync(options => options.WithTimeout(NativeImportOptions.ReadTimeout));
                var columns = string.Join(", ", fields.Select((field, index) => $"n.`{field}` AS f{index}"));
                var cursor = await transaction.RunAsync(
                    $"MATCH (n:`{sourceName}`) WHERE {predicateWhere} " +
                    $"RETURN elementId(n) AS key, {columns} ORDER BY key LIMIT $take", parameters);
                var records = new List<NativeRecord>(limit);
                long remainingBytes = 1024 * 1024;
                while (await cursor.FetchAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = cursor.Current["key"].As<string>();
                    remainingBytes -= System.Text.Encoding.UTF8.GetByteCount(key);
                    var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
                    for (var index = 0; index < fields.Count; index++)
                    {
                        var value = cursor.Current["f" + index];
                        if (value is not (null or string or bool or int or long or double))
                            return Error("NATIVE_QUERY_INVALID", "Virtual Neo4j projections support scalar properties.");
                        remainingBytes -= value is string text ? System.Text.Encoding.UTF8.GetByteCount(text) : sizeof(long);
                        if (remainingBytes < 0)
                            return Error("NATIVE_QUERY_PAYLOAD_LIMIT", "The native Neo4j projection exceeds the virtual query byte limit.");
                        metadata.Add(fields[index], ToScalar(value));
                    }
                    records.Add(new NativeRecord(sourceName, key, metadata));
                }
                await transaction.CommitAsync();
                result.Result = records.AsReadOnly();
                result.IsLoaded = true;
                return result;
            }
            catch (NativeConnectionRefusedException)
            {
                return Error("NATIVE_IMPORT_CONNECTION_REFUSED", "The native Neo4j connection cannot be used.");
            }
            catch (Exception cause) when (cause is Neo4jException or InvalidOperationException or ArgumentException)
            {
                return Error("NATIVE_QUERY_FAILED", "The native Neo4j query failed.");
            }

            OASISResult<IReadOnlyList<NativeRecord>> Error(string code, string message)
            {
                OASISErrorHandling.HandleError(ref result, message);
                result.ErrorCode = code;
                return result;
            }
        }

        internal static object ToScalar(object value) => value switch
        {
            null => null,
            string or bool or long or double => value,
            int number => (long)number,
            IDictionary or IList => System.Text.Json.JsonSerializer.Serialize(Plain(value)),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };

        private static object Plain(object value) => value switch
        {
            IDictionary<string, object> map => map.ToDictionary(entry => entry.Key, entry => Plain(entry.Value)),
            IList list => list.Cast<object>().Select(Plain).ToList(),
            string or bool or long or double or int or null => value,
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };

        /// <summary>The labels that can be read: the connection's own (all of them unless narrowed), or the configured allowlist.</summary>
        private async Task<IReadOnlyList<string>> LabelsAsync(NativeLease lease, NativeConnection connection, CancellationToken cancellationToken)
        {
            if (connection == null) return AllowedLabels();

            var narrowed = connection.Optional("labels");
            var labels = narrowed != null
                ? narrowed.Split(',').Select(label => label.Trim()).Where(label => label.Length > 0).Distinct(StringComparer.Ordinal).ToList()
                : (await (await lease.Session.RunAsync("CALL db.labels() YIELD label RETURN label ORDER BY label")).ToListAsync())
                    .Select(row => row["label"].As<string>()).Where(label => LabelPattern.IsMatch(label)).Take(NativeImportOptions.Positive(NativeImportOptions.Current.MaximumNeo4jDiscoveredLabels, "MaximumNeo4jDiscoveredLabels")).ToList();
            var invalid = labels.FirstOrDefault(label => !LabelPattern.IsMatch(label));
            if (invalid != null) throw new NativeConnectionRefusedException($"'{invalid}' is not a valid Neo4j label name.");
            return labels;
        }

        private IReadOnlyList<string> AllowedLabels()
        {
            var labels = (NativeImportLabels ?? "").Split(',').Select(label => label.Trim()).Where(label => label.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            if (labels.Count == 0)
                throw new InvalidOperationException("Set NativeImportLabels to the Neo4j labels other applications own before importing native data, or send a connection.");
            var invalid = labels.FirstOrDefault(label => !LabelPattern.IsMatch(label));
            if (invalid != null)
                throw new InvalidOperationException($"'{invalid}' is not a valid Neo4j label name.");
            return labels;
        }

        private sealed class NativeLease : IAsyncDisposable
        {
            private readonly IDriver _ownedDriver;
            public IAsyncSession Session { get; }

            public NativeLease(IAsyncSession session, IDriver ownedDriver) { Session = session; _ownedDriver = ownedDriver; }

            public async ValueTask DisposeAsync()
            {
                await Session.CloseAsync();
                if (_ownedDriver != null) _ownedDriver.Dispose();
            }
        }

        private async Task<NativeLease> OpenNativeAsync(NativeConnection connection, CancellationToken cancellationToken, int? maximumFetchSize = null)
        {
            if (connection == null)
            {
                if (!IsProviderActivated || _driver == null)
                    throw new InvalidOperationException("The Neo4j provider must be activated before importing native data.");
                return new NativeLease(_driver.AsyncSession(options =>
                    {
                        options.WithDefaultAccessMode(AccessMode.Read);
                        if (maximumFetchSize.HasValue) options.WithFetchSize(maximumFetchSize.Value);
                    }), null);
            }

            var (uri, username, password, database, _) = ParseConnection(connection);
            await NativeConnectionGuard.EnsurePublicHostAsync(uri.Host, cancellationToken);
            var driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password), config => config
                .WithConnectionTimeout(NativeImportOptions.ConnectTimeout)
                .WithMaxConnectionPoolSize(2)
                .WithMaxTransactionRetryTime(NativeImportOptions.ConnectTimeout));
            try
            {
                return new NativeLease(driver.AsyncSession(options =>
                {
                    options.WithDefaultAccessMode(AccessMode.Read);
                    if (database != null) options.WithDatabase(database);
                    if (maximumFetchSize.HasValue) options.WithFetchSize(maximumFetchSize.Value);
                }), driver);
            }
            catch
            {
                driver.Dispose();
                throw;
            }
        }

        private static (Uri Uri, string Username, string Password, string Database, string Labels) ParseConnection(NativeConnection connection)
        {
            connection.RequireOnly(ConnectionSettings);
            if (!Uri.TryCreate(connection.Require("uri"), UriKind.Absolute, out var uri) || !AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new NativeConnectionRefusedException("The Neo4j uri must look like neo4j+s://host:7687 (bolt, bolt+s, bolt+ssc, neo4j, neo4j+s or neo4j+ssc) without credentials in it.");
            return (uri, connection.Require("username"), connection.Require("password"), connection.Optional("database"), connection.Optional("labels"));
        }

        private static string Redact(NativeConnection connection, string message) => connection == null ? message : connection.Redact(message, "password", "username");
    }
}
