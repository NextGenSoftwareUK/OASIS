
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class GetWeb3TransactionsRequest : IGetWeb3TransactionsRequest
    {
        public string WalletAddress { get; set; }
    }
}