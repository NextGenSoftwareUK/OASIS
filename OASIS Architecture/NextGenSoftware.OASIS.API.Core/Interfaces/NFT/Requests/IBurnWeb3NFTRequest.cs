using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IBurnWeb3NFTRequest
    {
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
        public bool WaitTillNFTBurnt { get; set; }
        public int WaitForNFTToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}