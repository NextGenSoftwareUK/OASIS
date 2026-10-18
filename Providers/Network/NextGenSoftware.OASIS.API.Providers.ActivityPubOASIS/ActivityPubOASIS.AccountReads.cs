using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public partial class ActivityPubOASIS
{
    private async Task<OASISResult<IAvatar>> ReadMastodonAccountAsync(string selector,bool nativeId,int version)
    {
        var result=new OASISResult<IAvatar>();
        try{
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(string.IsNullOrWhiteSpace(selector))throw new ArgumentException("A nonblank native account ID or acct handle is required.",nameof(selector));
            if(version!=0)throw new NotSupportedException("Mastodon public account reads do not expose OASIS record history.");
            var selected=selector.Trim();
            if(!_isActivated){
                var activated=await ActivateProviderAsync();
                if(activated.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatar>(activated);
            }
            var path=nativeId?"/api/v1/accounts/"+Uri.EscapeDataString(selected):"/api/v1/accounts/lookup?acct="+Uri.EscapeDataString(selected);
            using var response=await _httpClient.GetAsync(path);response.EnsureSuccessStatusCode();
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var account=document.RootElement;
            string Required(string name){
                if(account.ValueKind!=JsonValueKind.Object||!account.TryGetProperty(name,out var field)||field.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(field.GetString()))
                    throw new InvalidDataException("Missing native Mastodon account field: "+name);
                return field.GetString();
            }
            var id=Required("id");var username=Required("username");var acct=Required("acct");var profile=Required("url");
            if(nativeId&&!string.Equals(selected,id,StringComparison.Ordinal))throw new InvalidDataException("Returned native account ID differs from the requested ID.");
            if(!Uri.TryCreate(profile,UriKind.Absolute,out var profileUri)||(profileUri.Scheme!=Uri.UriSchemeHttps&&!(profileUri.Scheme==Uri.UriSchemeHttp&&profileUri.IsLoopback)))
                throw new InvalidDataException("Expected an absolute HTTP(S) public profile URL.");
            var origin=new Uri(_instanceUrl).GetLeftPart(UriPartial.Authority);
            // Stable projection identity is scoped to the observing instance's native ID, not a random or native GUID.
            var hash=SHA256.HashData(Encoding.UTF8.GetBytes("oasis-mastodon-account-v1\n"+origin+"\n"+id));
            var projectedId=new Guid(hash.AsSpan(0,16));
            var avatar=new Avatar{Id=projectedId,Username=acct};
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS]=id;
            avatar.ProviderUsername[Core.Enums.ProviderType.ActivityPubOASIS]=acct;
            avatar.MetaData["MastodonInstance"]=origin;avatar.MetaData["MastodonProfileUrl"]=profileUri.AbsoluteUri;
            avatar.MetaData["MastodonLocalUsername"]=username;
            if(account.TryGetProperty("display_name",out var display)&&display.ValueKind==JsonValueKind.String)
                avatar.MetaData["MastodonDisplayName"]=display.GetString();
            result.Result=avatar;result.LoadedCount=1;
        }catch(Exception error){
            OASISErrorHandling.HandleError(ref result,"Mastodon public account read failed.",error);
            if(error is HttpRequestException http&&http.StatusCode==HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
}
