using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IBurnWeb3TokenRequest
    {
        public Guid BurntByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
        public string OwnerPublicKey { get; init; }
        public string OwnerPrivateKey { get; init; }
        public string OwnerSeedPhrase { get; init; }
    }
}