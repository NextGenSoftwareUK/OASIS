namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class ExchangeTokenResult : BaseTransactionResult
{
    public ExchangeTokenResult(string transactionHash) : base(transactionHash)
    {
    }

    public ExchangeTokenResult()
    {
    }
}