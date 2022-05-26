
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    //TODO: Need to research how other web3 wallets work and then improve upon them in OASIS style! ;-)
    public class ProviderWallet : HolonBase, IProviderWallet
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletAddress { get; set; } //Hash of Public Key (shorter version).
        public string SecretRecoveryPhrase { get; set; }
        public List<IWalletTransaction> Transactions {get;set;}
        public ProviderType ProviderType { get; set; }
        public int Balance { get; set; }
    }
}
