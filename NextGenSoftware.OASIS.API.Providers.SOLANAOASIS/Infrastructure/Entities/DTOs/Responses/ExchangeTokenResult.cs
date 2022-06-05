using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Responses
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