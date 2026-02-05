using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class BurnWeb3NFTRequest : IBurnWeb3NFTRequest
    {
        public Guid BurntByAvatarId { get; set; }
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public required string OwnerPublicKey { get; init; } //TODO: Not sure if we need these?
        public required string OwnerPrivateKey { get; init; } //TODO: Not sure if we need these?
        public required string OwnerSeedPhrase { get; init; } //TODO: Not sure if we need these?
        public string NFTTokenAddress { get; set; }
        public bool WaitTillNFTBurnt { get; set; } = true;
        public int WaitForNFTToBurnInSeconds { get; set; } = 60;
        public int AttemptToBurnEveryXSeconds { get; set; } = 3;
    }
}