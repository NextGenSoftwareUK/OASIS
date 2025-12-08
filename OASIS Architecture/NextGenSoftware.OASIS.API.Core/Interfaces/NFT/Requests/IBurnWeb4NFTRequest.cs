
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IBurnWeb4NFTRequest : IBurnWeb3NFTRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        //public bool WaitTillNFTBurnt { get; set; }
        //public int WaitForNFTToBurnInSeconds { get; set; }
        //public int AttemptToBurnEveryXSeconds { get; set; }
    }
}