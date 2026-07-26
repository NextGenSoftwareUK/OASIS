namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class MintNftResult : BaseTransactionResult
{
    public string MintAccount { get; set; }
    public string Network { get; set; }
    public string VerifyCollectionTransactionHash { get; set; }

    public MintNftResult(string transactionHash) : base(transactionHash)
    {
    }

    public MintNftResult(string mintAccount, string network, string transactionHash, string verifyCollectionTransactionHash) : base(transactionHash)
    {
        MintAccount = mintAccount;
        Network = network;
        VerifyCollectionTransactionHash = verifyCollectionTransactionHash;
    }

    public MintNftResult()
    {
    }
}