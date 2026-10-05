namespace Domain.Entities;

public sealed class RwaToken : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string AssetDescription { get; set; } = string.Empty;
    public string ProofOfOwnershipDocument { get; set; } = string.Empty;
    public string UniqueIdentifier { get; set; } = string.Empty;
    public int Royalty { get; set; }
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public string OwnerContact { get; set; } = string.Empty;
    public NftAssetType AssetType { get; set; }
    public InsuranceStatus InsuranceStatus { get; set; }
    public GeoLocation? Geolocation { get; set; }
    public DateOnly ValuationDate { get; set; }
    public NftPropertyType PropertyType { get; set; }
    public double Area { get; set; }
    public int ConstructionYear { get; set; }
    public string MintAccount { get; set; } = string.Empty;
    public NetworkType MintAccountType { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;

    public Guid? VirtualAccountId { get; set; }
    public VirtualAccount? VirtualAccount { get; set; }

    public Guid? WalletLinkedAccountId { get; set; }
    public WalletLinkedAccount? WalletLinkedAccount { get; set; }

    public ICollection<RwaTokenPriceHistory> RwaTokenPriceHistories { get; set; } = [];
    public ICollection<RwaTokenOwnershipTransfer> RwaTokenOwnershipTransfers { get; set; } = [];
}