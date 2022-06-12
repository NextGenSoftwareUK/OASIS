
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISNFTProvider : IOASISProvider
    {
        //TODO: More to come soon... ;-)
        public OASISResult<bool> SendNFT(IWalletTransaction transation);
        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation);
    }
}