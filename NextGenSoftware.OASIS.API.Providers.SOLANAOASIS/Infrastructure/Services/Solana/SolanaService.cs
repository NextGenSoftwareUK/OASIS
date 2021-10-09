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
using Solnet.Metaplex;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

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
            var mintAccount = _wallet.GetAccount(exchangeTokenRequest.MintAccount.GetAccountIndex());
            var fromAccount = _wallet.GetAccount(exchangeTokenRequest.FromAccount.GetAccountIndex());
            var toAccount = _wallet.GetAccount(exchangeTokenRequest.ToAccount.GetAccountIndex());

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
                return response;
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

            var mintAccount = _wallet.GetAccount(mintNftRequest.MintAccount.GetAccountIndex());
            var ownerAccount = _wallet.GetAccount(mintNftRequest.FromAccount.GetAccountIndex());
            var initialAccount = _wallet.GetAccount(mintNftRequest.ToAccount.GetAccountIndex());
            
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
                Build(new List<Account>() { mintAccount, ownerAccount, initialAccount });
            
            var sendTransactionResult = await _rpcClient.SimulateTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
                return response;
            }
            response.Result = new MintNftResult(sendTransactionResult.Result.Value.Error.InstructionError.BorshIoError);
            return response;
        }

        public async Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest)
        {
            var response = new OASISResult<SendTransactionResult>();
            var fromAccount = _wallet.GetAccount(sendTransactionRequest.FromAccount.GetAccountIndex());
            var toAccount = _wallet.GetAccount(sendTransactionRequest.ToAccount.GetAccountIndex());
            var blockHash = await _rpcClient.GetRecentBlockHashAsync();

            var tx = new TransactionBuilder().
                SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                SetFeePayer(fromAccount).
                AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText)).
                AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, sendTransactionRequest.Lampposts)).
                Build(new List<Account>() { fromAccount, toAccount });

            var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
            if (!sendTransactionResult.WasSuccessful)
            {
                response.IsError = true;
                response.Message = sendTransactionResult.Reason;
                return response;
            }
            response.Result = new SendTransactionResult(sendTransactionResult.Result);
            return response;
        }

        public async Task<OASISResult<MetadataAccount>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest)
        {
            var response = new OASISResult<MetadataAccount>();
            try
            {
                response.Result = await MetadataAccount
                    .GetAccount(_rpcClient, new PublicKey(getNftMetadataRequest.AccountAddress));
            }
            catch (NullReferenceException)
            {
                response.IsError = true;
                response.Message = "Account address is not correct or metadata not exists";
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
                var ownerAccount = _wallet.GetAccount(getNftWalletRequest.OwnerAccount.GetAccountIndex());
                
                var tokens = new TokenMintResolver();
                tokens.Add(new TokenDef(getNftWalletRequest.MintToken, getNftWalletRequest.MintName, getNftWalletRequest.MintSymbol, getNftWalletRequest.MintDecimal));

                var tokenWallet = await TokenWallet.LoadAsync(_rpcClient, tokens, ownerAccount);
                var balances = tokenWallet.Balances();
                var sublist = tokenWallet.TokenAccounts().WithSymbol(getNftWalletRequest.MintSymbol).WithMint(getNftWalletRequest.MintToken);
                if(!string.IsNullOrEmpty(getNftWalletRequest.MintSymbol))
                    sublist = sublist.WithSymbol(getNftWalletRequest.MintSymbol);
                if (!string.IsNullOrEmpty(getNftWalletRequest.MintToken))
                    sublist = sublist.WithMint(getNftWalletRequest.MintToken);
                
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
            _wallet = new Wallet(new Mnemonic(WordList.English, WordCount.Twelve));
            _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }
    }
}