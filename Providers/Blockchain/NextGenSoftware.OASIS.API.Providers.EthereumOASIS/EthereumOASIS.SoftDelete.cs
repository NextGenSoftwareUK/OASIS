using System;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private async Task<Nethereum.RPC.Eth.DTOs.TransactionReceipt> SoftDeleteStoredAvatarAsync(Guid id)
        {
            var key=HashUtility.GetNumericHash(id.ToString());
            var block=new Nethereum.RPC.Eth.DTOs.BlockParameter(await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync());
            var row=(await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(key,block)).ReturnValue1;
            var avatar=DeserializePrivateStorage<Avatar>("avatar",row.EntityId,row.Info,id);
            if(avatar.DeletedDate != DateTime.MinValue) throw new InvalidOperationException("Ethereum avatar was already soft deleted.");
            var deleted=DateTime.UtcNow;
            avatar.DeletedDate=deleted; avatar.IsActive=false;
            var request=new ContractDefinition.UpdateAvatarPairByIdentityFunction { EntityId=key,AvatarId=id.ToString(),
                ExpectedAvatarHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(row.Info)),
                AvatarInfo=SerializePrivateStorage("avatar",avatar),ExpectedDetailHash=new byte[32],DetailInfo=string.Empty };
            if(await _nextGenSoftwareOasisService.GetAvatarDetailsCountQueryAsync(block) > 0)
            {
                var detailRow=(await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(key,block)).ReturnValue1;
                if(detailRow.EntityId != 0 || !string.IsNullOrEmpty(detailRow.Info))
                {
                    var detail=DeserializePrivateStorage<AvatarDetail>("avatar-detail",detailRow.EntityId,detailRow.Info,id);
                    detail.DeletedDate=deleted; detail.IsActive=false;
                    request.ExpectedDetailHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(detailRow.Info));
                    request.DetailInfo=SerializePrivateStorage("avatar-detail",detail);
                }
            }
            return await Web3Client.Eth.GetContractHandler(ContractAddress).SendRequestAndWaitForReceiptAsync(request);
        }
    }
}
