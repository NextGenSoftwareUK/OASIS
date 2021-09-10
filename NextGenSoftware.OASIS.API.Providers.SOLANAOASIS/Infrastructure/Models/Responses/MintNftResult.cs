using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public sealed class MintNftResult : BaseTransactionResult
    {
        public MintNftResult(string transactionResult) : base(transactionResult)
        {
        }

        public MintNftResult()
        {
            
        }
    }
}