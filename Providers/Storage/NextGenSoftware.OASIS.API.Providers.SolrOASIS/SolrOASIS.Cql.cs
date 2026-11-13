using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS;

public partial class SolrOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.SolrOASIS;
    public CosmicQueryCapabilities QueryCapabilities => SolrCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Solr target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = SolrCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Solr history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Solr indices before querying.");

            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                // Native retrieval over admitted keys, without searcher-refresh delay, text ranking or collection scans.
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                var ids = string.Join(",", selected.Result.Select(id => id.ToString("D")));
                var path = $"{_baseUrl}/solr/oasis_holons/get?ids={Uri.EscapeDataString(ids)}" +
                    "&fl=id,data_json,_version_&wt=json&omitHeader=false&shards.tolerant=false";
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                using var document = await ReadNativeResponseAsync(response, 4 * 1024 * 1024, deadline.Token);
                var root = document.RootElement;
                ValidateNativeStatus(root);
                var result = root.GetProperty("response");
                var records = result.GetProperty("docs");
                if (result.GetProperty("start").GetInt32() != 0 || records.ValueKind != JsonValueKind.Array ||
                    records.GetArrayLength() > selected.Result.Count || result.GetProperty("numFound").GetInt32() != records.GetArrayLength())
                    throw new InvalidDataException("Native real-time get is not the complete admitted key set.");
                var seen = new HashSet<Guid>();
                foreach (var record in records.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = record.GetProperty("id").GetString();
                    if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") ||
                        !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native identity differs from admitted catalogue.");
                    if (record.GetProperty("_version_").GetInt64() <= 0)
                        throw new InvalidDataException("Native document version is invalid.");
                    var row = DecodeDocument<Holon>(record);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(id, row);
                }
            }
            var page = selected.Result.Where(rows.ContainsKey).Select(id => rows[id])
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Solr query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "Complete native response exceeds 4 MiB.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Solr query failed; no partial rows published.", cause); }
    }
}
