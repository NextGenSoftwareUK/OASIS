
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISWallet 
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransaction> Transactions { get; set; }
        public int Balance { get; set; }

        public OASISResult<bool> SendTrasaction(IWalletTransaction transation);
        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransaction transation);
        public OASISResult<bool> SendNFT(IWalletTransaction transation);
        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation);
    }
}