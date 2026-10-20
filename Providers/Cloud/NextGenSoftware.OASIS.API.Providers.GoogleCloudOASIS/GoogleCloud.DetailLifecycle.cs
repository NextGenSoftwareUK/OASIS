using System;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    public OASISResult<bool> DeleteAvatarDetail(Guid id,bool softDelete=true)
        =>DeleteAvatarDetailAsync(id,softDelete).GetAwaiter().GetResult();
    public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id,bool softDelete=true)
    {
        var result=new OASISResult<bool>();
        try {
            if(id==Guid.Empty)throw new ArgumentException("A nonempty AvatarDetail identity is required.");
            var ready=await EnsureDetailStoreAsync(0);if(ready.IsError)return ready;
            RequireSingleAttemptFirestoreCommit(_firestoreDb);
            var reference=_firestoreDb.Collection("avatarDetails").Document(id.ToString());
            var snapshot=await reference.GetSnapshotAsync();
            if(!snapshot.Exists)return new OASISResult<bool>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Firestore AvatarDetail not found."};
            var detail=DecodeAvatarDetail(snapshot.ToDictionary(),snapshot.Id);
            if(!snapshot.UpdateTime.HasValue||snapshot.UpdateTime.Value.Equals(default(Timestamp)))
                throw new System.IO.InvalidDataException("Firestore detail lacks an authoritative revision.");
            if(softDelete&&detail.IsDeleted&&!detail.IsActive)return new OASISResult<bool>(true){IsDeleted=true,DeletedCount=1};
            var condition=Precondition.LastUpdated(snapshot.UpdateTime.Value);
            System.Collections.Generic.Dictionary<string,object> update=null;
            if(softDelete){detail.IsActive=false;if(!detail.IsDeleted)detail.DeletedDate=DateTime.UtcNow;
                update=await StageFirestoreLifecycleUpdateAsync(detail,snapshot.ToDictionary(),snapshot.Id,IsVersionControlEnabled);}
            try {
                RequireFirestoreAcknowledgement(softDelete?await reference.UpdateAsync(update,condition):await reference.DeleteAsync(condition));
            }
            catch(Grpc.Core.RpcException cause)when(cause.StatusCode is Grpc.Core.StatusCode.FailedPrecondition or Grpc.Core.StatusCode.NotFound or Grpc.Core.StatusCode.InvalidArgument){throw;}
            catch(Exception cause){throw new GoogleFirestoreMutationReconciliationException(reference.Path,cause);}
            result.Result=true;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause){HandleFirestoreSaveError(ref result,"Firestore AvatarDetail lifecycle failed.",cause);
            if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";}
        return result;
    }
    private async Task<OASISResult<bool>> DeleteSelectedDetailAsync(string selector,bool email,bool softDelete)
    {
        if(string.IsNullOrWhiteSpace(selector))return new OASISResult<bool>{IsError=true,ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Exception=new ArgumentException("A nonempty AvatarDetail selector is required.")};
        var loaded=email?await LoadAvatarDetailByEmailAsync(selector):await LoadAvatarDetailByUsernameAsync(selector);
        if(loaded==null)return new OASISResult<bool>{IsError=true,Message="Firestore detail selector returned no acknowledgement."};
        if(loaded.IsError||loaded.Result==null)return new OASISResult<bool>{IsError=true,Exception=loaded.Exception,ErrorCode=loaded.ErrorCode,Message=loaded.Message};
        return await DeleteAvatarDetailAsync(loaded.Result.Id,softDelete);
    }
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username,bool softDelete=true)=>DeleteSelectedDetailAsync(username,false,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username,bool softDelete=true)=>DeleteAvatarDetailByUsernameAsync(username,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email,bool softDelete=true)=>DeleteSelectedDetailAsync(email,true,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email,bool softDelete=true)=>DeleteAvatarDetailByEmailAsync(email,softDelete).GetAwaiter().GetResult();
}
