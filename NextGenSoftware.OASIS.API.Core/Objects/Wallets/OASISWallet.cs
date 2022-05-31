
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class OASISWallet : IOASISWallet
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransaction> Transactions { get; set; }
        public int Balance { get; set; }
    }
}