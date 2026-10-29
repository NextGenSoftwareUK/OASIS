using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.AzureStorageOASIS;
    public CosmicQueryCapabilities QueryCapabilities => AzureBlobCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Azure Blob target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selected = AzureBlobCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Azure Blob provider before querying.");

            const int budget = 4 * 1024 * 1024;
            var payloadBytes = 0;
            var rows = new List<IHolon>();
            var container = _serviceClient.GetBlobContainerClient(_holonContainer);
            foreach (var id in selected.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key = CanonicalHolonKey(id);
                try
                {
                    // Stream selected canonical keys, never list or scan a container.
                    // Bound both declared and actual bytes before canonical decoding.
                    var response = await container.GetBlobClient(key)
                        .DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    using var native = response.GetRawResponse();
                    await using var content = response.Value.Content;
                    var remaining = budget - payloadBytes;
                    if (response.Value.Details.ContentLength > remaining)
                        return Error("CQL_QUERY_LIMIT", "The canonical response exceeds 4 MiB.");
                    using var buffer = new MemoryStream();
                    var chunk = new byte[Math.Min(65536, remaining + 1)];
                    int read;
                    while ((read = await content.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        payloadBytes = checked(payloadBytes + read);
                        if (payloadBytes > budget)
                            return Error("CQL_QUERY_LIMIT", "The canonical response exceeds 4 MiB.");
                        buffer.Write(chunk, 0, read);
                    }
                    var json = new UTF8Encoding(false, true).GetString(buffer.ToArray());
                    var row = await ReadBlobHistoryAsync<Holon>(json, 0).ConfigureAwait(false);
                    if (row == null || row.Id != id || HolonKey(row) != key || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Azure Blob record differs from the authorized canonical identity.");
                    if (row.IsDeleted) continue;
                    row.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                    row.ProviderUniqueStorageKey[QueryProviderType] = key;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
                catch (RequestFailedException cause) when (cause.Status == 404 && cause.ErrorCode == "BlobNotFound")
                {
                    // Container/permission/transport failures must not become absence.
                }
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Explicit caller context prevents the legacy SDK-timeout classifier
            // from turning a cancelled query into a provider transport outage.
            return Error("CQL_QUERY_CANCELLED", "Azure Blob query cancelled; no partial result published.",
                new OperationCanceledException(cancellationToken));
        }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Azure Blob query failed; no partial result published.", cause); }
    }
}
