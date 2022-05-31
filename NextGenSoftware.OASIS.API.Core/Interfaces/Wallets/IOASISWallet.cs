
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISWallet 
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransaction> Transactions { get; set; }
        public int Balance { get; set; }
    }
}