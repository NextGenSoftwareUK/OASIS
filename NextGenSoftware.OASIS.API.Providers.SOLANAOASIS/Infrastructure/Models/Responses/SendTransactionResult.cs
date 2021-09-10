namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public class SendTransactionResult
    {
        public string Transaction { get; set; }

        public SendTransactionResult(string transaction)
        {
            Transaction = transaction;
        }
        
        public SendTransactionResult() {}
    }
}