
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class GetWeb3WalletBalanceRequest : IGetWeb3WalletBalanceRequest
    {
        public string WalletAddress { get; set; }
    }
}