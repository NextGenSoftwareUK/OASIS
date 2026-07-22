namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class CreateCollectionNFTRequest
    {
        public string Title { get; set; }
        public string Symbol { get; set; }
        public string MetadataUri { get; set; }
        public ulong InitialSize { get; set; } = 0;
        public string OnChainProvider { get; set; } = "SolanaOASIS";
    }
}
