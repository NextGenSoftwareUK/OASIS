
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Keys
{
    public class LinkProviderKeyToAvatarParams : ProviderKeyForAvatarParams
    {
        public Guid WalletId { get; set; }
        public string ProviderKey { get; set; }
        public string WalletAddress { get; set; }
        public string WalletAddressSegwitP2SH {  get; set; }
        public bool ShowPublicKey { get; set; }
        public bool ShowPrivateKey { get; set; }
        public bool ShowSecretRecoveryWords { get; set; }
    }
}