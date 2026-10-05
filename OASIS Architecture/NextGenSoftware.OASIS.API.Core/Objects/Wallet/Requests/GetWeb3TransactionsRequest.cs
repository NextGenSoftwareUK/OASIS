
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class GetWeb3TransactionsRequest : IGetWeb3TransactionsRequest
    {
        public string WalletAddress { get; set; }
    }
}