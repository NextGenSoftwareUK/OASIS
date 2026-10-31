using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS
{
    public Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id,bool softDelete=true)=>DeleteStandaloneDetailAsync(id,softDelete);
    public OASISResult<bool> DeleteAvatarDetail(Guid id,bool softDelete=true)=>DeleteAvatarDetailAsync(id,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email,bool softDelete=true)=>DeleteDetailByIdentityAsync(email,true,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email,bool softDelete=true)=>DeleteAvatarDetailByEmailAsync(email,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username,bool softDelete=true)=>DeleteDetailByIdentityAsync(username,false,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username,bool softDelete=true)=>DeleteAvatarDetailByUsernameAsync(username,softDelete).GetAwaiter().GetResult();

    private async Task<OASISResult<bool>> DeleteDetailByIdentityAsync(string selector,bool email,bool softDelete)
    {
        // Lifecycle resolution includes tombstones so identity-addressed hard deletion
        // can remove a previously soft-deleted detail. Ordinary public reads stay filtered.
        var selected=await ReadSelectedDetailAsync(selector,email,0,includeDeleted:true);
        if(selected.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail,bool>(selected);
        if(selected.Result==null)return new(){IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Azure detail selector returned no record."};
        return await DeleteStandaloneDetailAsync(selected.Result.Id,softDelete,
            row=>string.Equals(email?row.Email:row.Username,selector,StringComparison.OrdinalIgnoreCase));
    }

    private async Task<OASISResult<bool>> DeleteStandaloneDetailAsync(Guid id,bool softDelete,Func<IAvatarDetail,bool> selectedIdentity=null)
    {
        var result=new OASISResult<bool>();
        try{
            if(id==Guid.Empty)throw new ArgumentException("A nonempty Azure detail identity is required.",nameof(id));
            var historyEnabled=IsVersionControlEnabled;
            var key=id.ToString();var blob=_serviceClient.GetBlobContainerClient(_detailContainer).GetBlobClient(key);
            var observed=(await blob.DownloadContentAsync()).Value;
            if(string.IsNullOrWhiteSpace(observed.Details.ETag.ToString()))throw new InvalidDataException("Azure detail observation has no ETag.");
            var json=observed.Content.ToString();DecodeDetail(json,key);
            var detail=await ReadBlobHistoryAsync<AvatarDetail>(json,0)??throw new InvalidDataException("Azure current detail is missing.");
            if(selectedIdentity!=null&&!selectedIdentity(detail))throw new StorageLifecycleConflictException("Azure detail identity changed after selection; refusing deletion.");
            if(softDelete){
                if(!detail.IsDeleted||detail.IsActive){
                    if(!detail.IsDeleted)detail.DeletedDate=DateTime.UtcNow;
                    detail.IsActive=false;
                    var payload=BinaryData.FromString((await StageBlobHistoryAsync(detail,json,historyEnabled)).ToString(Newtonsoft.Json.Formatting.None));
                    await WriteLifecycleBlobAsync(blob,observed.Details.ETag,payload);
                }
            }else await WriteLifecycleBlobAsync(blob,observed.Details.ETag,null);
            result.Result=true;result.IsDeleted=true;result.IsSaved=true;
        }catch(Exception cause){
            OASISErrorHandling.HandleError(ref result,"Azure standalone detail lifecycle failed.",cause);
            if(cause is StorageLifecycleConflictException||cause is RequestFailedException conflict&&conflict.Status==412)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
            else if(cause is RequestFailedException absent&&absent.Status==404&&absent.ErrorCode=="BlobNotFound")result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
            else if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
