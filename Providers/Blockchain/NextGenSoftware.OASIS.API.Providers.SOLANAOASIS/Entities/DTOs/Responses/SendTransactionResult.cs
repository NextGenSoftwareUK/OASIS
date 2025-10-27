namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class SendTransactionResult : BaseTransactionResult
{
    public SendTransactionResult(string transactionHash) : base(transactionHash)
    {
    }

    public SendTransactionResult()
    {
    }
}