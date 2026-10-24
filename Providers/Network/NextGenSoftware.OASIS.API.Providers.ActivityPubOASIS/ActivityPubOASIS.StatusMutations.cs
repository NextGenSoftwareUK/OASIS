using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public sealed class MastodonStatusMutationReconciliationException:IOException,INativeMutationReconciliationRequired
{
    public string InstanceOrigin{get;}
    public string NativeStatusId{get;}
    public string OriginalTransactionId=>string.Empty;
    public MastodonStatusMutationReconciliationException(string origin,string id,Exception cause)
        :base("Mastodon status deletion may have applied; reconcile the original status before another mutation.",cause)
        {InstanceOrigin=origin;NativeStatusId=id;}
}

public partial class ActivityPubOASIS
{
    private async Task<OASISResult<IHolon>> DeleteMastodonStatusAsync(string key)
    {
        var result=new OASISResult<IHolon>();var dispatched=false;string selected=null;
        try{
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A native status ID is required.",nameof(key));
            selected=key.Trim();
            if(selected is "." or "..")throw new ArgumentException("A native status ID cannot be a path navigation segment.",nameof(key));
            if(string.IsNullOrWhiteSpace(_accessToken))throw new UnauthorizedAccessException("A Mastodon user token with write:statuses is required.");
            if(!_isActivated){
                var activated=await ActivateProviderAsync();
                if(activated.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IHolon>(activated);
            }
            using var request=new HttpRequestMessage(HttpMethod.Delete,"/api/v1/statuses/"+Uri.EscapeDataString(selected));
            dispatched=true;
            using var response=await _httpClient.SendAsync(request);
            if(response.StatusCode!=HttpStatusCode.OK){
                if((int)response.StatusCode>=400&&(int)response.StatusCode<500&&response.StatusCode!=HttpStatusCode.RequestTimeout){
                    dispatched=false;response.EnsureSuccessStatusCode();
                }
                throw new InvalidDataException("Mastodon DELETE requires the completed HTTP200 status acknowledgement.");
            }
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            // Native service authorizes ownership; its returned ID must acknowledge this exact target.
            var deleted=ProjectMastodonStatus(document.RootElement,selected);
            deleted.DeletedDate=DateTime.UtcNow;deleted.IsActive=false;
            result.Result=deleted;result.IsDeleted=true;result.DeletedCount=1;
        }catch(Exception cause){
            var error=dispatched?new MastodonStatusMutationReconciliationException(_instanceUrl,selected,cause):cause;
            OASISErrorHandling.HandleError(ref result,"Mastodon native status deletion failed.",error);
            if(!dispatched&&cause is HttpRequestException http&&http.StatusCode==HttpStatusCode.NotFound)
                result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
}
