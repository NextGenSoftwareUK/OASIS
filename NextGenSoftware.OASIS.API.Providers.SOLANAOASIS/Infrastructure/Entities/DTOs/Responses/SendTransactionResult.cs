using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Responses
{
    public sealed class SendTransactionResult : BaseTransactionResult
    {
        public SendTransactionResult(string transactionHash) : base(transactionHash)
        {
        }

        public SendTransactionResult()
        {
            
        }
    }
}