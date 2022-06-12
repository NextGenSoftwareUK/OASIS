
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class OASISWallet : IOASISWallet
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransaction> Transactions { get; set; }
        public int Balance { get; set; }

        public OASISResult<bool> SendNFT(IWalletTransaction transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation)
        {
            throw new System.NotImplementedException();
        }

        public OASISResult<bool> SendTrasaction(IWalletTransaction transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransaction transation)
        {
            throw new System.NotImplementedException();
        }
    }
}