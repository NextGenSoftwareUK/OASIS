
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class GetWeb4WalletBalanceRequest : GetWeb3WalletBalanceRequest, IGetWeb4WalletBalanceRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillBalanceReturned { get; set; }
        public int WaitToGetBalanceInSeconds { get; set; }
        public int AttemptToGetBalanceEveryXSeconds { get; set; }
    }
}