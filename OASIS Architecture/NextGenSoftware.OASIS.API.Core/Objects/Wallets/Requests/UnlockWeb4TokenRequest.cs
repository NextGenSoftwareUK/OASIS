
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class UnlockWeb4TokenRequest : UnlockWeb3TokenRequest, IUnlockWeb4TokenRequest
    {
        public bool WaitTillTokenUnlocked { get; set; }
        public int WaitForTokenToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}