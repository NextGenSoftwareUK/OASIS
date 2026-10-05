using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ISendWeb3NFTRequest
    {
        public string FromNFTTokenAddress { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public string TokenAddress { get; set; }
        public string TokenId { get; set; }
        //public string FromToken { get; set; }
        //public string ToToken { get; set; }
        public decimal Amount { get; set; }
        public string MemoText { get; set; }
    }
}