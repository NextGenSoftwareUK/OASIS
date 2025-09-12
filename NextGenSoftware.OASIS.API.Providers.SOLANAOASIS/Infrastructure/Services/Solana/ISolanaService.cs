namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public interface ISolanaService
{
    Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest);
    Task<OASISResult<MintNftResult>> MintNftAsync(MintNFTTransactionRequest mintNftRequest);
    Task<OASISResult<SendTransactionResult>> SendNftAsync(NFTWalletTransactionRequest mintNftRequest);
    Task<OASISResult<GetNftResult>> LoadNftAsync(string address);
}