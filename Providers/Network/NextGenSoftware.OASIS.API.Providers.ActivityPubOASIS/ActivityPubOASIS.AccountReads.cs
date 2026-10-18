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
    private sealed record MastodonPublicProfile(Guid ProjectionId,string NativeId,string LocalUsername,string Acct,string ProfileUrl,string Origin,string DisplayName);
    private Task<OASISResult<IAvatar>> ReadMastodonAccountAsync(string selector,bool nativeId,int version)
        => ReadMastodonProfileAsync<IAvatar>(selector,nativeId,version,profile=>{
            var avatar=new Avatar{Id=profile.ProjectionId,Username=profile.Acct};
            PopulatePublicProfile(avatar,profile);avatar.ProviderUsername[Core.Enums.ProviderType.ActivityPubOASIS]=profile.Acct;
            return avatar;
        });
    private Task<OASISResult<IAvatarDetail>> ReadMastodonDetailAsync(string selector,int version)
        => ReadMastodonProfileAsync<IAvatarDetail>(selector,false,version,profile=>{
            var detail=new AvatarDetail{Id=profile.ProjectionId,Username=profile.Acct};
            PopulatePublicProfile(detail,profile);return detail;
        });
    private static void PopulatePublicProfile(IHolonBase target,MastodonPublicProfile profile)
    {
        target.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS]=profile.NativeId;
        target.MetaData["MastodonInstance"]=profile.Origin;target.MetaData["MastodonProfileUrl"]=profile.ProfileUrl;
        target.MetaData["MastodonLocalUsername"]=profile.LocalUsername;
        if(profile.DisplayName!=null)target.MetaData["MastodonDisplayName"]=profile.DisplayName;
    }
    private async Task<OASISResult<T>> ReadMastodonProfileAsync<T>(string selector,bool nativeId,int version,Func<MastodonPublicProfile,T> project)
    {
        var result=new OASISResult<T>();
        try{
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(string.IsNullOrWhiteSpace(selector))throw new ArgumentException("A nonblank native account ID or acct handle is required.",nameof(selector));
            if(version!=0)throw new NotSupportedException("Mastodon public account reads do not expose OASIS record history.");
            var selected=selector.Trim();
            if(!_isActivated){
                var activated=await ActivateProviderAsync();
                if(activated.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,T>(activated);
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
            var displayName=account.TryGetProperty("display_name",out var display)&&display.ValueKind==JsonValueKind.String?display.GetString():null;
            result.Result=project(new MastodonPublicProfile(projectedId,id,username,acct,profileUri.AbsoluteUri,origin,displayName));result.LoadedCount=1;
        }catch(Exception error){
            OASISErrorHandling.HandleError(ref result,"Mastodon public account read failed.",error);
            if(error is HttpRequestException http&&http.StatusCode==HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
}
