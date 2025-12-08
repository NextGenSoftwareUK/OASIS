using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class BurnWeb4NFTRequest : BurnWeb3NFTRequest, IBurnWeb4NFTRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillNFTBurnt { get; set; } = true;
        public int WaitForNFTToBurnInSeconds { get; set; } = 60;
        public int AttemptToBurnEveryXSeconds { get; set; } = 5;
    }
}