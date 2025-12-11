using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class KeyValuePairAndWallet : IKeyPairAndWallet
    {
        public string PrivateKey { get; set; }
        
        public string PublicKey { get; set; }
        
        public string WalletAddressLegacy { get; set; }
        
        public string WalletAddressSegwitP2SH { get; set; }

        public KeyValuePairAndWallet()
        {
            
        }
    }
}






