using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Base;
using Org.BouncyCastle.Ocsp;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana
{
    public class SolanaService : IBaseService, ISolanaService
    {
        private Wallet _wallet;
        private IRpcClient _rpcClient;

        public SolanaService()
        {
            InitializeService();
        }

        public async Task<OASISResult<ExchangeTokenResult>> ExchangeTokens(ExchangeTokenRequest exchangeTokenRequest)
        {
            var response = new OASISResult<ExchangeTokenResult>();
            var blockHash = await _rpcClient.GetRecentBlockHashAsync();
            var mintAccount = new PublicKey(exchangeTokenRequest.MintAccountAddress);
            var fromAccount = new PublicKey(exchangeTokenRequest.FromAccount);
            var toAccount = new PublicKey(exchangeTokenRequest.ToAccount);

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(TokenProgram.InitializeAccount(
                    toAccount,
                    mintAccount,
                    fromAccount)).
                AddInstruction(TokenProgram.Transfer(
                    fromAccount,
                    toAccount,
                    exchangeTokenRequest.Amount,
                    fromAccount)).
                AddInstruction(MemoProgram.NewMemo(fromAccount, exchangeTokenRequest.MemoText)).
                Build(new List<Account>{ fromAccount });
            
            var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
            }
            response.Result = new ExchangeTokenResult(sendTransactionResult.Result);
            return response;
        }
        
        public async Task<OASISResult<MintNftResult>> MintNft(MintNftRequest mintNftRequest)
        {
            var response = new OASISResult<MintNftResult>();
            
            var blockHash = await _rpcClient.GetRecentBlockHashAsync();
            var minBalanceForExemptionAcc = (await _rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            var minBalanceForExemptionMint =(await _rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;

            var mintAccount = new PublicKey("GsguXojeGATpZGW8VNfW8qQCBVodbW2qGS8bUEbdGZfv");
            var ownerAccount = new PublicKey("GsguXojeGATpZGW8VNfW8qQCBVodbW2qGS8bUEbdGZfv");
            var initialAccount = new PublicKey("GsguXojeGATpZGW8VNfW8qQCBVodbW2qGS8bUEbdGZfv");

            var account = new Account("4uhp5e78YwJwVAQnH3tsxZZYMrsdEX9fMKb5iCEC9mhw6mVusuzsCoMm93KEmJpurHVqARXV119yjX1gH2nh9w1r", ownerAccount.Key);
            
            
            
            var recentHash = await _rpcClient.GetRecentBlockHashAsync();

            var txs = new TransactionBuilder().AddInstruction(MemoProgram.NewMemoV2("Hello, From Here")).SetFeePayer(_wallet.Account)
                .SetRecentBlockHash(recentHash.Result.Value.Blockhash).Build(_wallet.Account);

            var txHash = await _rpcClient.SendTransactionAsync(txs);
            
            
            
            
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
                    mintAccount,
                    mintNftRequest.MintDecimals,
                    ownerAccount,
                    ownerAccount)).
                AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    initialAccount,
                    minBalanceForExemptionAcc,
                    TokenProgram.TokenAccountDataSize,
                    TokenProgram.ProgramIdKey)).
                AddInstruction(TokenProgram.InitializeAccount(
                    initialAccount,
                    mintAccount,
                    ownerAccount)).
                AddInstruction(TokenProgram.MintTo(
                    mintAccount,
                    initialAccount,
                    mintNftRequest.Amount,
                    ownerAccount)).
                AddInstruction(MemoProgram.NewMemo(initialAccount, mintNftRequest.MemoText)).
                Build(account);
            
            var sendTransactionResult = await _rpcClient.SimulateTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
            }
            response.Result = new MintNftResult(sendTransactionResult.Result.Value.Error.InstructionError.BorshIoError);
            return response;
        }

        public async Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest)
        {
            var response = new OASISResult<SendTransactionResult>();
            var fromAccount = new PublicKey(sendTransactionRequest.FromAccount);
            var toAccount = new PublicKey(sendTransactionRequest.ToAccount);
            var blockHash = await _rpcClient.GetRecentBlockHashAsync();

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText)).
                AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, sendTransactionRequest.Lampposts)).
                Build(fromAccount);

            var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
            }
            response.Result = new SendTransactionResult(sendTransactionResult.Result);
            return response;
        }

        public async Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest)
        {
            var response = new OASISResult<GetNftMetadataResult>();
            try
            {
                var ownerAccount = new Account(getNftMetadataRequest.OwnerAccount.PrivateKey, 
                    getNftMetadataRequest.OwnerAccount.PublicKey);

                var tokens = new TokenMintResolver();
                tokens.Add(new TokenDef(getNftMetadataRequest.MintToken, getNftMetadataRequest.MintName, getNftMetadataRequest.MintSymbol, getNftMetadataRequest.MintDecimal));
                var tokenWallet = await TokenWallet.LoadAsync(_rpcClient, tokens, ownerAccount);
                var sublist = tokenWallet.TokenAccounts();
                if(!string.IsNullOrEmpty(getNftMetadataRequest.MintSymbol))
                    sublist = sublist.WithSymbol(getNftMetadataRequest.MintSymbol);
                if (!string.IsNullOrEmpty(getNftMetadataRequest.MintToken))
                    sublist = sublist.WithMint(getNftMetadataRequest.MintToken);
                response.Result = new GetNftMetadataResult()
                {
                    Count = sublist.Count(),
                    Accounts = sublist
                };
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
            }
            return response;
        }

        public async Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest)
        {
            var response = new OASISResult<GetNftWalletResult>();
            try
            {
                var ownerAccount = new PublicKey(getNftWalletRequest.OwnerAccount);

                var tokens = new TokenMintResolver();
                tokens.Add(new TokenDef(getNftWalletRequest.MintToken, getNftWalletRequest.MintName, getNftWalletRequest.MintSymbol, getNftWalletRequest.MintDecimal));

                var tokenWallet = await TokenWallet.LoadAsync(_rpcClient, tokens, ownerAccount);
                var balances = tokenWallet.Balances();
                var sublist = tokenWallet.TokenAccounts().WithSymbol(getNftWalletRequest.MintSymbol).WithMint(getNftWalletRequest.MintToken);
            
                response.Result = new GetNftWalletResult()
                {
                    Accounts = sublist,
                    Balances = balances
                };
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
            }
            return response;
        }

        public void InitializeService()
        {
            _wallet = new Wallet("pattern vessel trade prosper cube okay dust pet primary during captain endless");
            _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }
    }
}