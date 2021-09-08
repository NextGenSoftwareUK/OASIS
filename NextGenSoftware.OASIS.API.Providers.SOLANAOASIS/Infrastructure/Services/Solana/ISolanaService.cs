using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana
{
    public interface IBaseService
    {
        void InitializeService();
    }
    
    public interface ISolanaService
    {
        Task<Response<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest);
        Task<Response<ExchangeNftResult>> ExchangeNft(ExchangeNftRequest exchangeNftRequest);
        Task<Response<MintNftResult>> MintNft(MintNftRequest mintNftRequest);
        Task<Response<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest);
        Task<Response<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest);
        Task<Response<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest);
    }

    public class SolanaService : IBaseService, ISolanaService
    {
        private Wallet _wallet;
        public SolanaService()
        {
            InitializeService();
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
            var response = new Response<SendTransactionResult>();
            var rpcClient = ClientFactory.GetClient(Cluster.MainNet);
            var fromAccount = _wallet.GetAccount(sendTransactionRequest.FromAccountIndex);
            var toAccount = _wallet.GetAccount(sendTransactionRequest.ToAccountIndex);
            var blockHash = await rpcClient.GetRecentBlockHashAsync();

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText)).
                AddInstruction(SystemProgram.Transfer(fromAccount, toAccount.PublicKey, sendTransactionRequest.Lampposts)).
                Build(fromAccount);

            var sendTransactionResult = await rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.Code = (int)sendTransactionResult.HttpStatusCode;
                response.Message = sendTransactionResult.Reason;
            }
            response.Payload = new SendTransactionResult(sendTransactionResult.Result);
            return response;
        }

        public async Task<Response<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest)
        {
            throw new System.NotImplementedException();
        }

        public void InitializeService()
        {
            _wallet = new Wallet(new Mnemonic(WordList.English, WordCount.Twelve));
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
        public int FromAccountIndex { get; set; }
        public int ToAccountIndex { get; set; }
        public ulong Lampposts { get; set; }
        public string MemoText { get; set; }
    }

    public class SendTransactionResult
    {
        public string Transaction { get; set; }

        public SendTransactionResult(string transaction)
        {
            Transaction = transaction;
        }
        
        public SendTransactionResult() {}
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