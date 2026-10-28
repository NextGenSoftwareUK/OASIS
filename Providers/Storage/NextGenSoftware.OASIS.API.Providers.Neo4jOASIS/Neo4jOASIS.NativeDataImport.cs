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
    /// Reads nodes other applications keep in the same Neo4j database so NativeDataImportManager can holonize them.
    /// OASIS shares the database with them, so only labels explicitly listed in <see cref="NativeImportLabels"/> are ever
    /// read: each label is a source, each node a record keyed by its element id, paged in element-id order.
    /// </summary>
    public partial class Neo4jOASIS : INativeDataImportProvider
    {
        private static readonly Regex LabelPattern = new Regex("^[A-Za-z_][A-Za-z0-9_]{0,99}$", RegexOptions.CultureInvariant);

        /// <summary>Comma-separated labels other applications own that native import may read (DNA Neo4jOASIS.NativeImportLabels).</summary>
        public string NativeImportLabels { get; set; }

        public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.Neo4jOASIS;

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                var sources = new List<NativeDataSource>();
                using var session = NativeSession();
                foreach (var label in AllowedLabels())
                {
                    var cursor = await session.RunAsync($"MATCH (n:`{label}`) RETURN count(n) AS total");
                    var total = (await cursor.SingleAsync())["total"].As<long>();
                    sources.Add(new NativeDataSource(label, "label", total));
                }
                result.Result = sources;
                result.Message = $"{sources.Count} allowed node labels in Neo4j.";
            }
            catch (Exception cause) when (cause is Neo4jException or InvalidOperationException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native Neo4j labels failed: {cause.Message}", cause);
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
                if (!AllowedLabels().Contains(sourceName, StringComparer.Ordinal))
                    throw new ArgumentException($"'{sourceName}' is not a label listed in NativeImportLabels.");
                using var session = NativeSession();
                var cursor = await session.RunAsync(
                    $"MATCH (n:`{sourceName}`) WHERE $after IS NULL OR elementId(n) > $after " +
                    "RETURN elementId(n) AS key, properties(n) AS fields ORDER BY key LIMIT $take",
                    new { after = continuationToken, take = pageSize + 1 });
                var rows = await cursor.ToListAsync();
                var page = rows.Take(pageSize).Select(row => new NativeRecord(sourceName, row["key"].As<string>(),
                    row["fields"].As<IDictionary<string, object>>().ToDictionary(field => field.Key, field => ToScalar(field.Value), StringComparer.Ordinal))).ToList();
                result.Result = new NativeRecordPage(page, rows.Count > pageSize ? page[^1].NativeKey : null);
            }
            catch (Exception cause) when (cause is Neo4jException or InvalidOperationException or ArgumentException)
            {
                OASISErrorHandling.HandleError(ref result, $"Reading native Neo4j label '{sourceName}' failed: {cause.Message}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
            }
            return result;
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

        private IReadOnlyList<string> AllowedLabels()
        {
            var labels = (NativeImportLabels ?? "").Split(',').Select(label => label.Trim()).Where(label => label.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            if (labels.Count == 0)
                throw new InvalidOperationException("Set NativeImportLabels to the Neo4j labels other applications own before importing native data.");
            var invalid = labels.FirstOrDefault(label => !LabelPattern.IsMatch(label));
            if (invalid != null)
                throw new InvalidOperationException($"'{invalid}' is not a valid Neo4j label name.");
            return labels;
        }

        private IAsyncSession NativeSession()
        {
            if (!IsProviderActivated || _driver == null)
                throw new InvalidOperationException("The Neo4j provider must be activated before importing native data.");
            return _driver.AsyncSession(options => options.WithDefaultAccessMode(AccessMode.Read));
        }
    }
}
