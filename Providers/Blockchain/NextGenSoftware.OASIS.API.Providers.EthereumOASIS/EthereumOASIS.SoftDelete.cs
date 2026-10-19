using System;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private async Task<(Nethereum.RPC.Eth.DTOs.TransactionReceipt Receipt,AvatarTombstone Tombstone,bool AlreadyApplied)>
            SoftDeleteStoredAvatarAsync(Guid id,AvatarTombstone supplied = null)
        {
            if(supplied != null && supplied.AvatarId != id) throw new ArgumentException("Tombstone identity mismatch.");
            var createHistory=IsVersionControlEnabled;
            var key=HashUtility.GetNumericHash(id.ToString());
            var block=new Nethereum.RPC.Eth.DTOs.BlockParameter(await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var row=(await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(key,block)).ReturnValue1;
            var avatar=DeserializePrivateStorage<Avatar>("avatar",row.EntityId,row.Info,id);
            if(supplied == null && avatar.DeletedDate != DateTime.MinValue)
                throw new StorageLifecycleConflictException("Ethereum avatar was already soft deleted.");
            AvatarDetail detail=null;
            string detailInfo=null;
            string detailIdentity=null;
            System.Numerics.BigInteger detailEntityId=0;
            if(await _nextGenSoftwareOasisService.GetAvatarDetailsCountQueryAsync(block) > 0)
            {
                var detailRow=(await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(key,block)).ReturnValue1;
                if(detailRow.EntityId != 0 || !string.IsNullOrEmpty(detailRow.Info))
                {
                    detail=DeserializePrivateStorage<AvatarDetail>("avatar-detail",detailRow.EntityId,detailRow.Info,id);
                    detailInfo=detailRow.Info;
                    detailIdentity=detailRow.AvatarId;detailEntityId=detailRow.EntityId;
                }
            }
            var tombstone=supplied ?? new AvatarTombstone(id,DateTime.UtcNow,avatar.DeletedByAvatarId);
            bool Matches(Core.Interfaces.IHolonBase record) => record.DeletedDate == tombstone.DeletedAtUtc
                && record.DeletedByAvatarId == tombstone.DeletedByAvatarId && !record.IsActive;
            if(supplied != null)
            {
                foreach(var record in new Core.Interfaces.IHolonBase[] {avatar,detail})
                    if(record != null && record.DeletedDate != DateTime.MinValue
                        && (record.DeletedDate != tombstone.DeletedAtUtc || record.DeletedByAvatarId != tombstone.DeletedByAvatarId))
                        throw new StorageLifecycleConflictException("Ethereum replica has a conflicting committed tombstone.");
                if(Matches(avatar) && (detail == null || Matches(detail))) return (null,tombstone,true);
            }
            avatar.DeletedDate=tombstone.DeletedAtUtc; avatar.DeletedByAvatarId=tombstone.DeletedByAvatarId; avatar.IsActive=false;
            var request=new ContractDefinition.UpdateAvatarPairByIdentityFunction { EntityId=key,AvatarId=id.ToString(),
                ExpectedAvatarHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(row.Info)),
                AvatarInfo=(await StageStorageHistoryAsync("avatar",avatar,row.EntityId,row.AvatarId,row.Info,createHistory)).Ciphertext,ExpectedDetailHash=new byte[32],DetailInfo=string.Empty };
            if(detail != null)
            {
                detail.DeletedDate=tombstone.DeletedAtUtc; detail.DeletedByAvatarId=tombstone.DeletedByAvatarId; detail.IsActive=false;
                request.ExpectedDetailHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(detailInfo));
                request.DetailInfo=(await StageStorageHistoryAsync("avatar-detail",detail,detailEntityId,detailIdentity,detailInfo,createHistory)).Ciphertext;
            }
            var receipt=await SubmitStorageRequestAsync(request);
            return (receipt,tombstone,false);
        }
    }
}
