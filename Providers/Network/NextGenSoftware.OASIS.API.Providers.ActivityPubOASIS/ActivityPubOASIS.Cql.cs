using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static readonly CosmicQueryCapabilities StatusKeySetCapabilities=
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(CqlStatusKey,128,40);
    /// <summary>Explicit enrollment for instances supporting native GET /api/v1/statuses?id[]=...; never falls back to point fan-out.</summary>
    public bool EnableCqlStatusKeySets {get;init;}
    public CosmicQueryCapabilities QueryCapabilities => EnableCqlStatusKeySets ? StatusKeySetCapabilities : StatusPointCapabilities;

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
            if(QueryCapabilities.StringKeySetLookup?.IsKeySetQuery(plan.Query)==true)
                return await ExecuteCqlStatusKeySetAsync(plan,maximumResults,cancellationToken);
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
            using var document=await ReadCqlStatusDocumentAsync(response,cancellationToken);
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
        catch(CqlStatusLimitException error){return CqlError("CQL_QUERY_LIMIT",error.Message,error);}
        catch (OperationCanceledException error) { return CqlError("CQL_QUERY_CANCELLED","The native status query was cancelled.",error); }
        catch (Exception error) { return CqlError("CQL_QUERY_FAILED","The native Mastodon status query failed; no partial results returned.",error); }
    }

    private async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCqlStatusKeySetAsync<T>(
        CosmicQueryPlan<T> plan,int maximumResults,CancellationToken token) where T:IHolon
    {
        var keys=QueryCapabilities.StringKeySetLookup.SelectKeys(plan.Query).Result;
        if(keys.Any(key=>key.Any(c=>c<'0'||c>'9')))
            return CqlError("CQL_INVALID_SOURCE_KEY","Native Mastodon status keys must be decimal IDs.");
        token.ThrowIfCancellationRequested();
        if(!_isActivated)return CqlError("CQL_PROVIDER_INACTIVE","Explicitly activate the configured native batch profile.");
        var path="/api/v1/statuses?"+string.Join("&",keys.Select(key=>"id%5B%5D="+Uri.EscapeDataString(key)));
        using var request=new HttpRequestMessage(HttpMethod.Get,path);request.Headers.Accept.ParseAdd("application/json");
        using var response=await _httpClient.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        using var document=await ReadCqlStatusDocumentAsync(response,token);
        if(document.RootElement.ValueKind!=JsonValueKind.Array || document.RootElement.GetArrayLength()>keys.Count)
            return CqlError("CQL_PROVIDER_RESULT_INVALID","The native key-set endpoint returned an invalid or excessive collection.");
        var allowed=keys.ToHashSet(StringComparer.Ordinal);var records=new Dictionary<string,IHolon>(StringComparer.Ordinal);
        foreach(var status in document.RootElement.EnumerateArray())
        {
            token.ThrowIfCancellationRequested();
            if(status.ValueKind!=JsonValueKind.Object || !status.TryGetProperty("visibility",out var visibility)
                ||visibility.ValueKind!=JsonValueKind.String || visibility.GetString() is not ("public" or "unlisted"))
                return CqlError("CQL_VISIBILITY_VIOLATION","The native collection contains a non-public or unverified status.");
            var record=ProjectMastodonStatus(status);var key=record.ProviderUniqueStorageKey[QueryProviderType];
            if(!allowed.Contains(key)||!records.TryAdd(key,record))
                return CqlError("CQL_PROVIDER_RESULT_INVALID","The native collection returned an unrequested or duplicate status.");
            record.Children=Array.Empty<IHolon>();
        }
        // Native indexed membership selected the records. Authorization and the captured
        // window apply only after the entire bounded native response has validated.
        var visible=new List<IHolon>();
        foreach(var key in keys)
            if(records.TryGetValue(key,out var record) && (plan.Query.ReadAccess==null || plan.Query.ReadAccess.CanRead(record)))visible.Add(record);
        var rows=visible.Skip(plan.Query.Offset).Take(plan.Query.Limit??visible.Count).ToArray();
        if(rows.Length>maximumResults)throw new CqlStatusLimitException("The complete native key-set page exceeds the result budget.");
        token.ThrowIfCancellationRequested();return CqlRows(rows);
    }

    private sealed class CqlStatusLimitException(string message):Exception(message);
    private static async Task<JsonDocument> ReadCqlStatusDocumentAsync(HttpResponseMessage response,CancellationToken token)
    {
        if(response.Content.Headers.ContentLength>CqlMaximumStatusBytes)
            throw new CqlStatusLimitException("The native status response exceeds its bounded payload limit.");
        using var source=await response.Content.ReadAsStreamAsync(token);using var body=new MemoryStream();
        var buffer=new byte[16384];int count;
        while((count=await source.ReadAsync(buffer.AsMemory(),token))!=0)
        {
            if(body.Length+count>CqlMaximumStatusBytes)throw new CqlStatusLimitException("The native status response exceeds its bounded payload limit.");
            body.Write(buffer,0,count);
        }
        token.ThrowIfCancellationRequested();return JsonDocument.Parse(body.ToArray());
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlRows(params IHolon[] rows)
        => new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
    private static OASISResult<IReadOnlyList<IHolon>> CqlError(string code,string message,Exception error=null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
}
