using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana
{
    public interface ISolanaService
    {
        Task<Response<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest);
        Task<Response<ExchangeNftResult>> ExchangeNft(ExchangeNftRequest exchangeNftRequest);
        Task<Response<MintNftResult>> MintNft(MintNftRequest mintNftRequest);
        Task<Response<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest);
        Task<Response<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest);
        Task<Response<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest);
    }

    public class SolanaService : ISolanaService
    {
        public SolanaService()
        {
            
        }
        
        public async Task<Response<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<ExchangeNftResult>> ExchangeNft(ExchangeNftRequest exchangeNftRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<MintNftResult>> MintNft(MintNftRequest mintNftRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GetNftWalletRequest
    {
    }

    public class GetNftWalletResult
    {
    }

    public class GetNftMetadataRequest
    {
    }

    public class GetNftMetadataResult
    {
    }

    public class SendTransactionRequest
    {
    }

    public class SendTransactionResult
    {
    }

    public class MintNftRequest
    {
    }

    public class MintNftResult
    {
    }

    public class ExchangeNftRequest
    {
    }

    public class ExchangeNftResult
    {
    }

    public class ExchangeTokenRequest
    {
    }

    public class ExchangeTokenResult
    {
    }
}