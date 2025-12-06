
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IGetWeb4WalletBalanceRequest : IGetWeb3WalletBalanceRequest
    {
        public bool WaitTillBalanceReturned { get; set; }
        public int WaitToGetBalanceInSeconds { get; set; }
        public int AttemptToGetBalanceEveryXSeconds { get; set; }
    }
}