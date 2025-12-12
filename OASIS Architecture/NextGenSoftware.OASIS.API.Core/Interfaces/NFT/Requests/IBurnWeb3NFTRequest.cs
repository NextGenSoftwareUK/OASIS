using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IBurnWeb3NFTRequest 
    {
        Guid BurntByAvatarId { get; set; }
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
        public string OwnerPublicKey { get; init; }  //TODO: Not sure if we need these?
        public string OwnerPrivateKey { get; init; }  //TODO: Not sure if we need these?
        public string OwnerSeedPhrase { get; init; }  //TODO: Not sure if we need these?
        public bool WaitTillNFTBurnt { get; set; }
        public int WaitForNFTToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}