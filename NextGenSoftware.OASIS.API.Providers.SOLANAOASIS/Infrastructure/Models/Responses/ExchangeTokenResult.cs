namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public class ExchangeTokenResult
    {
        public string TransactionResult { get; set; }

        public ExchangeTokenResult(string result)
        {
            TransactionResult = result;
        }
        
        public ExchangeTokenResult () {}
    }
}