using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
    public OASISResult<bool> DeleteAvatarDetail(Guid id,bool softDelete=true)=>DeleteAvatarDetailAsync(id,softDelete).GetAwaiter().GetResult();
    public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id,bool softDelete=true)
    {
        var result=new OASISResult<bool>();
        try {
            if(id==Guid.Empty)throw new ArgumentException("A nonempty Cosmos AvatarDetail identity is required.");
            if(!IsProviderActivated){var active=await ActivateProviderAsync();if(active.IsError)return active;if(!active.Result)throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");}
            var original=await avatarDetailRepository.GetByIdAsync(id.ToString());
            if(original==null)return new OASISResult<bool>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Cosmos AvatarDetail not found."};
            if(softDelete){
                if(original.IsActive||!original.IsDeleted){var proposal=OasisJson.Project<AvatarDetail>(original);proposal.IsActive=false;if(!proposal.IsDeleted)proposal.DeletedDate=DateTime.UtcNow;
                    if(await avatarDetailRepository.ReplaceIfUnchangedAsync(original,proposal)==null)throw new CosmosMutationReconciliationException(id.ToString(),new System.IO.InvalidDataException("Cosmos detail tombstone lacks acknowledged record."));
                    result.IsSaved=true;
                }
            }else {await avatarDetailRepository.DeleteIfUnchangedAsync(original);result.IsSaved=true;}
            result.Result=true;result.IsDeleted=true;result.DeletedCount=1;
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Cosmos AvatarDetail lifecycle failed.",cause);
            if(cause is StorageLifecycleConflictException||cause is Microsoft.Azure.Cosmos.CosmosException conflict&&conflict.StatusCode==System.Net.HttpStatusCode.PreconditionFailed)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
            else if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";}
        return result;
    }
    private async Task<OASISResult<bool>> DeleteDetailSelectorAsync(string selector,bool email,bool soft)
    {
        var loaded=await ReadCurrentSelectorAsync<IAvatarDetail>(()=>avatarDetailRepository,selector,row=>email?row.Email:row.Username,0);
        if(loaded.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail,bool>(loaded);
        return await DeleteAvatarDetailAsync(loaded.Result.Id,soft);
    }
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username,bool softDelete=true)=>DeleteDetailSelectorAsync(username,false,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username,bool softDelete=true)=>DeleteAvatarDetailByUsernameAsync(username,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email,bool softDelete=true)=>DeleteDetailSelectorAsync(email,true,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email,bool softDelete=true)=>DeleteAvatarDetailByEmailAsync(email,softDelete).GetAwaiter().GetResult();
}
