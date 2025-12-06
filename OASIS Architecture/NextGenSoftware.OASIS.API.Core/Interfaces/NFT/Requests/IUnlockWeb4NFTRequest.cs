
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IUnlockWeb4NFTRequest : IUnlockWeb3NFTRequest
    {
        public bool WaitTillNFTUnlocked { get; set; }
        public int WaitForNFTToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}