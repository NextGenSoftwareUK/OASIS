
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IGetWeb4TransactionsRequest : IGetWeb3TransactionsRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTransactionsReturned { get; set; }
        public int WaitToGetTransactionsInSeconds { get; set; }
        public int AttemptToGetTransactionsEveryXSeconds { get; set; }
    }
}