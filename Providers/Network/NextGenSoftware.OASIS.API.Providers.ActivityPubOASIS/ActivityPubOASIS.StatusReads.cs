using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public partial class ActivityPubOASIS
{
    private static OASISResult<T> RejectProjectionGuid<T>()
    {
        var result=new OASISResult<T>();
        OASISErrorHandling.HandleError(ref result,"Use the native Mastodon account/status key; projection GUIDs are not reversible native IDs.",
            new NotSupportedException("An OASIS projection GUID has no reversible Mastodon native identity."));
        return result;
    }
    private async Task<OASISResult<IHolon>> ReadMastodonStatusAsync(string key,bool children,bool providerChildren,int depth,int version,bool recursive)
    {
        var result=new OASISResult<IHolon>();
        try{
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A native status ID is required.",nameof(key));
            if(depth<0)throw new ArgumentOutOfRangeException(nameof(depth));
            if(version!=0)throw new NotSupportedException("Native status edit history is not an OASIS holon version.");
            var selected=key.Trim();
            if(!_isActivated){var activated=await ActivateProviderAsync();if(activated.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IHolon>(activated);}
            using var response=await _httpClient.GetAsync("/api/v1/statuses/"+Uri.EscapeDataString(selected));response.EnsureSuccessStatusCode();
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var status=document.RootElement;
            var holon=ProjectMastodonStatus(status,selected);
            result.Result=children&&providerChildren?await ReadMastodonReplyGraphAsync(holon,recursive,depth):holon;
            result.LoadedCount=1;
            result.IsLoaded=true;
        }catch(Exception error){
            OASISErrorHandling.HandleError(ref result,"Mastodon native status read failed.",error);
            if(error is HttpRequestException http&&http.StatusCode==HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }

    private Holon ProjectMastodonStatus(JsonElement status,string selected=null)
    {
            string Required(JsonElement node,string name,bool allowEmpty=false){
                if(node.ValueKind!=JsonValueKind.Object||!node.TryGetProperty(name,out var field)||field.ValueKind!=JsonValueKind.String||(!allowEmpty&&string.IsNullOrWhiteSpace(field.GetString())))
                    throw new InvalidDataException("Missing native status field: "+name);
                return field.GetString();
            }
            var id=Required(status,"id");if(selected!=null&&id!=selected)throw new InvalidDataException("Native status identity differs from requested ID.");
            var uri=Required(status,"uri");
            if(!Uri.TryCreate(uri,UriKind.Absolute,out var objectUri)||(objectUri.Scheme!=Uri.UriSchemeHttps&&!(objectUri.Scheme==Uri.UriSchemeHttp&&objectUri.IsLoopback)))throw new InvalidDataException("Invalid native status URI.");
            var content=Required(status,"content",true);var created=Required(status,"created_at");
            if(!DateTimeOffset.TryParse(created,CultureInfo.InvariantCulture,DateTimeStyles.None,out var timestamp))throw new InvalidDataException("Invalid native status timestamp.");
            if(!status.TryGetProperty("account",out var account))throw new InvalidDataException("Missing native status author.");
            var authorId=Required(account,"id");var authorAcct=Required(account,"acct");
            var holon=new Holon{Id=MastodonProjectionId("status",id),CreatedByAvatarId=MastodonProjectionId("account",authorId),Name="Mastodon status "+id,CreatedDate=timestamp.UtcDateTime,HolonType=Core.Enums.HolonType.Holon};
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS]=id;
            holon.MetaData["MastodonObjectUri"]=objectUri.AbsoluteUri;holon.MetaData["MastodonContentHtml"]=content;
            holon.MetaData["MastodonAuthorId"]=authorId;holon.MetaData["MastodonAuthorAcct"]=authorAcct;
            if(status.TryGetProperty("in_reply_to_id",out var reply)&&reply.ValueKind!=JsonValueKind.Null){
                if(reply.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(reply.GetString()))throw new InvalidDataException("Invalid native reply ID.");
                holon.ParentHolonId=MastodonProjectionId("status",reply.GetString());
            }
            if(status.TryGetProperty("visibility",out var visibility)&&visibility.ValueKind==JsonValueKind.String)holon.MetaData["MastodonVisibility"]=visibility.GetString();
            if(status.TryGetProperty("replies_count",out var count)){
                if(!count.TryGetInt64(out var replies)||replies<0)throw new InvalidDataException("Invalid native reply count.");
                holon.MetaData["MastodonRepliesCount"]=replies;
            }
            return holon;
    }
}
