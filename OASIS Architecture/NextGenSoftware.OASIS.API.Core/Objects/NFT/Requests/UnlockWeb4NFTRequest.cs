
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class UnlockWeb4NFTRequest : UnlockWeb3NFTRequest, IUnlockWeb3NFTRequest
    {
        public bool WaitTillNFTUnlocked { get; set; }
        public int WaitForNFTToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}