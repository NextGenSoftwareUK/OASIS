using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    /// <summary>
    /// Resolves a Solana wallet's profile picture (PFP) URL, e.g. from the first NFT image via Helius DAS getAssetsByOwner.
    /// </summary>
    public interface IWalletPfpService
    {
        /// <summary>
        /// Gets a profile image URL for the wallet (e.g. first NFT image). Returns null if none found or on error.
        /// </summary>
        Task<string> GetPfpUrlAsync(string walletAddress, CancellationToken cancellationToken = default);
    }
}
