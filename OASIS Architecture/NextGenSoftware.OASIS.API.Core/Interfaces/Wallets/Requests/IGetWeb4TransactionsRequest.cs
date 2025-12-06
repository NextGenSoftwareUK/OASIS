
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IGetWeb4TransactionsRequest : IGetWeb3TransactionsRequest
    {
        public bool WaitTillTransactionsReturned { get; set; }
        public int WaitToGetTransactionsInSeconds { get; set; }
        public int AttemptToGetTransactionsEveryXSeconds { get; set; }
    }
}