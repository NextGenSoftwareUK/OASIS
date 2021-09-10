using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public sealed class SendTransactionResult : BaseTransactionResult
    {
        public SendTransactionResult(string transactionResult) : base(transactionResult)
        {
        }
    }
}