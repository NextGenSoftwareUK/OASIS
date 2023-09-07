using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers
{
    public interface INFTManager
    {
        Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request);
    }
}