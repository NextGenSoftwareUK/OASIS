using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class CreateCollectionNFTRequest : ICreateCollectionNFTRequest
    {
        public string Title { get; set; }
        public string Symbol { get; set; }
        public string MetadataUri { get; set; }
        public ulong InitialSize { get; set; } = 0;
    }
}
