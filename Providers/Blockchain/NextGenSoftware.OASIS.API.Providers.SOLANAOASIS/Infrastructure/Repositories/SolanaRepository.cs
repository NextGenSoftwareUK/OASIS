namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public class SolanaRepository(Account oasisAccount, IRpcClient rpcClient, Func<byte[]> storageKeyResolver) : ISolanaRepository
{
    public SolanaRepository(Account oasisAccount, IRpcClient rpcClient) : this(oasisAccount, rpcClient, null) { }
    private const string MemoIdentity = "oasis/solana/private-memo/v1";
    private NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.EncryptedRecordCodec OpenCodec()
    {
        var key = storageKeyResolver != null ? storageKeyResolver()
            : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_SOLANA_STORAGE_ENCRYPTION_KEY");
        try { return new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.EncryptedRecordCodec(key, compress:true); }
        finally { if (key != null) System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }
    }
    public async Task<string> CreateAsync<T>(T entity)
        where T : SolanaBaseDto, new()
    {
        var serializedEntity = EncodeEntity(entity);
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        if (blockHash.Result != null)
        {
            var entityTransactionBytes = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount)
                .AddInstruction(SetComputeUnitLimit(1_400_000))
                .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
                .Build(oasisAccount);

            return await SubmitAndConfirmAsync(entityTransactionBytes);
        }
        else
            return "";
    }

    public async Task<string> UpdateAsync<T>(T entity)
        where T : SolanaBaseDto, new()
    {
        entity.Version++;
        entity.PreviousVersionId = entity.Id;
        var serializedEntity = EncodeEntity(entity);
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        var entityUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
            .Build(oasisAccount);

        return await SubmitAndConfirmAsync(entityUpdateTransactionBytes);
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

        await SubmitAndConfirmAsync(entityDeleteUpdateTransactionBytes);
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

    private async Task<string> SubmitAndConfirmAsync(byte[] signedTransaction)
    {
        // This repository builds one-signer transactions; preserve its actual native
        // identity before broadcast, including when the RPC response is lost.
        if (signedTransaction == null || signedTransaction.Length < 65 || signedTransaction[0] != 1)
            throw new ArgumentException("Expected a one-signature Solana transaction.");
        var signature = Encoders.Base58.EncodeData(signedTransaction.AsSpan(1,64).ToArray());
        try
        {
            var submitted = await rpcClient.SendTransactionAsync(signedTransaction);
            if (!submitted.WasSuccessful || submitted.HttpStatusCode != HttpStatusCode.OK || submitted.Result != signature)
                throw new InvalidOperationException("Solana submission did not acknowledge the signed transaction: " + submitted.RawRpcResponse);
            // Bounded status reads only; never rebroadcast. A missing/pending status
            // is not proof of failure or permission to replay on another provider.
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var statuses = await rpcClient.GetSignatureStatusesAsync(new List<string> { signature }, true);
                if (!statuses.WasSuccessful || statuses.Result?.Value == null || statuses.Result.Value.Count != 1)
                    throw new InvalidOperationException("Solana status response was unavailable or malformed.");
                var status = statuses.Result.Value[0];
                if (status?.Error != null) throw new InvalidOperationException("Solana transaction reports native execution error: " + status.Error);
                if (status?.ConfirmationStatus == "finalized") return signature;
                if (attempt < 29) await Task.Delay(500);
            }
            throw new TimeoutException("Solana transaction has not reached finalized status within the observation window.");
        }
        catch (Exception error) { throw new SolanaTransactionReconciliationException(signature, error); }
    }

    private string EncodeEntity<T>(T entity)
    {
        using var codec = OpenCodec();
        return codec.Encrypt(MemoIdentity, JsonConvert.SerializeObject(entity));
    }

    private string DecodeEntity(byte[] transactionData)
    {
        using var codec = OpenCodec();
        return codec.Decrypt(MemoIdentity, new UTF8Encoding(false,true).GetString(transactionData));
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
