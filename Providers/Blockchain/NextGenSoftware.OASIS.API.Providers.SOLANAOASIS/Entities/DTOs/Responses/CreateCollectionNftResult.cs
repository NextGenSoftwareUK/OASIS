namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class CreateCollectionNftResult : BaseTransactionResult
{
    public string CollectionMintAddress { get; set; }
    public string Network { get; set; }
    public string SetCollectionSizeTransactionHash { get; set; }

    public CreateCollectionNftResult() { }

    public CreateCollectionNftResult(
        string collectionMintAddress,
        string network,
        string mintTransactionHash,
        string setCollectionSizeTransactionHash) : base(mintTransactionHash)
    {
        CollectionMintAddress = collectionMintAddress;
        Network = network;
        SetCollectionSizeTransactionHash = setCollectionSizeTransactionHash;
    }
}
