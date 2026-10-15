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
        await CreateTombstoneAsync(transactionHashReference);
        return true;
    }

    public async Task<string> CreateTombstoneAsync(string transactionHashReference)
    {
        var blockHash = await rpcClient.GetLatestBlockHashAsync();

        var transactionContent = await ReadStoredRecordAsync(transactionHashReference);
        var deserializedEntity = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(transactionContent)
            ?? throw new InvalidOperationException("Finalized Solana memo has no record content.");

        var identity = deserializedEntity["Id"]?.ToObject<Guid?>();
        var version = deserializedEntity.Value<int?>("Version");
        if (!identity.HasValue || identity.Value == Guid.Empty || !version.HasValue || version.Value < 0)
            throw new InvalidOperationException("Stored Solana record has invalid identity/version.");
        deserializedEntity["IsDeleted"] = true;
        deserializedEntity["Version"] = checked(version.Value + 1);
        deserializedEntity["PreviousVersionId"] = identity.Value;
        var serializedEntity = EncodeEntity(deserializedEntity);

        var entityDeleteUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
            .Build(oasisAccount);

        return await SubmitAndConfirmAsync(entityDeleteUpdateTransactionBytes);
    }

    public async Task<T> GetAsync<T>(string transactionHashReference)
        where T : SolanaBaseDto, new()
    {
        var transactionContent = await ReadStoredRecordAsync(transactionHashReference);
        var deserializedEntity = JsonConvert.DeserializeObject<T>(transactionContent)
            ?? throw new InvalidOperationException("Finalized Solana memo has no record content.");

        return deserializedEntity;
    }

    private async Task<string> ReadStoredRecordAsync(string signature)
    {
        var queried = await rpcClient.GetTransactionAsync(signature, Commitment.Finalized);
        if (!queried.WasSuccessful || queried.HttpStatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Solana finalized record lookup failed: " + queried.RawRpcResponse);
        var memo = ValidateStoredTransaction(queried.Result, signature, oasisAccount.PublicKey.Key);
        return DecodeEntity(memo);
    }

    private static byte[] ValidateStoredTransaction(TransactionMetaSlotInfo native, string signature, string writer)
    {
        if (native?.Meta == null || native.Meta.Error != null)
            throw new InvalidOperationException("Solana storage transaction is missing success metadata or failed execution.");
        var transaction = native.Transaction;
        if (transaction?.Signatures == null || transaction.Signatures.Length != 1 || transaction.Signatures[0] != signature)
            throw new InvalidOperationException("Solana storage transaction signature does not match its reference.");
        var message = transaction.Message;
        if (message?.AccountKeys == null || message.AccountKeys.Length == 0 || message.AccountKeys[0] != writer
            || message.Instructions == null)
            throw new InvalidOperationException("Solana storage transaction has an unexpected writer or missing message.");
        var memos = new List<InstructionInfo>();
        foreach (var instruction in message.Instructions)
        {
            if (instruction == null || instruction.ProgramIdIndex >= message.AccountKeys.Length)
                throw new InvalidOperationException("Solana storage instruction has an invalid program index.");
            if (message.AccountKeys[instruction.ProgramIdIndex] == "MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr")
                memos.Add(instruction);
        }
        if (memos.Count != 1 || string.IsNullOrWhiteSpace(memos[0].Data))
            throw new InvalidOperationException("Solana storage requires exactly one Memo-v2 payload.");
        return Encoders.Base58.DecodeData(memos[0].Data);
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
