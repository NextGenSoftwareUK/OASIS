
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class NFTWalletTransaction : WalletTransaction, INFTWalletTransaction
    {
        public string MintWalletAddress { get; set; }
    }
}