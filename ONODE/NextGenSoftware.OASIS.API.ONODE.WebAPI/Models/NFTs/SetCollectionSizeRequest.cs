namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class SetCollectionSizeRequest
    {
        public string CollectionMintAddress { get; set; }
        public ulong Size { get; set; }
        public string OnChainProvider { get; set; } = "SolanaOASIS";
    }
}
