using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public partial class ActivityPubOASIS
{
    /// <summary>A single native timeline page, not a complete storage collection or consistent snapshot.</summary>
    public sealed record PublicTimelinePage(IReadOnlyList<IHolon> Statuses,string OlderThanId);

    public OASISResult<PublicTimelinePage> LoadPublicTimelinePage(string olderThanId=null,int limit=20,bool localOnly=false)
        => LoadPublicTimelinePageAsync(olderThanId,limit,localOnly).GetAwaiter().GetResult();

    /// <summary>Read only. The returned cursor is an exclusive native max_id, not an OASIS identity/version.</summary>
    public async Task<OASISResult<PublicTimelinePage>> LoadPublicTimelinePageAsync(string olderThanId=null,int limit=20,bool localOnly=false)
    {
        var result=new OASISResult<PublicTimelinePage>();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(limit<1||limit>40)throw new ArgumentOutOfRangeException(nameof(limit));
            if(olderThanId!=null&&(olderThanId.Length==0||olderThanId.Length>128||olderThanId.Any(c=>c<'0'||c>'9')))
                throw new ArgumentException("A native decimal status cursor is required.",nameof(olderThanId));
            if(!_isActivated){var activated=await ActivateProviderAsync();if(activated.IsError){
                OASISErrorHandling.HandleError(ref result,activated.Message,activated.Exception);return result;}}
            var path="/api/v1/timelines/public?limit="+limit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                +"&local="+(localOnly?"true":"false")+(olderThanId==null?"":"&max_id="+Uri.EscapeDataString(olderThanId));
            using var response=await _httpClient.GetAsync(path);response.EnsureSuccessStatusCode();
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if(json.RootElement.ValueKind!=JsonValueKind.Array||json.RootElement.GetArrayLength()>limit)
                throw new InvalidDataException("Native timeline returned an invalid or oversized page.");
            var records=new List<IHolon>();var seen=new HashSet<string>();System.Numerics.BigInteger? previous=olderThanId==null?null:System.Numerics.BigInteger.Parse(olderThanId);
            foreach(var status in json.RootElement.EnumerateArray())
            {
                var record=ProjectMastodonStatus(status);var id=record.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS];
                if(id.Length>128||id.Length==0||id.Any(c=>c<'0'||c>'9')||!seen.Add(id))throw new InvalidDataException("Invalid or duplicate native timeline identity.");
                var number=System.Numerics.BigInteger.Parse(id);
                if(previous.HasValue&&number>=previous.Value)throw new InvalidDataException("Native timeline cursor did not move strictly backwards.");
                previous=number;records.Add(record);
            }
            result.Result=new PublicTimelinePage(records.AsReadOnly(),records.Count==0?null:records[^1].ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS]);
            result.LoadedCount=records.Count;result.IsLoaded=true;
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Native public timeline page read failed.",cause);}
        return result;
    }
}
