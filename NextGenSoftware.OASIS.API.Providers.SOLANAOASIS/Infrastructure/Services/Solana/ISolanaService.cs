using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana
{
    public interface ISolanaService
    {
        Task<Response<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest);
        Task<Response<MintNftResult>> MintNft(MintNftRequest mintNftRequest);
        Task<Response<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest);
        Task<Response<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest);
        Task<Response<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest);
    }
}