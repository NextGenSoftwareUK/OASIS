using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ExportStoredHolonsForAvatarAsync(Guid id,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            return await ReadHolonCollectionAsync(row=>row.CreatedByAvatarId==id || row.ParentHolonId==id,true,true,0,0,true,version);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,"Solana avatar holon export failed: "+cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportStoredHolonsForSelectorAsync(string selector,bool email,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonempty avatar selector is required.");
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            // Username/email resolve the current identity. Version applies to exported roots,
            // not to the avatar's independent revision or provider-linked descendants.
            var avatar=email ? await LoadAvatarByEmailAsync(selector):await LoadAvatarByUsernameAsync(selector);
            if(avatar.IsError)
            {
                OASISErrorHandling.HandleError(ref result,avatar.Message,avatar.Exception);
                result.ErrorCode=avatar.ErrorCode; return result;
            }
            if(avatar.Result==null) return new OASISResult<IEnumerable<IHolon>> {IsError=true,ErrorCount=1,
                ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="No Solana avatar matches the export selector."};
            return await ExportStoredHolonsForAvatarAsync(avatar.Result.Id,version);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,"Solana avatar selector export failed: "+cause.Message,cause);}
        return result;
    }
}
