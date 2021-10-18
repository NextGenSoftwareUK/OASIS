using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories
{
    public class SolanaRepository<T> : ISolanaRepository<T> where T : IHolonBase, new()
    {
        private readonly IRpcClient _rpcClient;
        private readonly Wallet _wallet;

        public SolanaRepository()
        {
            _wallet = new Wallet(new Mnemonic(WordList.English, WordCount.Twelve));
            _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }

        public async Task<string> CreateAsync(T entity)
        {
            try
            {
                entity.Id = new Guid();
                var entityData = JsonConvert.SerializeObject(entity);
                var account = _wallet.Account;
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, entityData)).
                    Build(account);

                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
                return !sendTransactionResult.WasSuccessful ? string.Empty : sendTransactionResult.Result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> UpdateAsync(T entity)
        {
            try
            {
                entity.PreviousVersionId = entity.Id;
                entity.Id = new Guid();
                var entityData = JsonConvert.SerializeObject(entity);
                var account = _wallet.Account;
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, entityData)).
                    Build(account);

                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
                return !sendTransactionResult.WasSuccessful ? string.Empty : sendTransactionResult.Result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> DeleteAsync(string hash)
        {
            try
            {
                string transactionData = "";
                var entity = JsonConvert.DeserializeObject<T>(transactionData);
                if (entity == null)
                    return string.Empty;
                entity.IsActive = false;
                entity.PreviousVersionId = entity.Id;
                entity.Id = new Guid();
                var entityData = JsonConvert.SerializeObject(entity);
                var account = _wallet.Account;
                var blockHash = await _rpcClient.GetRecentBlockHashAsync();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, entityData)).
                    Build(account);

                var sendTransactionResult = await _rpcClient.SendTransactionAsync(tx);
                return !sendTransactionResult.WasSuccessful ? string.Empty : sendTransactionResult.Result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<T> GetAsync(string hash)
        {
            try
            {
                string transactionData = "";
                var entity = JsonConvert.DeserializeObject<T>(transactionData);
                return entity ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        public string Create(T entity)
        {
            try
            {
                entity.Id = new Guid();
                var entityData = JsonConvert.SerializeObject(entity);
                var account = _wallet.Account;
                var blockHash = _rpcClient.GetRecentBlockHash();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, entityData)).
                    Build(account);

                var sendTransactionResult = _rpcClient.SendTransaction(tx);
                return !sendTransactionResult.WasSuccessful ? string.Empty : sendTransactionResult.Result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public string Update(T entity)
        {
            try
            {
                entity.PreviousVersionId = entity.Id;
                entity.Id = new Guid();
                var entityData = JsonConvert.SerializeObject(entity);
                var account = _wallet.Account;
                var blockHash = _rpcClient.GetRecentBlockHash();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, entityData)).
                    Build(account);

                var sendTransactionResult = _rpcClient.SendTransaction(tx);
                return !sendTransactionResult.WasSuccessful ? string.Empty : sendTransactionResult.Result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public string Delete(string hash)
        {
            try
            {
                string transactionData = "";
                var entity = JsonConvert.DeserializeObject<T>(transactionData);
                if (entity == null)
                    return string.Empty;
                entity.IsActive = false;
                entity.PreviousVersionId = entity.Id;
                entity.Id = new Guid();
                var entityData = JsonConvert.SerializeObject(entity);
                var account = _wallet.Account;
                var blockHash = _rpcClient.GetRecentBlockHash();

                var tx = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(account).
                    AddInstruction(MemoProgram.NewMemo(account, entityData)).
                    Build(account);

                var sendTransactionResult = _rpcClient.SendTransaction(tx);
                return !sendTransactionResult.WasSuccessful ? string.Empty : sendTransactionResult.Result;
            }
            catch
            {
                return string.Empty;
            }
        }

        public T Get(string hash)
        {
            try
            {
                string transactionData = "";
                var entity = JsonConvert.DeserializeObject<T>(transactionData);
                return entity ?? new T();
            }
            catch
            {
                return new T();
            }
        }
    }
}