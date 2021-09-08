using System.Collections.Generic;
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
            var rpcClient = ClientFactory.GetClient(Cluster.MainNet);
            var blockHash = await rpcClient.GetRecentBlockHashAsync();
            var minBalanceForExemptionAcc =
                (await rpcClient.GetMinimumBalanceForRentExemptionAsync()).Result;

            var mintAccount = _wallet.GetAccount(21);
            var ownerAccount = _wallet.GetAccount(10);
            var initialAccount = _wallet.GetAccount(22);
            var newAccount = _wallet.GetAccount(23);

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(ownerAccount).
                AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    newAccount,
                    minBalanceForExemptionAcc,
                    SystemProgram.AccountDataSize,
                    TokenProgram.ProgramIdKey)).
                AddInstruction(TokenProgram.InitializeAccount(
                    newAccount.PublicKey,
                    mintAccount.PublicKey,
                    ownerAccount.PublicKey)).
                AddInstruction(TokenProgram.Transfer(
                    initialAccount.PublicKey,
                    newAccount.PublicKey,
                    25000,
                    ownerAccount)).
                AddInstruction(MemoProgram.NewMemo(initialAccount, "Hello from Sol.Net")).
                Build(new List<Account>{ ownerAccount, newAccount });
        }

        public async Task<Response<ExchangeNftResult>> ExchangeNft(ExchangeNftRequest exchangeNftRequest)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Response<MintNftResult>> MintNft(MintNftRequest mintNftRequest)
        {
            var wallet = new Wallet.Wallet(MnemonicWords);

            var blockHash = rpcClient.GetRecentBlockHash();
            var minBalanceForExemptionAcc = rpcClient.GetMinimumBalanceForRentExemption(SystemProgram.AccountDataSize).Result;

            var minBalanceForExemptionMint =rpcClient.GetMinimumBalanceForRentExemption(TokenProgram.MintAccountDataSize).Result;

            var mintAccount = wallet.GetAccount(21);
            var ownerAccount = wallet.GetAccount(10);
            var initialAccount = wallet.GetAccount(22);

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(ownerAccount).
                AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    mintAccount,
                    minBalanceForExemptionMint,
                    TokenProgram.MintAccountDataSize,
                    TokenProgram.ProgramIdKey)).
                AddInstruction(TokenProgram.InitializeMint(
                    mintAccount.PublicKey,
                    2,
                    ownerAccount.PublicKey,
                    ownerAccount.PublicKey)).
                AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    initialAccount,
                    minBalanceForExemptionAcc,
                    SystemProgram.AccountDataSize,
                    TokenProgram.ProgramIdKey)).
                AddInstruction(TokenProgram.InitializeAccount(
                    initialAccount.PublicKey,
                    mintAccount.PublicKey,
                    ownerAccount.PublicKey)).
                AddInstruction(TokenProgram.MintTo(
                    mintAccount.PublicKey,
                    initialAccount.PublicKey,
                    25000,
                    ownerAccount)).
                AddInstruction(MemoProgram.NewMemo(initialAccount, "Hello from Sol.Net")).
                Build(new List<Account>{ ownerAccount, mintAccount, initialAccount });
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
            var tokens = TokenInfoResolver.Load();
            var client = ClientFactory.GetClient(Cluster.MainNet);

// load snapshot of wallet and sub-accounts
            TokenWallet tokenWallet = TokenWallet.Load(client, tokens, ownerAccount);
            var balances = tokenWallet.Balances();

// show individual token accounts
            var maxsym = balances.Max(x => x.Symbol.Length);
            var maxname = balances.Max(x => x.TokenName.Length);
            Console.WriteLine("Individual Accounts...");
            foreach (var account in tokenWallet.TokenAccounts())
            {
                Console.WriteLine($"{account.Symbol.PadRight(maxsym)} {account.BalanceDecimal,14} {account.TokenName.PadRight(maxname)} {account.PublicKey} {(account.IsAssociatedTokenAccount ? "[ATA]" : "")}");
            }
            Console.WriteLine();
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