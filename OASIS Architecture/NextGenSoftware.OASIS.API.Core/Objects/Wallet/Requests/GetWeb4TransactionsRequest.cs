
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class GetWeb4TransactionsRequest : GetWeb3TransactionsRequest, IGetWeb4TransactionsRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTransactionsReturned { get; set; }
        public int WaitToGetTransactionsInSeconds { get; set; }
        public int AttemptToGetTransactionsEveryXSeconds { get; set; }
    }
}