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
                //Now done in AvatarManager and HolonManager so no need for it here... :)
                //TODO: Need to move this new Blockchain version control system into HolonManager so is shared with other Providers...
                //entity.Id = new Guid();
                //entity.IsNewHolon = true;
                //entity.Version = 1;
                //entity.VersionId = Guid.NewGuid();

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

                    //TODO: Not much point setting the provider key because we cannot update the record, update below will create a NEW record. 
                    //So only in the Update method do we link to the previous version...

                    // We can of course always return the providerKey, which the caller can then store or do as they wish, it just will not be stored on the blockchain record.
                    result.Result.ProviderKey[ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    //OASISResult<T> updateResult = Update<T>(entity);

                    //if (updateResult != null && !updateResult.IsError && updateResult.Result != null)
                    //    result.IsSaved = true;
                    //else
                    //    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {updateResult.Message}");
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
                //Now done in AvatarManager and HolonManager so no need for it here... :)
                //TODO: Need to move this new Blockchain version control system into HolonManager so is shared with other Providers...
                //entity.Id = new Guid();
                //entity.IsNewHolon = true;
                //entity.Version = 1;
                //entity.VersionId = Guid.NewGuid();

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

                    //TODO: Not much point setting the provider key because we cannot update the record, update below will create a NEW record. 
                    //So only in the Update method do we link to the previous version...

                    // We can of course always return the providerKey, which the caller can then store or do as they wish, it just will not be stored on the blockchain record.
                    result.Result.ProviderKey[ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    //OASISResult<T> updateResult = Update<T>(entity);

                    //if (updateResult != null && !updateResult.IsError && updateResult.Result != null)
                    //    result.IsSaved = true;
                    //else
                    //    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {updateResult.Message}");
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
                //Now done in AvatarManager and HolonManager so no need for it here... :)
                /*
                // We need to store the previous version id, this will effectively be a linked list where each record will point to it's previous version.
                //entity.PreviousVersionId = entity.Id;
                //entity.Id = new Guid();
                entity.IsNewHolon = false;
                
                // Actually I think this way is better, we keep the same unique ID but just increment the version.
                // So later we can query for a record for a given id and version number.... :)
                // So don't think we need a previousVersionID at all? Maybe we could store just in case we need for later?
                entity.Version++;
                entity.PreviousVersionId = entity.VersionId;
                */

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

                    // TODO: Not much point setting the provider key because we cannot update the record, update below will create a NEW record.
                    //So only in the Update method do we link to the previous version...

                    // We can of course always return the providerKey, which the caller can then store or do as they wish, it just will not be stored on the blockchain record.
                    result.Result.ProviderKey[ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    
                    //TODO: Actually we can save the providerKey in the next version of the record. Just not sure if we need or want to do this?
                    // Probably not a good idea because of the overhead writing to a Blockchain such as gas fees, time, etc.
                    //result.Result.PreviousVersionProviderKey[ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    //OASISResult<T> updateResult = Update<T>(entity);

                    //if (updateResult != null && !updateResult.IsError && updateResult.Result != null)
                    //    result.IsSaved = true;
                    //else
                    //    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {updateResult.Message}");
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
                //Now done in AvatarManager and HolonManager so no need for it here... :)
                /*
                // We need to store the previous version id, this will effectively be a linked list where each record will point to it's previous version.
                //entity.PreviousVersionId = entity.Id;
                //entity.Id = new Guid();
                entity.IsNewHolon = false;
                
                // Actually I think this way is better, we keep the same unique ID but just increment the version.
                // So later we can query for a record for a given id and version number.... :)
                // So don't think we need a previousVersionID at all? Maybe we could store just in case we need for later?
                entity.Version++;
                entity.PreviousVersionId = entity.VersionId;
                */

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

                    // TODO: Not much point setting the provider key because we cannot update the record, update below will create a NEW record.
                    //So only in the Update method do we link to the previous version...

                    // We can of course always return the providerKey, which the caller can then store or do as they wish, it just will not be stored on the blockchain record.
                    result.Result.ProviderKey[ProviderType.SolanaOASIS] = sendTransactionResult.Result;

                    //TODO: Actually we can save the providerKey in the next version of the record. Just not sure if we need or want to do this?
                    // Probably not a good idea because of the overhead writing to a Blockchain such as gas fees, time, etc.
                    //result.Result.PreviousVersionProviderKey[ProviderType.SolanaOASIS] = sendTransactionResult.Result;
                    //OASISResult<T> updateResult = await UpdateAsync<T>(entity);

                    //if (updateResult != null && !updateResult.IsError && updateResult.Result != null)
                    //    result.IsSaved = true;
                    //else
                    //    ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {updateResult.Message}");
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