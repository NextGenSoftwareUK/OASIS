namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public class SolanaRepository(Account oasisAccount, IRpcClient rpcClient) : ISolanaRepository
{
    public async Task<string> CreateAsync<T>(T entity)
        where T : SolanaBaseDto, new()
    {
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        if (blockHash.Result != null)
        {
            var serializedEntity = JsonConvert.SerializeObject(entity);

            var entityTransactionBytes = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount)
                .AddInstruction(MemoProgram.NewMemo(oasisAccount, serializedEntity))
                .Build(oasisAccount);

            var transactionResult = await rpcClient.SendTransactionAsync(entityTransactionBytes);
            if (!transactionResult.WasSuccessful || transactionResult.HttpStatusCode != HttpStatusCode.OK)
                throw new Exception(
                    $"{transactionResult.RawRpcResponse} Reason: Transaction processing failed, entity Id: {entity.Id}");

            // Wait for transaction creating is done on provider side...
            await Task.Delay(3000);
            return transactionResult.Result;
        }
        else
            return "";
    }

    public async Task<string> UpdateAsync<T>(T entity)
        where T : SolanaBaseDto, new()
    {
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        entity.Version++;
        entity.PreviousVersionId = entity.Id;
        var serializedEntity = JsonConvert.SerializeObject(entity);

        var entityUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(MemoProgram.NewMemo(oasisAccount, serializedEntity))
            .Build(oasisAccount);

        var transactionResult = await rpcClient.SendTransactionAsync(entityUpdateTransactionBytes);
        if (!transactionResult.WasSuccessful || transactionResult.HttpStatusCode != HttpStatusCode.OK)
            throw new Exception(
                $"{transactionResult.RawRpcResponse} Reason: Transaction processing failed, updating entity Id: {entity.Id}");

        // Wait for transaction creating is done on provider side...
        await Task.Delay(3000);
        return transactionResult.Result;
    }

    public async Task<bool> DeleteAsync(string transactionHashReference)
    {
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        var entityTransactionQueryResult =
            await rpcClient.GetTransactionAsync(transactionHashReference, Commitment.Confirmed);
        if (!entityTransactionQueryResult.WasSuccessful ||
            entityTransactionQueryResult.HttpStatusCode != HttpStatusCode.OK)
            throw new Exception(entityTransactionQueryResult.RawRpcResponse);

        if (entityTransactionQueryResult.Result == null)
            throw new Exception(
                $"{entityTransactionQueryResult.RawRpcResponse} Reason: No record found for hash {transactionHashReference} (transactionData.Result is null).");

        if (entityTransactionQueryResult.Result.Transaction.Message.Instructions.Length == 0)
            throw new Exception(
                $"{entityTransactionQueryResult.RawRpcResponse} Reason: No record found for hash {transactionHashReference}. (transactionData.Result.Transaction.Message.Instructions.Length is 0)");

        var transactionDataBuffer =
            Encoders.Base58.DecodeData(entityTransactionQueryResult.Result.Transaction.Message.Instructions[0]
                .Data);
        var transactionContent = Encoding.UTF8.GetString(transactionDataBuffer);
        var deserializedEntity = JsonConvert.DeserializeObject<SolanaAvatarDto>(transactionContent)
                                 ?? throw new Exception(
                                     $"{entityTransactionQueryResult.RawRpcResponse} Reason: No content found for hash {transactionHashReference}.");

        deserializedEntity.IsDeleted = true;
        deserializedEntity.Version++;
        deserializedEntity.PreviousVersionId = deserializedEntity.Id;
        var serializedEntity = JsonConvert.SerializeObject(deserializedEntity);

        var entityDeleteUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(MemoProgram.NewMemo(oasisAccount, serializedEntity))
            .Build(oasisAccount);

        var transactionResult = await rpcClient.SendTransactionAsync(entityDeleteUpdateTransactionBytes);
        if (!transactionResult.WasSuccessful || transactionResult.HttpStatusCode != HttpStatusCode.OK)
            throw new Exception(
                $"{transactionResult.RawRpcResponse} Reason: transaction processing failed, entity Id: {deserializedEntity.Id}");

        // Wait for transaction creating is done on provider side...
        await Task.Delay(3000);
        return true;
    }

    public async Task<T> GetAsync<T>(string transactionHashReference)
        where T : SolanaBaseDto, new()
    {
        var entityTransactionQueryResult =
            await rpcClient.GetTransactionAsync(transactionHashReference, Commitment.Confirmed);
        if (!entityTransactionQueryResult.WasSuccessful ||
            entityTransactionQueryResult.HttpStatusCode != HttpStatusCode.OK)
            throw new Exception(entityTransactionQueryResult.RawRpcResponse);

        if (entityTransactionQueryResult.Result == null)
            throw new Exception(
                $"{entityTransactionQueryResult.RawRpcResponse} Reason: No record found for hash {transactionHashReference} (transactionData.Result is null).");

        if (entityTransactionQueryResult.Result.Transaction.Message.Instructions.Length == 0)
            throw new Exception(
                $"{entityTransactionQueryResult.RawRpcResponse} Reason: No record found for hash {transactionHashReference}. (transactionData.Result.Transaction.Message.Instructions.Length is 0)");

        var transactionDataBuffer =
            Encoders.Base58.DecodeData(entityTransactionQueryResult.Result.Transaction.Message.Instructions[0]
                .Data);
        var transactionContent = Encoding.UTF8.GetString(transactionDataBuffer);
        var deserializedEntity = JsonConvert.DeserializeObject<T>(transactionContent)
                                 ?? throw new Exception(
                                     $"{entityTransactionQueryResult.RawRpcResponse} Reason: No content found for hash {transactionHashReference}.");

        return deserializedEntity;
    }
}