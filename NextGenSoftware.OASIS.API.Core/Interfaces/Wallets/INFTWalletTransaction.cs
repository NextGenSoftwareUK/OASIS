
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface INFTWalletTransaction : IWalletTransaction
    {
        public string MintWalletAddress { get; set; }
    }
}