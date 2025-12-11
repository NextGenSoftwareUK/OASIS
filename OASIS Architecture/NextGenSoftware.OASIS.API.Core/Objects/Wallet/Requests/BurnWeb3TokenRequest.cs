using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class BurnWeb3TokenRequest : IBurnWeb3TokenRequest
    {
        public Guid BurntByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
        public required string OwnerPublicKey { get; init; }
        public required string OwnerPrivateKey { get; init; }
        public required string OwnerSeedPhrase { get; init; }
    }
}