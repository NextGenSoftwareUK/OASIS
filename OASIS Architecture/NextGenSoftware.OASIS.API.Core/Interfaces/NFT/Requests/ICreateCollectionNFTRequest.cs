namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ICreateCollectionNFTRequest
    {
        string Title { get; set; }
        string Symbol { get; set; }
        string MetadataUri { get; set; }
        ulong InitialSize { get; set; }
    }
}
