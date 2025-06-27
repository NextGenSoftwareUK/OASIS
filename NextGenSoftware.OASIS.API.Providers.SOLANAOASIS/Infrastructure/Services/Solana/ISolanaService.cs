namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public interface ISolanaService
{
    Task<OASISResult<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest);
    Task<OASISResult<MintNftResult>> MintNftAsync(MintNFTTransactionRequestForProvider mintNftRequest);
    Task<OASISResult<SendTransactionResult>> SendNftAsync(NFTWalletTransactionRequest mintNftRequest);
    Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest);
    Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest);
    Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest);
}