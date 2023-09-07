
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISNFTProvider : IOASISProvider
    {
        //TODO: More to come soon... ;-)
        public OASISResult<TransactionRespone> SendNFT(INFTWalletTransaction transation);
        public Task<OASISResult<TransactionRespone>> SendNFTAsync(INFTWalletTransaction transation);
    }
}