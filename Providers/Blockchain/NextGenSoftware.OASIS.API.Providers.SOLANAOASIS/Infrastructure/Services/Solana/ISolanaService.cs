using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public interface ISolanaService
{
    Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest);
    Task<OASISResult<MintNftResult>> MintNftAsync(MintWeb3NFTRequest mintNftRequest);
    Task<OASISResult<BurnNftResult>> BurnNftAsync(IBurnWeb3NFTRequest burnNftRequest);
    Task<OASISResult<decimal>> GetAccountBalanceAsync(IGetWeb3WalletBalanceRequest request);
    Task<OASISResult<SendTransactionResult>> SendNftAsync(SendWeb3NFTRequest mintNftRequest);
    Task<OASISResult<GetNftResult>> LoadNftAsync(string address);
    Task<OASISResult<SolanaAvatarDto>> GetAvatarByUsernameAsync(string username);
    Task<OASISResult<SolanaAvatarDto>> GetAvatarByIdAsync(Guid id);
    Task<OASISResult<SolanaAvatarDto>> GetAvatarByEmailAsync(string email);
    Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByIdAsync(Guid id);
    Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByUsernameAsync(string username);
    Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByEmailAsync(string email);

    /// <summary>Mint fungible SPL tokens to a recipient wallet ATA. The OASIS mint-authority account signs.</summary>
    Task<OASISResult<MintNftResult>> MintSplTokensAsync(string tokenMintAddress, string toWalletAddress, ulong amount, string cluster = "devnet");

    /// <summary>Burn fungible SPL tokens from a wallet ATA. The OASIS account must be the mint authority or token-account owner.</summary>
    Task<OASISResult<BurnNftResult>> BurnSplTokensAsync(string tokenMintAddress, string fromWalletAddress, ulong amount, string cluster = "devnet");

    /// <summary>Transfer fungible SPL tokens between two wallets, creating the recipient ATA if needed.</summary>
    Task<OASISResult<SendTransactionResult>> SendSplTokensAsync(string tokenMintAddress, string fromWalletAddress, string toWalletAddress, ulong amount, string cluster = "devnet");
}