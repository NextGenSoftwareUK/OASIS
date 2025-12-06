using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class BurnWeb4NFTRequest : BurnWeb3NFTRequest, IBurnWeb4NFTRequest
    {
        public bool WaitTillNFTBurnt { get; set; }
        public int WaitForNFTToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}