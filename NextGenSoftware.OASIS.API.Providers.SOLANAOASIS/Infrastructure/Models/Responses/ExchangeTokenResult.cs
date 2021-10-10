using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public sealed class ExchangeTokenResult : BaseTransactionResult
    {
        public ExchangeTokenResult(string transactionHash) : base(transactionHash)
        {
        }

        public ExchangeTokenResult()
        {
        }
    }
}