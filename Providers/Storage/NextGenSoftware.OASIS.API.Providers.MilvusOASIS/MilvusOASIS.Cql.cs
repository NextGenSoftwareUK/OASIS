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

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

public partial class MilvusOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.MilvusOASIS;
    public CosmicQueryCapabilities QueryCapabilities => MilvusCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Milvus target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = MilvusCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Milvus history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Milvus indices before querying.");

            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                // Native retrieval over admitted keys, without vector similarity or collection scans.
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                var ids = selected.Result.Select(id => id.ToString("D")).ToArray();
                var filter = "id in " + JsonSerializer.Serialize(ids);
                using var request = new HttpRequestMessage(HttpMethod.Post,$"{_baseUrl}/v2/vectordb/entities/query") {
                    Content = Json(new {collectionName=CollHolons,filter,outputFields=new[]{"id","doc"},limit=ids.Length+1,offset=0,consistencyLevel="Strong"}) };
                request.Headers.Add("Request-Timeout","15");
                // The pinned2.6.4 REST handler explicitly maps consistencyLevel.
                // Require Strong on the collection and the native query; no
                // unsupported Get parameter or stale-read fallback.
                using var response = await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
                response.EnsureSuccessStatusCode();
                using var document = await ReadQueryResponseAsync(response,deadline.Token);
                if (document.RootElement.GetProperty("code").GetInt32()!=0)
                    throw new InvalidDataException("Native Milvus query failed.");
                var records = document.RootElement.GetProperty("data");
                if (records.ValueKind!=JsonValueKind.Array || records.GetArrayLength()>selected.Result.Count)
                    throw new InvalidDataException("Native result exceeds the admitted key catalogue.");
                var seen = new HashSet<Guid>();
                foreach (var record in records.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = record.GetProperty("id").GetString();
                    if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") ||
                        !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native identity differs from admitted catalogue.");
                    var json = record.GetProperty("doc").GetString()!;
                    if (Encoding.UTF8.GetByteCount(json)>65535)
                        throw new NativePayloadLimitException();
                    var row = JsonSerializer.Deserialize<Holon>(json);
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
        { return Error("CQL_QUERY_CANCELLED", "Milvus query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "Native response or stored payload exceeds the admitted byte budget.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Milvus query failed; no partial rows published.", cause); }
    }
    private sealed class NativePayloadLimitException : IOException { }

    private static async Task<JsonDocument> ReadQueryResponseAsync(HttpResponseMessage response,CancellationToken token)
    {
        using var stream=await response.Content.ReadAsStreamAsync(token);
        using var body=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=await stream.ReadAsync(buffer,token))!=0)
        {
            if(body.Length+count>4*1024*1024)throw new NativePayloadLimitException();
            body.Write(buffer,0,count);
        }
        return JsonDocument.Parse(body.ToArray());
    }

}
