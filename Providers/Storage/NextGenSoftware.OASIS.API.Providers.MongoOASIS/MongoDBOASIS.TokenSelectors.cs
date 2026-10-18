using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<OASISResult<IAvatar>> ReadTokenAvatarAsync(string value,int version,int kind)
    {
        var result=new OASISResult<IAvatar>();
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(!IsProviderActivated)
            {
                var active=await ActivateProviderAsync();
                if(active.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatar>(active);
            }
            // Preserve native filtered/limited identity queries; tombstones are never credential identities.
            var selected=kind switch {
                0=>await _avatarRepository.GetAvatarAsync(x=>x.VerificationToken==value && x.DeletedDate==DateTime.MinValue && (version==0 || x.Version==version)),
                1=>await _avatarRepository.GetAvatarAsync(x=>x.ResetToken==value && x.DeletedDate==DateTime.MinValue && (version==0 || x.Version==version)),
                _=>await _avatarRepository.GetAvatarAsync(x=>(x.RefreshToken==value || x.RefreshTokens.Any(token=>token.Token==value)) && x.DeletedDate==DateTime.MinValue && (version==0 || x.Version==version))};
            result=DataHelper.ConvertMongoEntityToOASISAvatar(selected);
            if(!result.IsError && result.Result==null) result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=cause;result.Message="Mongo avatar token lookup failed.";
            if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
