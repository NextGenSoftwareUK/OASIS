
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class GetWeb4TransactionsRequest : GetWeb3TransactionsRequest, IGetWeb4TransactionsRequest
    {
        public bool WaitTillTransactionsReturned { get; set; }
        public int WaitToGetTransactionsInSeconds { get; set; }
        public int AttemptToGetTransactionsEveryXSeconds { get; set; }
    }
}