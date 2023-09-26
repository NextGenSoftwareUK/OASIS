using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface INFTWalletTransactionRequest : IWalletTransactionRequest
    {
        public string MintWalletAddress { get; set; }
    }
}