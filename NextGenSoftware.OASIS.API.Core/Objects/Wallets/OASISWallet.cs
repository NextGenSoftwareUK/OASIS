using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class OASISWallet : IOASISWallet
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransactionRequest> Transactions { get; set; }
        public int Balance { get; set; }

        public OASISResult<bool> SendNFT(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public OASISResult<bool> SendTrasaction(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }
    }
}