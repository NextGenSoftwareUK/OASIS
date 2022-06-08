using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Responses;
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
    public class SolanaService : ISolanaService
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
            try
            {
                var (err, res) = exchangeTokenRequest.IsRequestValid();
                if (!err)
                {
                    response.Message = res;
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, res);
                    return response;
                }
                
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var mintAccount = new PublicKey(exchangeTokenRequest.MintAccount.PublicKey);
                var fromAccount = new PublicKey(exchangeTokenRequest.FromAccount.PublicKey);
                var toAccount = new PublicKey(exchangeTokenRequest.ToAccount.PublicKey);
            
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
                    Build(_wallet.Account);
            
                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
                if (!sendTransactionResult.WasSuccessful)
                {
                    response.IsError = true;
                    response.Message = sendTransactionResult.Reason;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                response.Result = new ExchangeTokenResult(sendTransactionResult.Result);
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }
        
        public async Task<OASISResult<MintNftResult>> MintNft(MintNftRequest mintNftRequest)
        {
            var response = new OASISResult<MintNftResult>();
            try
            {
                var (err, res) = mintNftRequest.IsRequestValid();
                if (!err)
                {
                    response.IsError = true;
                    response.Message = res;
                    ErrorHandling.HandleError(ref response, res);
                    return response;
                }
                
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();
                var minBalanceForExemptionAcc = (await _rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
                var minBalanceForExemptionMint =(await _rpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;

                var mintAccount = _wallet.Account;
                var ownerAccount = new PublicKey(mintNftRequest.FromAccount.PublicKey);
                var initialAccount = new PublicKey(mintNftRequest.ToAccount.PublicKey);
                
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
                    Build(_wallet.Account);
                
                var sendTransactionResult = await _rpcClient.SimulateTransactionAsync(tx);
                if (!sendTransactionResult.WasSuccessful)
                {
                    response.IsError = true;
                    response.Message = sendTransactionResult.Reason;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                response.Result = new MintNftResult(sendTransactionResult.Result.Value.Error.InstructionError.BorshIoError);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest)
        {
            var response = new OASISResult<SendTransactionResult>();
            try
            {
                var (err, res) = sendTransactionRequest.IsRequestValid();
                if (!err)
                {
                    response.Message = res;
                    response.IsError = true;
                    ErrorHandling.HandleError(ref response, res);
                    return response;
                }

                var fromAccount = new PublicKey(sendTransactionRequest.FromAccount.PublicKey);
                var toAccount = new PublicKey(sendTransactionRequest.ToAccount.PublicKey);
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(fromAccount).
                    AddInstruction(MemoProgram.NewMemo(fromAccount, sendTransactionRequest.MemoText)).
                    AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, sendTransactionRequest.Lampposts)).
                    Build(_wallet.Account);

                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
                if (!sendTransactionResult.WasSuccessful)
                {
                    response.IsError = true;
                    response.Message = sendTransactionResult.Reason;
                    ErrorHandling.HandleError(ref response, response.Message);
                    return response;
                }
                response.Result = new SendTransactionResult(sendTransactionResult.Result);
            }
            catch (Exception e)
            {
                response.Exception = e;
                response.Message = e.Message;
                response.IsError = true;
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<GetNftMetadataResult>> GetNftMetadata(GetNftMetadataRequest getNftMetadataRequest)
        {
            var response = new OASISResult<GetNftMetadataResult>();
            try
            {
                var account = await MetadataAccount
                    .GetAccount(_rpcClient, new PublicKey(getNftMetadataRequest.AccountAddress));
                response.Result = new GetNftMetadataResult(account);
            }
            catch (ArgumentNullException)
            {
                response.IsError = true;
                response.Message = "Account address is not correct or metadata not exists";
                ErrorHandling.HandleError(ref response, response.Message);
            }
            catch (NullReferenceException)
            {
                response.IsError = true;
                response.Message = "Account address is not correct or metadata not exists";
                ErrorHandling.HandleError(ref response, response.Message);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.Message = e.Message;
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        public async Task<OASISResult<GetNftWalletResult>> GetNftWallet(GetNftWalletRequest getNftWalletRequest)
        {
            var response = new OASISResult<GetNftWalletResult>();
            try
            {
                var ownerAccount = new PublicKey(getNftWalletRequest.OwnerAccount.PublicKey);
                
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
                ErrorHandling.HandleError(ref response, e.Message);
            }
            return response;
        }

        private void InitializeService()
        {
            _wallet = new Wallet(new Mnemonic(WordList.English, WordCount.Twelve));
            _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }
    }
}