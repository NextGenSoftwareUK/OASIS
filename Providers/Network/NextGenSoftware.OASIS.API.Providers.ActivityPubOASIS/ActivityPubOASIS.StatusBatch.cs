using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public partial class ActivityPubOASIS
{
    /// <summary>One bounded native id[] request (Mastodon4.3+), not a full collection or reply graph.
    /// Missing/inaccessible statuses may be omitted by the native service. Returned roots follow input order.
    /// The40-ID bound is local admission policy, not a claimed server limit.</summary>
    public OASISResult<IReadOnlyList<IHolon>> LoadStatusBatch(IEnumerable<string> nativeStatusIds)
        =>LoadStatusBatchAsync(nativeStatusIds).GetAwaiter().GetResult();

    public Task<OASISResult<IReadOnlyList<IHolon>>> LoadStatusBatchAsync(IEnumerable<string> nativeStatusIds)
        =>ReadMastodonBatchAsync<IHolon>(nativeStatusIds,"/api/v1/statuses",row=>ProjectMastodonStatus(row));

    private async Task<OASISResult<IReadOnlyList<T>>> ReadMastodonBatchAsync<T>(IEnumerable<string> nativeIds,string path,Func<JsonElement,T> project) where T:IHolonBase
    {
        var result=new OASISResult<IReadOnlyList<T>>();
        try{
            ObjectDisposedException.ThrowIf(_disposed,this);
            ArgumentNullException.ThrowIfNull(nativeIds);
            // Bound enumeration as well as dispatch; never enumerate arbitrary input without a limit.
            var ids=nativeIds.Take(41).ToArray();
            if(ids.Length==0||ids.Length>40)throw new ArgumentOutOfRangeException(nameof(nativeIds),"Provide1to40 native IDs.");
            if(ids.Any(id=>id==null||id.Length==0||id.Length>128||id.Any(c=>c<'0'||c>'9')))
                throw new ArgumentException("Native decimal IDs are required.",nameof(nativeIds));
            var requested=ids.ToHashSet(StringComparer.Ordinal);
            if(requested.Count!=ids.Length)throw new ArgumentException("Repeated native IDs are not allowed.",nameof(nativeIds));
            if(!_isActivated){var activated=await ActivateProviderAsync();if(activated.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IReadOnlyList<T>>(activated);}
            using var response=await _httpClient.GetAsync(path+"?"+string.Join("&",ids.Select(id=>"id%5B%5D="+Uri.EscapeDataString(id))));
            response.EnsureSuccessStatusCode();
            if(response.StatusCode!=HttpStatusCode.OK)throw new InvalidDataException("A completed HTTP200 native batch is required.");
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if(document.RootElement.ValueKind!=JsonValueKind.Array||document.RootElement.GetArrayLength()>ids.Length)
                throw new InvalidDataException("Invalid or oversized native batch.");
            var selected=new Dictionary<string,T>(StringComparer.Ordinal);
            foreach(var status in document.RootElement.EnumerateArray()){
                var record=project(status);var id=record.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS];
                if(!requested.Contains(id)||!selected.TryAdd(id,record))throw new InvalidDataException("Native batch returned an unrequested or repeated identity.");
            }
            result.Result=Array.AsReadOnly(ids.Where(selected.ContainsKey).Select(id=>selected[id]).ToArray());
            result.IsLoaded=true;result.LoadedCount=selected.Count;
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Native Mastodon batch read failed.",cause);}
        return result;
    }
}
