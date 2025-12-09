
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IGetWeb3WalletBalanceRequest
    {
        public string WalletAddress { get; set; }
    }
}