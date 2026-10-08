namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public class SolanaRepository(Account oasisAccount, IRpcClient rpcClient) : ISolanaRepository
{
    public async Task<string> CreateAsync<T>(T entity)
        where T : SolanaBaseDto, new()
    {
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        if (blockHash.Result != null)
        {
            var serializedEntity = EncodeEntity(entity);

            var entityTransactionBytes = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount)
                .AddInstruction(SetComputeUnitLimit(1_400_000))
                .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
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
        var serializedEntity = EncodeEntity(entity);

        var entityUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
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
            Encoders.Base58.DecodeData(entityTransactionQueryResult.Result.Transaction.Message.Instructions[^1]
                .Data);
        var transactionContent = DecodeEntity(transactionDataBuffer);
        var deserializedEntity = JsonConvert.DeserializeObject<SolanaAvatarDto>(transactionContent)
                                 ?? throw new Exception(
                                     $"{entityTransactionQueryResult.RawRpcResponse} Reason: No content found for hash {transactionHashReference}.");

        deserializedEntity.IsDeleted = true;
        deserializedEntity.Version++;
        deserializedEntity.PreviousVersionId = deserializedEntity.Id;
        var serializedEntity = EncodeEntity(deserializedEntity);

        var entityDeleteUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
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
            Encoders.Base58.DecodeData(entityTransactionQueryResult.Result.Transaction.Message.Instructions[^1]
                .Data);
        var transactionContent = DecodeEntity(transactionDataBuffer);
        var deserializedEntity = JsonConvert.DeserializeObject<T>(transactionContent)
                                 ?? throw new Exception(
                                     $"{entityTransactionQueryResult.RawRpcResponse} Reason: No content found for hash {transactionHashReference}.");

        return deserializedEntity;
    }

    private static string EncodeEntity<T>(T entity)
    {
        byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity));
        using var output = new System.IO.MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(
                   output, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
            gzip.Write(json, 0, json.Length);
        return $"oasis:gzip:{Convert.ToBase64String(output.ToArray())}";
    }

    private static string DecodeEntity(byte[] transactionData)
    {
        string content = Encoding.UTF8.GetString(transactionData);
        const string prefix = "oasis:gzip:";
        if (!content.StartsWith(prefix, StringComparison.Ordinal))
            return content;

        byte[] compressed = Convert.FromBase64String(content[prefix.Length..]);
        using var input = new System.IO.MemoryStream(compressed);
        using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new System.IO.StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static TransactionInstruction SetComputeUnitLimit(uint units)
    {
        byte[] data = new byte[5];
        data[0] = 2;
        BitConverter.GetBytes(units).CopyTo(data, 1);
        return new TransactionInstruction
        {
            ProgramId = new PublicKey("ComputeBudget111111111111111111111111111111").KeyBytes,
            Keys = new List<AccountMeta>(),
            Data = data
        };
    }
}
