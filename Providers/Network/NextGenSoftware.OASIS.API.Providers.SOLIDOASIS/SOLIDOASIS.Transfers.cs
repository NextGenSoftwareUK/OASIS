using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public partial class SOLIDOASIS
{
    private async Task<OASISResult<bool>> ImportPrivateRecordsAsync(IEnumerable<IHolon> records)
    {
        var saved=await SavePrivateGraphAsync(records,true,true,0,0);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved);
        result.Result=!saved.IsError;return result;
    }
    private async Task<OASISResult<IEnumerable<IHolon>>> ExportPrivateRecordsAsync(Guid? owner,int version)
    {
        if(owner.HasValue&&owner.Value==Guid.Empty){
            var invalid=new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref invalid,"A nonempty avatar identity is required for scoped export.",new ArgumentException("Invalid owner identity.",nameof(owner)));return invalid;
        }
        var result=version==-1?await LoadPrivateRecordsAsync<IHolon>(0,allVersions:true)
            :await QueryPrivateHolonsAsync(HolonType.All,null,false,false,0,0,false,version);
        if(!result.IsError){
            result.Result=result.Result.Where(record=>!record.IsDeleted&&record.DeletedDate==DateTime.MinValue
                &&(!owner.HasValue||record.CreatedByAvatarId==owner.Value)).ToArray();
            result.LoadedCount=result.Result.Count();
        }
        return result;
    }
    private async Task<OASISResult<IEnumerable<IHolon>>> ExportPrivateIdentityAsync(string identity,bool email,int version)
    {
        var avatar=email?await LoadAvatarByEmailAsync(identity,0):await LoadAvatarByUsernameAsync(identity,0);
        if(avatar.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(avatar);
        return await ExportPrivateRecordsAsync(avatar.Result.Id,version);
    }
}
