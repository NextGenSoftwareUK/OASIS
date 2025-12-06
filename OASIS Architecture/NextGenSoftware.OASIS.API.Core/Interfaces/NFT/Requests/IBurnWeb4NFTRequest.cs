
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IBurnWeb4NFTRequest : IBurnWeb3NFTRequest
    {
        public bool WaitTillNFTBurnt { get; set; }
        public int WaitForNFTToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}