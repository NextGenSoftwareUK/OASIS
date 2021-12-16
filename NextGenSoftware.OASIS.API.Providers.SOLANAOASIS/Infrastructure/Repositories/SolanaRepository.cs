using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public class SolanaRepository : ISolanaRepository
    {
        private readonly IRpcClient _rpcClient;
        private readonly Wallet _wallet;

        public SolanaRepository(string mnemonicWords)
        {
            _wallet = new Wallet(mnemonicWords);
            _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }

        public OASISResult<T> Create<T>(T entity) where T : IHolonBase, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in Create method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                entity.Id = new Guid();
                entity.IsNewHolon = true;
                var account = _wallet.Account;
                var blockHash = _rpcClient.GetRecentBlockHash();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, JsonConvert.SerializeObject(entity))).
                    Build(account);

                var sendTransactionResult = _rpcClient.SendTransaction(tx);

                if (sendTransactionResult != null && sendTransactionResult.WasSuccessful)
                {
                    result.Result = entity;
                    result.Result.ProviderKey[Core.Enums.ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    OASISResult<T> updateResult = Update<T>(entity);

                    if (updateResult != null && !updateResult.IsError && updateResult.Result != null)
                        result.IsSaved = true;
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {updateResult.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} {GetTransactionResultError(sendTransactionResult)}");
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {ex}");
            }

            return result;
        }
        public async Task<OASISResult<T>> CreateAsync<T>(T entity) where T : IHolonBase, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in CreateAsync method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                entity.Id = new Guid();
                entity.IsNewHolon = true;
                var account = _wallet.Account;
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, JsonConvert.SerializeObject(entity))).
                    Build(account);

                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);

                if (sendTransactionResult != null && sendTransactionResult.WasSuccessful)
                {
                    result.Result = entity;
                    result.Result.ProviderKey[Core.Enums.ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    OASISResult<T> updateResult = Update<T>(entity);

                    if (updateResult != null && !updateResult.IsError && updateResult.Result != null)
                        result.IsSaved = true;
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {updateResult.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} {GetTransactionResultError(sendTransactionResult)}");
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T> Get<T>(string hash) where T : IHolonBase, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in Get method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                var transactionData = _rpcClient.GetTransaction(hash, Commitment.Confirmed);

                if (transactionData.Result == null)
                {
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: No record found for hash {hash} (transactionData.Result is null).");
                    return result;
                }

                if (transactionData.Result.Transaction.Message.Instructions.Length == 0)
                {
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: No record found for hash {hash}. (transactionData.Result.Transaction.Message.Instructions.Length is 0)");
                    return result;
                }

                var entityBytes = Encoders.Base58.DecodeData(transactionData.Result.Transaction.Message.Instructions[0].Data);
                var entity = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(entityBytes));

                if (entity != null)
                {
                    result.Result = entity;
                    result.IsLoaded = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: No record found for hash {hash}. (transactionData.Result.Transaction.Message.Instructions[0].Data is null)");

            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex.ToString()}");
            }

            return result;
        }

        public async Task<OASISResult<T>> GetAsync<T>(string hash) where T : IHolonBase, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in GetAsync method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                var transactionData = await _rpcClient.GetTransactionAsync(hash, Commitment.Confirmed);

                if (transactionData.Result == null)
                {
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: No record found for hash {hash} (transactionData.Result is null).");
                    return result;
                }

                if (transactionData.Result.Transaction.Message.Instructions.Length == 0)
                {
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: No record found for hash {hash}. (transactionData.Result.Transaction.Message.Instructions.Length is 0)");
                    return result;
                }

                var entityBytes = Encoders.Base58.DecodeData(transactionData.Result.Transaction.Message.Instructions[0].Data);
                var entity = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(entityBytes));

                if (entity != null)
                {
                    result.Result = entity;
                    result.IsLoaded = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: No record found for hash {hash}. (transactionData.Result.Transaction.Message.Instructions[0].Data is null)");

            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex.ToString()}");
            }

            return result;
        }

        public OASISResult<T> Update<T>(T entity) where T : IHolonBase, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in Update method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                //entity.PreviousVersionId = entity.Id;
                //entity.Id = new Guid();
                //entity.IsNewHolon = false;

                //Store the previous ProviderKey to link the records but use the same Id (Guid).

                //TODO: Needs more thought... ;-)
                entity.PreviousVersionProviderKey[ProviderType.SolanaOASIS] = entity.ProviderKey[ProviderType.SolanaOASIS];

                var account = _wallet.Account;
                var blockHash = _rpcClient.GetRecentBlockHash();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, JsonConvert.SerializeObject(entity))).
                    Build(account);

                var sendTransactionResult = _rpcClient.SendTransaction(tx);

                if (sendTransactionResult != null && sendTransactionResult.WasSuccessful)
                {
                    result.Result = entity;
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} {GetTransactionResultError(sendTransactionResult)}");

            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T>> UpdateAsync<T>(T entity) where T : IHolonBase, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = "Error occured in UpdateAsync method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                //entity.PreviousVersionId = entity.Id;
                //entity.Id = new Guid();
                //entity.IsNewHolon = false;

                //Store the previous ProviderKey to link the records but use the same Id (Guid).

                //TODO: Needs more thought... ;-)
                entity.PreviousVersionProviderKey[ProviderType.SolanaOASIS] = entity.ProviderKey[ProviderType.SolanaOASIS];
                
                var account = _wallet.Account;
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, JsonConvert.SerializeObject(entity))).
                    Build(account);

                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);

                if (sendTransactionResult != null && sendTransactionResult.WasSuccessful)
                {
                    result.Result = entity;
                    result.IsSaved = true;
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} {GetTransactionResultError(sendTransactionResult)}");

            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {ex}");
            }

            return result;
        }

        public OASISResult<bool> Delete<T>(string hash) where T : IHolonBase, new()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in Delete method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                OASISResult<T> getResult = Get<T>(hash);

                if (getResult != null && !getResult.IsError && getResult.Result != null)
                {
                    getResult.Result.IsActive = false;
                    getResult.Result.DeletedDate = DateTime.UtcNow;

                    if (AvatarManager.LoggedInAvatar != null)
                        getResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.DeletedByAvatarId;

                    OASISResult<T> updateResut = Update(getResult.Result);

                    if (updateResut != null && !updateResut.IsError && updateResut.Result != null)
                    {
                        result.Result = true;
                        result.Message = "Avatar Successfully Deleted.";
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {updateResut.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {getResult.Message}");
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAsync<T>(string hash) where T : IHolonBase, new()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in SolanaRepository in SolanaOASIS Provider.";

            try
            {
                OASISResult<T> getResult = await GetAsync<T>(hash);

                if (getResult != null && !getResult.IsError && getResult.Result != null)
                {
                    getResult.Result.IsActive = false;
                    getResult.Result.DeletedDate = DateTime.UtcNow;

                    if (AvatarManager.LoggedInAvatar != null)
                        getResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.DeletedByAvatarId;

                    OASISResult<T> updateResut = await UpdateAsync(getResult.Result);

                    if (updateResut != null && !updateResut.IsError && updateResut.Result != null)
                    {
                        result.Result = true;
                        result.Message = "Avatar Successfully Deleted.";
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {updateResut.Message}");
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {getResult.Message}");
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
            }

            return result;
        }

        private string GetTransactionResultError(RequestResult<string> result)
        {
            return $"Reason: {result.Reason}, ErrorData: {result.ErrorData}, ServerErrorCode: {result.ServerErrorCode}";
        }
    }
}