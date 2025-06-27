namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;

public abstract class BaseTransactionResult
{
    public string TransactionHash { get; set; }

    public BaseTransactionResult(string transactionHash)
    {
        TransactionHash = transactionHash;
    }

    public BaseTransactionResult()
    {
    }
}