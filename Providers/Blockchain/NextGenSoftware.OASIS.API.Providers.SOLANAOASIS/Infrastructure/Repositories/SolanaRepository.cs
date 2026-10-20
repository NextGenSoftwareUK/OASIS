namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public partial class SolanaRepository(Account oasisAccount, IRpcClient rpcClient, Func<byte[]> storageKeyResolver) : ISolanaRepository
{
    public SolanaRepository(Account oasisAccount, IRpcClient rpcClient) : this(oasisAccount, rpcClient, null) { }
    private const string MemoIdentity = "oasis/solana/private-memo/v1";
    // Finalization requires additional rooted slots after confirmation. Observe up
    // to ~60s rather than assuming the earlier ~15s window always suffices.
    // This is a read-only observation policy, never permission to resubmit.
    private const int FinalityObservationAttempts = 120;
    private const int FinalityObservationIntervalMs = 500;
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
        ValidateStorageEntity(entity);
        var serializedEntity = EncodeEntity(entity);
        var blockHash = await GetWritableBlockHashAsync();
        var entityTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
            .Build(oasisAccount);

        return await SubmitAndConfirmAsync(entityTransactionBytes);
    }

    public async Task<string> UpdateAsync<T>(T entity)
        where T : SolanaBaseDto, new()
    {
        ValidateStorageEntity(entity);
        var nextVersion=checked(entity.Version + 1);
        var snapshot=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.ParseStoredObject(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(entity));
        snapshot["Version"]=nextVersion;
        snapshot["PreviousVersionId"]=entity.Id;
        var serializedEntity = EncodeEntity(snapshot);
        var blockHash = await GetWritableBlockHashAsync();

        var entityUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
            .Build(oasisAccount);

        var signature=await SubmitAndConfirmAsync(entityUpdateTransactionBytes);
        entity.Version=nextVersion;
        entity.PreviousVersionId=entity.Id;
        return signature;
    }

    public async Task<bool> DeleteAsync(string transactionHashReference)
    {
        await CreateTombstoneAsync(transactionHashReference);
        return true;
    }

    public async Task<string> CreateTombstoneAsync(string transactionHashReference)
    {
        var transactionContent = await ReadStoredRecordAsync(transactionHashReference);
        var deserializedEntity = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.ParseStoredObject(transactionContent);
        if(deserializedEntity.Value<string>("RecordKind") is not ("oasis.avatar.v1" or "oasis.avatar-detail.v1" or "oasis.holon.v1"))
            throw new InvalidOperationException("Solana tombstone requires a canonical typed storage record.");

        var identity = deserializedEntity["Id"]?.ToObject<Guid?>();
        var version = deserializedEntity.Value<int?>("Version");
        if (!identity.HasValue || identity.Value == Guid.Empty || !version.HasValue || version.Value < 0)
            throw new InvalidOperationException("Stored Solana record has invalid identity/version.");
        // A signature identifies an immutable revision, not necessarily the current record.
        // Reject known stale revisions before signing; this is not atomic chain-side CAS.
        var kind=deserializedEntity.Value<string>("RecordKind");
        if(kind=="oasis.avatar.v1") await RequireCurrentTombstoneReferenceAsync<SolanaAvatarDto>(identity.Value,transactionHashReference);
        else if(kind=="oasis.avatar-detail.v1") await RequireCurrentTombstoneReferenceAsync<SolanaAvatarDetailDto>(identity.Value,transactionHashReference);
        else await RequireCurrentTombstoneReferenceAsync<SolanaHolonDto>(identity.Value,transactionHashReference);
        deserializedEntity["IsDeleted"] = true;
        deserializedEntity["DeletedDate"] = DateTime.UtcNow;
        deserializedEntity["Version"] = checked(version.Value + 1);
        deserializedEntity["PreviousVersionId"] = identity.Value;
        var serializedEntity = EncodeEntity(deserializedEntity);
        var blockHash = await GetWritableBlockHashAsync();

        var entityDeleteUpdateTransactionBytes = new TransactionBuilder()
            .SetRecentBlockHash(blockHash)
            .SetFeePayer(oasisAccount)
            .AddInstruction(SetComputeUnitLimit(1_400_000))
            .AddInstruction(MemoProgram.NewMemoV2(serializedEntity))
            .Build(oasisAccount);

        return await SubmitAndConfirmAsync(entityDeleteUpdateTransactionBytes);
    }

    private async Task RequireCurrentTombstoneReferenceAsync<T>(Guid id,string signature) where T:SolanaBaseDto,new()
    {
        var current=(await GetAllAsync<T>()).SingleOrDefault(entry=>entry.Record.Id==id);
        if(current.Record==null) throw new KeyNotFoundException("No current nondeleted Solana record matches the deletion identity.");
        if(!string.Equals(current.Signature,signature,StringComparison.Ordinal))
            throw new InvalidOperationException("Solana deletion signature is stale; no transaction submitted.");
    }

    public async Task<T> GetAsync<T>(string transactionHashReference)
        where T : SolanaBaseDto, new()
    {
        var transactionContent = await ReadStoredRecordAsync(transactionHashReference);
        return DeserializeStoredRecord<T>(transactionContent);
    }

    private static T DeserializeStoredRecord<T>(string content) where T : SolanaBaseDto,new()
    {
        var document=Newtonsoft.Json.Linq.JObject.Parse(content);
        if(document.Value<string>("RecordKind") != new T().RecordKind)
            throw new InvalidOperationException("Solana storage record kind does not match the requested type; untyped historical memos require explicit review.");
        var record=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<T>(content) ?? throw new InvalidOperationException("Finalized Solana memo has no record content.");
        if(record.Id == Guid.Empty || record.Version < 0)
            throw new InvalidOperationException("Solana storage record has invalid identity/version.");
        if(!record.HasValidCreationOrigin())
            throw new System.IO.InvalidDataException("Stored Solana record has an invalid creation origin.");
        return record;
    }

    private static void ValidateStorageEntity(SolanaBaseDto entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if(!entity.HasValidCreationOrigin()) throw new SolanaOriginValidationException();
        if(entity.Id==Guid.Empty || entity.Version<0)
            throw new ArgumentException("Solana storage requires a nonempty identity and nonnegative version.",nameof(entity));
    }

    private async Task<string> ReadStoredRecordAsync(string signature)
    {
        var queried = await rpcClient.GetTransactionAsync(signature, Commitment.Finalized);
        if (!queried.WasSuccessful || queried.HttpStatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Solana finalized record lookup failed: " + queried.RawRpcResponse);
        var memo = ValidateStoredTransaction(queried.Result, signature, oasisAccount.PublicKey.Key);
        return DecodeEntity(memo);
    }

    private async Task<string> GetWritableBlockHashAsync()
    {
        var response=await rpcClient.GetLatestBlockHashAsync();
        if(response == null || !response.WasSuccessful || response.HttpStatusCode != HttpStatusCode.OK
            || string.IsNullOrWhiteSpace(response.Result?.Value?.Blockhash))
            throw new InvalidOperationException("Solana blockhash lookup failed before transaction submission.");
        var hash=response.Result.Value.Blockhash;
        if(Encoders.Base58.DecodeData(hash).Length != 32)
            throw new InvalidOperationException("Solana blockhash lookup returned an invalid native hash.");
        return hash;
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

    internal Task<string> SubmitSignedTransactionAsync(byte[] signedTransaction) => SubmitAndConfirmAsync(signedTransaction);

    private async Task<string> SubmitAndConfirmAsync(byte[] signedTransaction)
    {
        // The first (fee-payer) signature is the native transaction identity, including
        // multisigner mint transactions. Decode canonical Solana compact-u16 first.
        if(signedTransaction==null) throw new ArgumentNullException(nameof(signedTransaction));
        var prefix=0;var count=0;var shift=0;byte tag;
        do
        {
            if(prefix>=signedTransaction.Length || prefix==3) throw new ArgumentException("Invalid Solana signature-count encoding.");
            tag=signedTransaction[prefix++];
            if(prefix==3 && tag>3) throw new ArgumentException("Solana signature count exceeds compact-u16.");
            count|=(tag&127)<<shift;shift+=7;
        } while((tag&128)!=0);
        if(count==0 || (prefix>1 && (tag&127)==0) || signedTransaction.Length<=prefix+count*64)
            throw new ArgumentException("Solana transaction has a noncanonical or incomplete signature section.");
        var signature = Encoders.Base58.EncodeData(signedTransaction.AsSpan(prefix,64).ToArray());
        try
        {
            var submitted = await rpcClient.SendTransactionAsync(signedTransaction);
            if (!submitted.WasSuccessful || submitted.HttpStatusCode != HttpStatusCode.OK || submitted.Result != signature)
                throw new InvalidOperationException("Solana submission did not acknowledge the signed transaction: " + submitted.RawRpcResponse);
            // Bounded status reads only; never rebroadcast. A missing/pending status
            // is not proof of failure or permission to replay on another provider.
            string lastStatus = "not observed";
            for (var attempt = 0; attempt < FinalityObservationAttempts; attempt++)
            {
                var statuses = await rpcClient.GetSignatureStatusesAsync(new List<string> { signature }, true);
                if (!statuses.WasSuccessful || statuses.Result?.Value == null || statuses.Result.Value.Count != 1)
                    throw new InvalidOperationException("Solana status response was unavailable or malformed.");
                var status = statuses.Result.Value[0];
                lastStatus = status?.ConfirmationStatus ?? "not found";
                if (status?.Error != null) throw new InvalidOperationException("Solana transaction reports native execution error: " + status.Error);
                if (status?.ConfirmationStatus == "finalized") return signature;
                if (attempt < FinalityObservationAttempts - 1) await Task.Delay(FinalityObservationIntervalMs);
            }
            throw new TimeoutException("Solana transaction has not reached finalized status within the observation window; last status: " + lastStatus);
        }
        catch (Exception error) { throw new SolanaTransactionReconciliationException(signature, error); }
    }

    private string EncodeEntity<T>(T entity)
    {
        using var codec = OpenCodec();
        return codec.Encrypt(MemoIdentity, NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(entity));
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
