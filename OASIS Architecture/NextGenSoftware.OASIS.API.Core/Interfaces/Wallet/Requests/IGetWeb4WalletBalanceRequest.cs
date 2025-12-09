
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IGetWeb4WalletBalanceRequest : IGetWeb3WalletBalanceRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillBalanceReturned { get; set; }
        public int WaitToGetBalanceInSeconds { get; set; }
        public int AttemptToGetBalanceEveryXSeconds { get; set; }
    }
}