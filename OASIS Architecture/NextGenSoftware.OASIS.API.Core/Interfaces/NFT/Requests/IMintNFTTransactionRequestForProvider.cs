
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IMintNFTTransactionRequestForProvider : IMintNFTTransactionRequest
    {
        //public string JSONUrl { get; set; }
        public string Symbol { get; set; }
        //public string NFTJsonERC721 { get; set; }
        //public string NFTJsonERC1155 { get; set; }
    }
}