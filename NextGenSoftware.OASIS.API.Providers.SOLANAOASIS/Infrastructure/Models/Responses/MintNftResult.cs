using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public sealed class MintNftResult : BaseTransactionResult
    {
        public MintNftResult(string transactionHash) : base(transactionHash)
        {
        }

        public MintNftResult()
        {
            
        }
    }
}