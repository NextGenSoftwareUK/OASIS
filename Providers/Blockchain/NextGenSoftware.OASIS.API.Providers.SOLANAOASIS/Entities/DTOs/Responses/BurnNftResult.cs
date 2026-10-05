namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class BurnNftResult : BaseTransactionResult
{
    public string MintAccount { get; set; }
    public string Network { get; set; }

    public BurnNftResult(string transactionHash) : base(transactionHash)
    {
    }

    public BurnNftResult(string mintAccount, string network, string transactionHash) : base(transactionHash)
    {
        MintAccount = mintAccount;
        Network = network;
    }

    public BurnNftResult()
    {
    }
}