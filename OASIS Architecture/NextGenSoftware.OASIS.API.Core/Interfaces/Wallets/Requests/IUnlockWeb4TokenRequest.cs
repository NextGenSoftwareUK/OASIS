
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IUnlockWeb4TokenRequest : IUnlockWeb3TokenRequest
    {
        public bool WaitTillTokenUnlocked { get; set; }
        public int WaitForTokenToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}