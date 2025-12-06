
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class BurnWeb4TokenRequest : BurnWeb3TokenRequest, IBurnWeb4TokenRequest
    {
        public bool WaitTillTokenBurnt { get; set; }
        public int WaitForTokenToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}