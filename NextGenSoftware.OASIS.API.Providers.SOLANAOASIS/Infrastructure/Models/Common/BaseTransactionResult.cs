namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common
{
    public abstract class BaseTransactionResult
    {
        public string TransactionResult { get; set; }

        public BaseTransactionResult(string transactionResult)
        {
            TransactionResult = transactionResult;
        }

        public BaseTransactionResult()
        {
        }
    }
}