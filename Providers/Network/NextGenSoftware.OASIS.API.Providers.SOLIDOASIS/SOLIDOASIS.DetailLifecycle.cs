using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public partial class SOLIDOASIS
{
    public Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id,bool softDelete=true)
        =>DeleteStandaloneDetailAsync(id,softDelete);
    public OASISResult<bool> DeleteAvatarDetail(Guid id,bool softDelete=true)
        =>DeleteAvatarDetailAsync(id,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email,bool softDelete=true)
        =>DeleteDetailByIdentityAsync(email,true,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email,bool softDelete=true)
        =>DeleteAvatarDetailByEmailAsync(email,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username,bool softDelete=true)
        =>DeleteDetailByIdentityAsync(username,false,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username,bool softDelete=true)
        =>DeleteAvatarDetailByUsernameAsync(username,softDelete).GetAwaiter().GetResult();

    private async Task<OASISResult<bool>> DeleteDetailByIdentityAsync(string identity,bool email,bool softDelete)
    {
        var selected=await FindPrivateRecordAsync<IAvatarDetail>(identity,record=>email?record.Email:record.Username,0,includeDeleted:true);
        if(selected.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail,bool>(selected);
        if(selected.Result==null)return new OASISResult<bool>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="SOLID detail identity was not found."};
        return await DeleteStandaloneDetailAsync(selected.Result.Id,softDelete,record=>string.Equals((email?record.Email:record.Username)?.Trim(),identity.Trim(),StringComparison.OrdinalIgnoreCase));
    }

    private async Task<OASISResult<bool>> DeleteStandaloneDetailAsync(Guid id,bool softDelete,Func<IAvatarDetail,bool> selectedIdentity=null)
    {
        var result=new OASISResult<bool>();
        try{
            RequirePrivateStorage();
            var observed=await ReadPrivateRecordStateAsync<IAvatarDetail>(id);
            RequireStrongDeleteTag(observed.ETag);
            if(selectedIdentity!=null&&!selectedIdentity(observed.Record))throw new StorageLifecycleConflictException("SOLID detail identity changed before deletion.");
            if(softDelete){
                if(observed.Record.DeletedDate==DateTime.MinValue){
                    observed.Record.DeletedDate=DateTime.UtcNow;observed.Record.IsActive=false;
                    var saved=await SavePrivateRecordAsync(observed.Record,observed.ETag,expectedTurtle:observed.Turtle);
                    if(saved.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail,bool>(saved);
                }
            }else await DeletePrivateResourceAsync(_privateRecords.Resource<IAvatarDetail>(id),observed.ETag);
            result.Result=true;result.IsDeleted=true;
        }catch(Exception cause){
            OASISErrorHandling.HandleError(ref result,"SOLID standalone detail deletion failed.",cause);
            if(cause is HttpRequestException http&&http.StatusCode==HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
}
