using System.Net.Http;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

public partial class WeaviateOASIS : ICosmicQueryStorageProvider
{
    private sealed class NativeQueryConfigurationException : InvalidOperationException { }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.WeaviateOASIS;
    public CosmicQueryCapabilities QueryCapabilities => WeaviateCqlCompiler.Capabilities;

    protected sealed class NativeCqlDocument
    {
        public NativeCqlDocument() { }
        public string Key { get; set; } = "";
        public string Id { get; set; } = "";
        public bool IsDeleted { get; set; }
        public string DataJson { get; set; } = "";
    }

    protected virtual async IAsyncEnumerable<NativeCqlDocument> ReadNativeCqlAsync(
        IReadOnlyList<Guid> ids, [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (_queryClient == null) throw new NativeQueryConfigurationException();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var result = await _queryClient.Collections.Use("OasisHolon").Query.FetchObjectsByIDs(
            ids.ToHashSet(), limit: (uint)ids.Count + 1,
            returnProperties: new[] { "dataJson", "isDeleted" }, cancellationToken: deadline.Token);
        foreach (var item in result.Objects)
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (!item.UUID.HasValue || !item.Properties.TryGetValue("dataJson", out var raw) || raw is not string json ||
                !item.Properties.TryGetValue("isDeleted", out var deleted) || deleted is not bool flag)
                throw new InvalidDataException("Native object identity or required property types missing.");
            var id = item.UUID.Value.ToString("D");
            yield return new NativeCqlDocument { Key = id, Id = id, IsDeleted = flag, DataJson = json };
        }
    }

    private static async Task<JsonDocument> ReadNativeResponseAsync(HttpResponseMessage response,
        int maximumBytes, CancellationToken cancellation)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, cancellation)) != 0)
        {
            if (body.Length + count > maximumBytes)
                throw new NativePayloadLimitException();
            body.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(body.ToArray());
    }

    private sealed class NativePayloadLimitException : IOException { }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Weaviate target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = WeaviateCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Weaviate history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate configured Weaviate collections before querying.");
            var rows = new Dictionary<Guid, IHolon>();
            var seen = new HashSet<Guid>();
            long bytes = 0;
            if (selected.Result.Count != 0)
            {
                await foreach (var record in ReadNativeCqlAsync(selected.Result, cancellationToken).WithCancellation(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Guid.TryParseExact(record.Key, "D", out var id) || record.Key != id.ToString("D") ||
                        record.Id != record.Key || !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native key/identity differs from admitted catalogue.");
                    bytes = checked(bytes + Encoding.UTF8.GetByteCount(record.DataJson));
                    if (bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.");
                    var row = Des<Holon>(record.DataJson);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (record.IsDeleted || row.IsDeleted) continue;
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
        catch (NativeQueryConfigurationException cause)
        { return Error("CQL_PROVIDER_CONFIGURATION_REQUIRED", "Provide an explicit configured Weaviate gRPC client for CQL.", cause); }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Weaviate query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Weaviate query failed; no partial rows published.", cause); }
    }
}
