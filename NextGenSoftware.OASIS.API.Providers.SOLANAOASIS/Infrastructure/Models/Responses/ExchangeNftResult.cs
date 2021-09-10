using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public sealed class ExchangeNftResult : BaseTransactionResult
    {
        public ExchangeNftResult(string transactionResult) : base(transactionResult)
        {
        }

        public ExchangeNftResult()
        {
        }
    }
}