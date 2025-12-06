
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class GetWeb4WalletBalanceRequest : GetWeb3WalletBalanceRequest, IGetWeb4WalletBalanceRequest
    {
        public bool WaitTillBalanceReturned { get; set; }
        public int WaitToGetBalanceInSeconds { get; set; }
        public int AttemptToGetBalanceEveryXSeconds { get; set; }
    }
}