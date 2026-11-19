using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => StorageProviderType;
    public CosmicQueryCapabilities QueryCapabilities => DynamoCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        OASISResult<IReadOnlyList<IHolon>> Rows(IHolon[] rows)
            => new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted Dynamo/AWS target is required.");
            if (maximumResults < 1 || maximumResults > 10000) return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var compiled = DynamoCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (compiled.IsError) return Error(compiled.ErrorCode, compiled.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the explicitly configured Dynamo/AWS provider before querying.");
            if (compiled.Result.Count == 0) return Rows(Array.Empty<IHolon>());
            var response = await _client.BatchGetItemAsync(new BatchGetItemRequest {
                RequestItems = new() { ["OasisHolons"] = new KeysAndAttributes {
                    ConsistentRead = true,
                    Keys = compiled.Result.Select(id => new Dictionary<string, AttributeValue> { ["id"] = new() { S = id.ToString("D") } }).ToList() } }
            }, cancellationToken).ConfigureAwait(false);
            if (response == null || response.HttpStatusCode != HttpStatusCode.OK || response.Responses == null
                || response.Responses.Count != 1 || !response.Responses.TryGetValue("OasisHolons", out var items) || items == null)
                throw new InvalidDataException("Native BatchGetItem returned an incomplete acknowledgement.");
            if (response.UnprocessedKeys?.Count > 0)
                return Error("CQL_SOURCE_INCOMPLETE", "Native keys remain unprocessed; no retry or partial result is published.");
            var expected = compiled.Result.ToHashSet(); var records = new Dictionary<Guid, IHolon>(); var bytes = 0;
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = ReadNativeItemIdentity(item);
                if (!expected.Contains(id) || records.ContainsKey(id)) throw new InvalidDataException("Native selection returned an unexpected or duplicate identity.");
                if (!item.TryGetValue("data_json", out var json) || json.S == null) throw new InvalidDataException("Canonical native payload is absent.");
                bytes = checked(bytes + Encoding.UTF8.GetByteCount(json.S));
                if (bytes > 4 * 1024 * 1024) return Error("CQL_QUERY_LIMIT", "The complete native response exceeds the 4 MiB canonical payload budget.");
                var row = ReadCanonicalIdentity<Holon>(item, id);
                if (!plan.Query.ReadAccess.CanRead(row)) throw new InvalidDataException("Native identity escaped the authorized catalogue.");
                row.Children = Array.Empty<IHolon>();
                records.Add(id, row);
            }
            // Only visibility and paging remain after native indexed key selection.
            // Strong reads are per item; BatchGetItem is not a multi-item snapshot.
            var visible = compiled.Result.Where(id => records.TryGetValue(id, out var row) && !row.IsDeleted).Select(id => records[id]);
            var page = visible.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? compiled.Result.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested(); return Rows(page);
        }
        catch (OperationCanceledException error) { return Error("CQL_QUERY_CANCELLED", "The native query was cancelled.", error); }
        catch (Exception error) { return Error("CQL_QUERY_FAILED", "The native Dynamo/AWS key-set query failed; no partial result returned.", error); }
    }
}
