using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class KeyPairAndWallet : IKeyPairAndWallet
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletAddress { get; set; }
        public string WalletAddressLegacy { get; set; }
        public string WalletAddressSegwitP2SH { get; set; }

        public KeyPairAndWallet()
        {
        }
    }
}
