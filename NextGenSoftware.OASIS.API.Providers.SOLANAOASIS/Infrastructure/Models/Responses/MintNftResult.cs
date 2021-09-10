namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public class MintNftResult
    {
        public string TransactionResult { get; set; }

        public MintNftResult(string result)
        {
            TransactionResult = result;
        }
        
        public MintNftResult() {}
    }
}