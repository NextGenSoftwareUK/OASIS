using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public class SendTransactionResult : BaseTransactionResult
    {
        public SendTransactionResult(string transactionResult) : base(transactionResult)
        {
        }
    }
}