
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    public class LinkProviderKeyToAvatarParams : ProviderKeyForAvatarParams
    {
        public Guid WalletId { get; set; }
        public string ProviderKey { get; set; }
        public bool ShowPublicKey { get; set; }
        public bool ShowPrivateKey { get; set; }
    }
}