
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IProviderWallet
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletAddress { get; set; } //Hash of Public Key (shorter version).
        public List<IWalletTransaction> Transactions {get;set;}
        public ProviderType ProviderType { get; set; }
        public int Balance { get; set; }
    }
}
