using System;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider : IAvatarTombstoneStorageProvider
{
    public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
        => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

    public Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
    {
        return DeletePrivateAvatarAsync(tombstone?.AvatarId ?? Guid.Empty,true,tombstone);
    }

    private static bool MatchesTombstone(IHolonBase record,AvatarTombstone tombstone)
        => !record.IsActive && record.DeletedDate==tombstone.DeletedAtUtc && record.DeletedByAvatarId==tombstone.DeletedByAvatarId;

    private async Task<OASISResult<bool>> DeletePrivateAvatarAsync(Guid id,bool soft,AvatarTombstone supplied=null)
    {
        var result=new OASISResult<bool>(false);
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty Web3Core avatar identity is required.",nameof(id));
            if(_web3CoreOASIS==null) throw new InvalidOperationException(Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            var pair=await _web3CoreOASIS.ReadAvatarPairAsync((uint)HashUtility.GetNumericHash(id.ToString()));
            if(IsPrivateRecordAbsent(pair.Avatar)) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Web3Core avatar not found.";return result;}
            var avatar=DecodePrivateRecord<Avatar>("avatar",id,pair.Avatar);
            var detail=IsPrivateRecordAbsent(pair.Detail) ? null:DecodePrivateRecord<AvatarDetail>("detail",id,pair.Detail);
            AvatarTombstone patch=null;
            if(soft)
            {
                patch=supplied ?? new AvatarTombstone(id,!avatar.IsActive && avatar.DeletedDate!=default ? avatar.DeletedDate:DateTime.UtcNow,!avatar.IsActive ? avatar.DeletedByAvatarId:Guid.Empty);
                if(!avatar.IsActive && !MatchesTombstone(avatar,patch) || detail!=null && !detail.IsActive && !MatchesTombstone(detail,patch))
                    throw new StorageLifecycleConflictException("Web3Core stored tombstone differs from requested committed lifecycle state.");
                if(MatchesTombstone(avatar,patch) && (detail==null || MatchesTombstone(detail,patch)))
                {result.Result=true;result.IsDeleted=true;result.DeletedCount=1;AvatarTombstoneEvidence.Attach(result,patch);return result;}
            }
            var request=new AvatarLifecycleRequest {EntityId=pair.Avatar.EntityId,ExternalId=PrivateExternalId(id),
                ExpectedAvatarHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(pair.Avatar.Info),
                ExpectedDetailHash=detail==null ? new byte[32]:Nethereum.Util.Sha3Keccack.Current.CalculateHash(pair.Detail.Info),HasDetail=detail!=null,HardDelete=!soft};
            if(soft)
            {
                var date=patch.DeletedAtUtc;
                var actor=patch.DeletedByAvatarId;
                avatar.IsActive=false;avatar.DeletedDate=date;avatar.DeletedByAvatarId=actor;
                request.AvatarInfo=Encoding.UTF8.GetBytes(EncodePrivateRecord("avatar",avatar));
                if(detail!=null) {detail.IsActive=false;detail.DeletedDate=date;detail.DeletedByAvatarId=actor;request.DetailInfo=Encoding.UTF8.GetBytes(EncodePrivateRecord("detail",detail));}
            }
            await _web3CoreOASIS.ApplyAvatarLifecycleAsync(request);
            result.Result=true;result.IsSaved=true;
            result.IsDeleted=true;result.DeletedCount=1;
            if(soft) AvatarTombstoneEvidence.Attach(result,patch);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;}
        return result;
    }

    private async Task<OASISResult<bool>> DeletePrivateAvatarSelectorAsync(string selector,bool email,bool soft)
    {
        var loaded=email ? await LoadAvatarByEmailAsync(selector):await LoadAvatarByUsernameAsync(selector);
        if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(loaded);
        return await DeletePrivateAvatarAsync(loaded.Result.Id,soft);
    }
}
