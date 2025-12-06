
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IBurnWeb4TokenRequest : IBurnWeb3TokenRequest
    {
        public bool WaitTillTokenBurnt { get; set; }
        public int WaitForTokenToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}