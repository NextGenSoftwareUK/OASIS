using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISWallet 
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransactionRequest> Transactions { get; set; }
        public int Balance { get; set; }

        public OASISResult<bool> SendTrasaction(IWalletTransactionRequest transation);
        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransactionRequest transation);
        public OASISResult<bool> SendNFT(IWalletTransactionRequest transation);
        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transation);
    }
}