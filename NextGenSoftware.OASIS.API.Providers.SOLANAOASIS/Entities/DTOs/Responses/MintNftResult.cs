using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses
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