using System;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private async Task<(Nethereum.RPC.Eth.DTOs.TransactionReceipt Receipt,AvatarTombstone Tombstone,bool AlreadyApplied)>
            SoftDeleteStoredAvatarAsync(Guid id,AvatarTombstone supplied = null)
        {
            if(supplied != null && supplied.AvatarId != id) throw new ArgumentException("Tombstone identity mismatch.");
            var key=HashUtility.GetNumericHash(id.ToString());
            var block=new Nethereum.RPC.Eth.DTOs.BlockParameter(await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var row=(await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(key,block)).ReturnValue1;
            var avatar=DeserializePrivateStorage<Avatar>("avatar",row.EntityId,row.Info,id);
            if(supplied == null && avatar.DeletedDate != DateTime.MinValue)
                throw new InvalidOperationException("Ethereum avatar was already soft deleted.");
            AvatarDetail detail=null;
            string detailInfo=null;
            if(await _nextGenSoftwareOasisService.GetAvatarDetailsCountQueryAsync(block) > 0)
            {
                var detailRow=(await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(key,block)).ReturnValue1;
                if(detailRow.EntityId != 0 || !string.IsNullOrEmpty(detailRow.Info))
                {
                    detail=DeserializePrivateStorage<AvatarDetail>("avatar-detail",detailRow.EntityId,detailRow.Info,id);
                    detailInfo=detailRow.Info;
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
                        throw new InvalidOperationException("Ethereum replica has a conflicting committed tombstone.");
                if(Matches(avatar) && (detail == null || Matches(detail))) return (null,tombstone,true);
            }
            avatar.DeletedDate=tombstone.DeletedAtUtc; avatar.DeletedByAvatarId=tombstone.DeletedByAvatarId; avatar.IsActive=false;
            var request=new ContractDefinition.UpdateAvatarPairByIdentityFunction { EntityId=key,AvatarId=id.ToString(),
                ExpectedAvatarHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(row.Info)),
                AvatarInfo=SerializePrivateStorage("avatar",avatar),ExpectedDetailHash=new byte[32],DetailInfo=string.Empty };
            if(detail != null)
            {
                detail.DeletedDate=tombstone.DeletedAtUtc; detail.DeletedByAvatarId=tombstone.DeletedByAvatarId; detail.IsActive=false;
                request.ExpectedDetailHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(detailInfo));
                request.DetailInfo=SerializePrivateStorage("avatar-detail",detail);
            }
            var receipt=await Web3Client.Eth.GetContractHandler(ContractAddress).SendRequestAndWaitForReceiptAsync(request);
            return (receipt,tombstone,false);
        }
    }
}
