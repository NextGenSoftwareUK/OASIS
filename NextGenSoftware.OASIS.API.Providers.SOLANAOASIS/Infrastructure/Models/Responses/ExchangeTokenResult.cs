using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public class ExchangeTokenResult : BaseTransactionResult
    {
        public ExchangeTokenResult(string transactionResult) : base(transactionResult)
        {
        }
    }
}