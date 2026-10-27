using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

/// <summary>Native Mastodon status-point CQL only; no storage or general timeline-query claim.</summary>
public partial class ActivityPubOASIS : ICosmicQueryStorageProvider
{
    public const string CqlStatusKey = "MastodonStatusId";
    private const int CqlMaximumStatusBytes = 1024 * 1024;
    private static readonly CosmicQueryCapabilities StatusPointCapabilities =
        CosmicQueryCapabilities.ForStringMetadataPointLookup(CqlStatusKey);
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ActivityPubOASIS;
    public CosmicQueryCapabilities QueryCapabilities => StatusPointCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return CqlError("CQL_PLAN_TARGET_UNAVAILABLE","A single admitted Mastodon status-point target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return CqlError("CQL_QUERY_LIMIT","The CQL result budget must be between one and 10,000.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return CqlError(admitted.ErrorCode,admitted.Message);
            var key = QueryCapabilities.StringPointLookup.SelectKey(plan.Query).Result;
            cancellationToken.ThrowIfCancellationRequested();
            if (!_isActivated)
                return CqlError("CQL_PROVIDER_INACTIVE","Explicitly activate the configured Mastodon provider before querying.");
            if (plan.Query.Offset > 0 || plan.Query.Limit == 0) return CqlRows();

            // One native point GET. Never enumerate a timeline or switch endpoints after failure.
            using var request = new HttpRequestMessage(HttpMethod.Get,"/api/v1/statuses/"+Uri.EscapeDataString(key));
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await _httpClient.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return CqlRows();
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > CqlMaximumStatusBytes)
                return CqlError("CQL_QUERY_LIMIT","The native status response exceeds the bounded payload limit.");
            using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var body = new MemoryStream();
            var buffer = new byte[16384];
            int count;
            while ((count = await source.ReadAsync(buffer.AsMemory(),cancellationToken)) != 0)
            {
                if (body.Length + count > CqlMaximumStatusBytes)
                    return CqlError("CQL_QUERY_LIMIT","The native status response exceeds the bounded payload limit.");
                body.Write(buffer,0,count);
            }
            cancellationToken.ThrowIfCancellationRequested();
            using var document = JsonDocument.Parse(body.ToArray());
            var status = document.RootElement;
            if (!status.TryGetProperty("visibility",out var visibility) || visibility.ValueKind != JsonValueKind.String
                || visibility.GetString() is not ("public" or "unlisted"))
                return CqlError("CQL_VISIBILITY_VIOLATION","This native CQL profile publishes only explicitly public or unlisted statuses.");
            var holon = ProjectMastodonStatus(status,key);
            holon.Children = Array.Empty<IHolon>();
            cancellationToken.ThrowIfCancellationRequested();
            if (plan.Query.ReadAccess != null && !plan.Query.ReadAccess.CanRead(holon)) return CqlRows();
            return CqlRows(holon);
        }
        catch (OperationCanceledException error) { return CqlError("CQL_QUERY_CANCELLED","The native status query was cancelled.",error); }
        catch (Exception error) { return CqlError("CQL_QUERY_FAILED","The native Mastodon status query failed; no partial results returned.",error); }
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlRows(params IHolon[] rows)
        => new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
    private static OASISResult<IReadOnlyList<IHolon>> CqlError(string code,string message,Exception error=null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
}
