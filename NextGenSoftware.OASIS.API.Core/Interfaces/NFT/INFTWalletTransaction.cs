namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface INFTWalletTransaction : IWalletTransaction
    {
        public string MintWalletAddress { get; set; }
    }
}