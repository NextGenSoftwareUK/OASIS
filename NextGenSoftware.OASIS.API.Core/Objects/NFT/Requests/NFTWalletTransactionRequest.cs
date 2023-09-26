using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class NFTWalletTransactionRequest : WalletTransactionRequest, INFTWalletTransactionRequest
    {
        public string MintWalletAddress { get; set; }
    }
}