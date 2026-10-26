using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.Numerics;
using System.Globalization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

internal sealed partial class NearSdkService
{
    private readonly string _rpcEndpoint;
    private readonly string _networkId;
    private readonly string _contractId;
    private readonly string _accountId;
    private readonly string _privateKey;
    private readonly string _nodeExecutable;
    private readonly string _bridgePath;

    public NearSdkService(string rpcEndpoint, string networkId, string contractId, string accountId, string privateKey,
        string nodeExecutable = null, string bridgePath = null)
    {
        _rpcEndpoint = rpcEndpoint;
        _networkId = networkId;
        _contractId = contractId;
        _accountId = accountId;
        _privateKey = privateKey;
        _nodeExecutable = nodeExecutable ?? Environment.GetEnvironmentVariable("OASIS_NEAR_NODE") ?? "node";
        _bridgePath = bridgePath ?? Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "NearSdkBridge", "bridge.mjs");
    }

    public async Task ProbeAsync()
    {
        if (!File.Exists(_bridgePath))
            throw new FileNotFoundException("The official near-api-js SDK bridge was not found. Install its locked npm dependencies before activating NEAROASIS.", _bridgePath);
        using var _ = await InvokeAsync("probe");
    }

    public async Task<string> GetAsync(string key)
        => (await ReadSnapshotAsync(key)).Plaintext;

    internal sealed class StoredSnapshot
    {
        public NearSdkService Owner {get;}
        public string Key {get;}
        public string Ciphertext {get;}
        public string Plaintext {get;}
        internal StoredSnapshot(NearSdkService owner,string key,string ciphertext,string plaintext)
        {Owner=owner;Key=key;Ciphertext=ciphertext;Plaintext=plaintext;}
    }

    // Preserve the SAME observed encrypted bytes for conditional writes and the decoded record for staging.
    // Re-encrypting the old plaintext would produce a different nonce and cannot be used as a CAS condition.
    public async Task<StoredSnapshot> ReadSnapshotAsync(string key)
    {
        using var codec=OpenStorageCodec();
        var context=StorageContext(key);
        using var response = await InvokeAsync("get", new { key });
        if(response.RootElement.ValueKind==JsonValueKind.Null)return new StoredSnapshot(this,key,null,null);
        if(response.RootElement.ValueKind!=JsonValueKind.String)throw new InvalidDataException("Native NEAR record must be ciphertext or absent.");
        var ciphertext=response.RootElement.GetString();
        return new StoredSnapshot(this,key,ciphertext,codec.Decrypt(context,ciphertext));
    }

    public async Task<IReadOnlyList<KeyValuePair<string, string>>> EntriesAsync(string prefix)
    {
        using var codec=OpenStorageCodec();
        using var response = await InvokeAsync("entries", new { prefix = prefix ?? string.Empty });
        return response.RootElement.EnumerateArray()
            .Select(pair => new KeyValuePair<string, string>(pair[0].GetString(), codec.Decrypt(StorageContext(pair[0].GetString()),pair[1].GetString())))
            .ToList();
    }

    public async Task<string> PutAsync(string key, string value)
    {
        using var codec=OpenStorageCodec();
        var encrypted=codec.Encrypt(StorageContext(key),value);
        EnsureWritable();
        return await CallAsync("put",new { key, value=encrypted }).ConfigureAwait(false);
    }

    private Dictionary<string,object> PrepareCheckedRecordArguments(StoredSnapshot observed,string plaintext,string archiveKey,string archivePlaintext)
    {
        ArgumentNullException.ThrowIfNull(observed);
        if(!ReferenceEquals(observed.Owner,this))throw new ArgumentException("Observed snapshot belongs to another NEAR service scope.");
        var split=observed.Key?.IndexOf(':')??-1;
        if(split<1||observed.Key.Substring(0,split) is not ("avatar" or "avatar-detail" or "holon")
            ||!Guid.TryParseExact(observed.Key.Substring(split+1),"D",out var id)||id==Guid.Empty
            ||observed.Key.Substring(split+1)!=id.ToString("D"))throw new ArgumentException("Canonical native record key required.");
        if((observed.Ciphertext==null)!=(observed.Plaintext==null)||string.IsNullOrEmpty(plaintext))throw new ArgumentException("Valid observed state and proposed record required.");
        var archive=archiveKey!=null||archivePlaintext!=null;
        if(archive){
            var prefix="history:"+observed.Key+":";
            if(observed.Ciphertext==null||archiveKey==null||!archiveKey.StartsWith(prefix,StringComparison.Ordinal)||archiveKey.Length!=prefix.Length+10
                ||!int.TryParse(archiveKey.Substring(prefix.Length),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var version)||version<1
                ||string.IsNullOrEmpty(archivePlaintext))throw new ArgumentException("A positive same-record archive requires observed current ciphertext.");
        }
        using var codec=OpenStorageCodec();
        var args=new Dictionary<string,object>{{"key",observed.Key},{"expected_value",observed.Ciphertext},{"value",codec.Encrypt(StorageContext(observed.Key),plaintext)}};
        if(archive){args["archive_key"]=archiveKey;args["archive_value"]=codec.Encrypt(StorageContext(archiveKey),archivePlaintext);}
        return args;
    }

    public async Task<string> PutRecordCheckedAsync(StoredSnapshot observed,string plaintext,string archiveKey=null,string archivePlaintext=null)
    {
        var args=PrepareCheckedRecordArguments(observed,plaintext,archiveKey,archivePlaintext);
        EnsureWritable();
        try{
            using var response=await InvokeAsync("putRecordChecked",new{argsBase64=Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(args))});
            return ReadCheckedRecordOutcome(response.RootElement);
        }
        catch(NearRecordAdmissionException){throw;}
        catch(NearMutationUncertainException){throw;}
        catch(NearRecordWriteRejectedException){throw;}
        catch(NearRecordMutationUncertainException){throw;}
        catch(Exception cause){throw new NearRecordMutationUncertainException(null,cause);}
    }

    private static string ReadCheckedRecordOutcome(JsonElement response)
    {
        string hash=null;
        try{
            hash=response.GetProperty("transactionHash").GetString();
            if(string.IsNullOrWhiteSpace(hash))throw new InvalidDataException("Native checked write lacks an original transaction identity.");
            if(response.GetProperty("finalExecutionStatus").GetString()!="FINAL")throw new InvalidDataException("Native checked write is not FINAL.");
            var status=response.GetProperty("nativeStatus");
            if(status.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Missing native checked-write status.");
            var success=status.TryGetProperty("SuccessValue",out var value);var failure=status.TryGetProperty("Failure",out var rejected);
            if(success==failure)throw new InvalidDataException("Ambiguous native checked-write status.");
            if(failure){if(rejected.ValueKind!=JsonValueKind.Object||!rejected.EnumerateObject().Any())throw new InvalidDataException("Malformed native failure.");throw new NearRecordWriteRejectedException(hash,status.GetRawText());}
            if(value.ValueKind!=JsonValueKind.String||value.GetString()!=string.Empty)throw new InvalidDataException("Unexpected checked-write success value.");
            return hash;
        }
        catch(NearRecordWriteRejectedException){throw;}
        catch(Exception cause){throw new NearRecordMutationUncertainException(hash,cause);}
    }

    public async Task<string> DeleteAsync(string key)
    {
        using var codec=OpenStorageCodec();
        _=StorageContext(key);
        EnsureWritable();
        return await CallAsync("delete_record",new { key }).ConfigureAwait(false);
    }

    private static EncryptedRecordCodec OpenStorageCodec()
    {
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_NEAR_STORAGE_ENCRYPTION_KEY");
        try { return new EncryptedRecordCodec(key,compress:true); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private string StorageContext(string key)
    {
        if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A nonempty NEAR storage key is required.",nameof(key));
        return "oasis/near/storage/v1/"+_networkId+"/"+_contractId+"/"+key;
    }

    public async Task<NearKeyPair> GenerateKeyPairAsync()
    {
        using var response = await InvokeAsync("generateKey");
        return new NearKeyPair(
            response.RootElement.GetProperty("publicKey").GetString(),
            response.RootElement.GetProperty("privateKey").GetString(),
            response.RootElement.GetProperty("implicitAccountId").GetString());
    }

    public async Task<NearKeyPair> DerivePublicKeyAsync(string secretKey,CancellationToken cancellationToken=default)
    {
        using var response = await InvokeAsync("derivePublicKey", new { secretKey },cancellationToken);
        return new NearKeyPair(
            response.RootElement.GetProperty("publicKey").GetString(),
            secretKey,
            response.RootElement.GetProperty("implicitAccountId").GetString());
    }

    public async Task<(NearKeyPair Key,string SeedPhrase)> GenerateMnemonicAsync(CancellationToken cancellationToken=default)
    {
        using var response=await InvokeAsync("generateMnemonic",cancellationToken:cancellationToken);
        var row=response.RootElement;
        return (new NearKeyPair(row.GetProperty("publicKey").GetString(),row.GetProperty("privateKey").GetString(),row.GetProperty("implicitAccountId").GetString()),row.GetProperty("seedPhrase").GetString());
    }

    public async Task<NearKeyPair> RestoreMnemonicAsync(string seedPhrase,CancellationToken cancellationToken=default)
    {
        using var response=await InvokeAsync("deriveMnemonic",new{seedPhrase},cancellationToken);
        var row=response.RootElement;
        return new NearKeyPair(row.GetProperty("publicKey").GetString(),row.GetProperty("privateKey").GetString(),row.GetProperty("implicitAccountId").GetString());
    }

    public async Task<string> GetBalanceYoctoAsync(string accountId)
    {
        using var response = await InvokeAsync("balance", new { targetAccountId = accountId });
        return response.RootElement.GetProperty("amount").GetString();
    }

    public async Task<string> TransferAsync(string receiverId, string amountYocto,
        string signerAccountId = null, string signerPrivateKey = null)
    {
        EnsureWritable(signerAccountId, signerPrivateKey);
        var intent=new MutationIntent("Transfer",CanonicalNativeAmount(amountYocto),null,default,0);
        return await InvokeMutationAsync("transfer", new
        {
            receiverId,
            amountYocto,
            accountId = signerAccountId ?? _accountId,
            privateKey = signerPrivateKey ?? _privateKey
        },signerAccountId ?? _accountId,receiverId,intent);
    }

    public async Task<string> CallAsync(string methodName, object args, string targetContractId = null,
        string depositYocto = "0", string signerAccountId = null, string signerPrivateKey = null)
    {
        EnsureWritable(signerAccountId, signerPrivateKey);
        const ulong gas=100_000_000_000_000;
        var intent=new MutationIntent("FunctionCall",CanonicalNativeAmount(depositYocto),methodName,
            JsonSerializer.SerializeToElement(args??new{}),gas);
        return await InvokeMutationAsync("call", new
        {
            methodName,
            argsBase64=Convert.ToBase64String(Encoding.UTF8.GetBytes(intent.Arguments.GetRawText())),
            targetContractId = targetContractId ?? _contractId,
            depositYocto,
            gas,
            accountId = signerAccountId ?? _accountId,
            privateKey = signerPrivateKey ?? _privateKey
        },signerAccountId ?? _accountId,targetContractId ?? _contractId,intent);
    }

    private sealed record MutationIntent(string Kind,string Deposit,string Method,JsonElement Arguments,ulong Gas);

    private static string CanonicalNativeAmount(string value)
    {
        if(!BigInteger.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var amount)||amount.Sign<0)
            throw new ArgumentException("A nonnegative decimal native amount is required.",nameof(value));
        return amount.ToString(CultureInfo.InvariantCulture);
    }

    private static void ValidateMutationAction(JsonElement transaction,MutationIntent intent)
    {
        var actions=transaction.GetProperty("actions");
        if(actions.ValueKind!=JsonValueKind.Array||actions.GetArrayLength()!=1)
            throw new InvalidDataException("Native mutation must contain exactly its admitted action.");
        var action=actions[0];
        if(action.ValueKind!=JsonValueKind.Object||action.EnumerateObject().Count()!=1||!action.TryGetProperty(intent.Kind,out var body)
            ||body.ValueKind!=JsonValueKind.Object||body.EnumerateObject().Count()!=(intent.Kind=="Transfer"?1:4)
            ||body.GetProperty("deposit").GetString()!=intent.Deposit)
            throw new InvalidDataException("Native mutation action or amount differs from admitted intent.");
        if(intent.Kind=="FunctionCall"){
            if(body.GetProperty("method_name").GetString()!=intent.Method||body.GetProperty("gas").GetUInt64()!=intent.Gas)
                throw new InvalidDataException("Native mutation method or gas differs from admitted intent.");
            using var arguments=JsonDocument.Parse(Convert.FromBase64String(body.GetProperty("args").GetString()));
            if(!JsonElement.DeepEquals(arguments.RootElement,intent.Arguments))
                throw new InvalidDataException("Native mutation arguments differ from admitted intent.");
        }
    }

    private async Task<string> InvokeMutationAsync(string operation,object args,string signer,string receiver,MutationIntent intent)
    {
        string hash=null;
        try{
            using var response=await InvokeAsync(operation,args);
            var root=response.RootElement;
            hash=root.GetProperty("transactionHash").GetString();
            if(string.IsNullOrWhiteSpace(hash))throw new InvalidDataException("Native mutation lacks an original transaction identity.");
            var outcome=root.GetProperty("outcome");
            if(outcome.GetProperty("transaction_outcome").GetProperty("id").GetString()!=hash)
                throw new InvalidDataException("Native mutation outcome transaction identity differs.");
            var transaction=outcome.GetProperty("transaction");
            if(transaction.GetProperty("signer_id").GetString()!=signer||transaction.GetProperty("receiver_id").GetString()!=receiver)
                throw new InvalidDataException("Native mutation signer or receiver differs from admitted intent.");
            ValidateMutationAction(transaction,intent);
            var status=ClassifyTransactionOutcome(outcome);
            if(status=="Failed")throw new NearMutationRejectedException(hash,outcome.GetProperty("status").GetRawText());
            if(status!="Completed")throw new InvalidDataException("Native mutation execution is not FINAL.");
            return hash;
        }
        catch(NearMutationRejectedException){throw;}
        catch(NearMutationUncertainException){throw;}
        catch(Exception cause){throw new NearMutationUncertainException(hash,cause);}
    }

    public async Task<JsonElement> ViewAsync(string methodName, object args, string targetContractId = null)
    {
        using var response = await InvokeAsync("view", new { methodName, args, targetContractId = targetContractId ?? _contractId });
        return response.RootElement.Clone();
    }

    public async Task<string> GetTransactionStatusAsync(string transactionHash, string signerAccountId = null,
        CancellationToken cancellationToken = default)
        =>ClassifyTransactionOutcome(await ReadTransactionOutcomeAsync(transactionHash,signerAccountId,cancellationToken));

    private async Task<JsonElement> ReadTransactionOutcomeAsync(string transactionHash,string signerAccountId,CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(transactionHash))throw new ArgumentException("A transaction hash is required.",nameof(transactionHash));
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        JsonDocument response;
        try{response = await InvokeAsync("transactionStatus", new
        {
            transactionHash,
            signerAccountId = signerAccountId ?? _accountId
        },deadline.Token);}
        catch(OperationCanceledException ex) when(!cancellationToken.IsCancellationRequested)
        {throw new TimeoutException("NEAR transaction-status observation exceeded30seconds; outcome remains unknown.",ex);}
        using var ownedResponse=response;
        var outcome=response.RootElement.GetProperty("outcome");
        if(outcome.GetProperty("transaction_outcome").GetProperty("id").GetString()!=transactionHash)
            throw new InvalidDataException("NEAR status outcome does not match the requested transaction.");
        _=ClassifyTransactionOutcome(outcome);
        return outcome.Clone();
    }

    internal async Task VerifyNftLockSourceAsync(string transactionHash,string tokenId,CancellationToken token=default)
    {
        var outcome=await ReadTransactionOutcomeAsync(transactionHash,_accountId,token);
        if(ClassifyTransactionOutcome(outcome)!="Completed")throw new InvalidDataException("Source NFT lock must be finalized successfully.");
        var transaction=outcome.GetProperty("transaction");
        if(transaction.GetProperty("signer_id").GetString()!=_accountId||transaction.GetProperty("receiver_id").GetString()!=_contractId)
            throw new InvalidDataException("Source NFT lock signer or contract differs from the configured owner.");
        var gas=transaction.GetProperty("actions")[0].GetProperty("FunctionCall").GetProperty("gas").GetUInt64();
        if(gas==0)throw new InvalidDataException("Source NFT lock gas is invalid.");
        ValidateMutationAction(transaction,new MutationIntent("FunctionCall","0","nft_set_locked",JsonSerializer.SerializeToElement(new{token_id=tokenId,locked=true}),gas));
    }

    internal static string ClassifyTransactionOutcome(JsonElement outcome)
    {
        if(outcome.ValueKind!=JsonValueKind.Object || !outcome.TryGetProperty("final_execution_status",out var finality)
            || finality.ValueKind!=JsonValueKind.String)
            throw new InvalidDataException("NEAR status requires explicit execution finality.");
        var state=finality.GetString();
        if(state is "NONE" or "INCLUDED" or "INCLUDED_FINAL" or "EXECUTED" or "EXECUTED_OPTIMISTIC")return "Pending";
        if(state!="FINAL")throw new InvalidDataException("Unknown NEAR execution finality.");
        if(!outcome.TryGetProperty("status",out var status) || status.ValueKind!=JsonValueKind.Object
            || status.EnumerateObject().Count()!=1)
            throw new InvalidDataException("Final NEAR outcome requires one explicit execution result.");
        if(status.TryGetProperty("Failure",out var failure) && failure.ValueKind==JsonValueKind.Object && failure.EnumerateObject().Any())return "Failed";
        if(status.TryGetProperty("SuccessValue",out var success) && success.ValueKind==JsonValueKind.String)return "Completed";
        throw new InvalidDataException("Final NEAR execution outcome is malformed.");
    }

    private void EnsureWritable(string accountId = null, string privateKey = null)
    {
        if (string.IsNullOrWhiteSpace(accountId ?? _accountId) || string.IsNullOrWhiteSpace(privateKey ?? _privateKey))
            throw new InvalidOperationException("NEAR accountId and privateKey are required for storage writes.");
    }

    private readonly OwnedPreparationScope _preparationScope=new();
    internal bool HasPreparationOwner=>_preparationScope.HasCurrent;
    internal Task<T> WithPreparationAsync<T>(Func<Task<T>> operation,Func<string,string,PreparedSubmissionServer> factory,CancellationToken token)
        =>_preparationScope.RunAsync(operation,factory,token);

    private async Task<JsonDocument> InvokeAsync(string operation, object args = null, CancellationToken cancellationToken = default)
    {
        var scopeToken=_preparationScope.CurrentToken;
        using var linked=scopeToken.CanBeCanceled&&cancellationToken.CanBeCanceled
            ?CancellationTokenSource.CreateLinkedTokenSource(scopeToken,cancellationToken):null;
        cancellationToken=linked?.Token??(scopeToken.CanBeCanceled?scopeToken:cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        using var server=operation is "transfer" or "call" or "putRecordChecked"
            ?_preparationScope.CreateCurrent(operation,PreparationContext(operation,args)):null;
        return await InvokeCoreAsync(operation,args,cancellationToken,server).ConfigureAwait(false);
    }

    private string PreparationContext(string operation,object args)
    {
        var input=JsonSerializer.SerializeToElement(args);
        var sender=input.TryGetProperty("accountId",out var supplied)?supplied.GetString():_accountId;
        var intent=new Dictionary<string,object>();
        if(operation=="transfer"){
            intent["kind"]="transfer";intent["recipient"]=input.GetProperty("receiverId").GetString();
            intent["depositUnits"]=CanonicalNativeAmount(input.GetProperty("amountYocto").GetString());
        }else{
            intent["kind"]="functionCall";
            intent["recipient"]=operation=="putRecordChecked"?_contractId:input.GetProperty("targetContractId").GetString();
            intent["depositUnits"]=operation=="putRecordChecked"?"0":CanonicalNativeAmount(input.GetProperty("depositYocto").GetString());
            intent["methodName"]=operation=="putRecordChecked"?"put_record_checked":input.GetProperty("methodName").GetString();
            intent["gasUnits"]="100000000000000";
            intent["argsBase64"]=input.GetProperty("argsBase64").GetString();
        }
        return JsonSerializer.Serialize(new{sender,intent});
    }

    private async Task<JsonDocument> InvokeCoreAsync(string operation,object args,CancellationToken cancellationToken,PreparedSubmissionServer preparationServer)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var submitting=preparationServer!=null;
        using var expectedContext=preparationServer!=null&&operation is "transfer" or "call" or "putRecordChecked"
            ?JsonDocument.Parse(PreparationContext(operation,args)):null;
        if(operation=="submitNativePreparation"&&preparationServer==null)throw new InvalidOperationException("NEAR original submission requires the existing owned preparation server.");
        var request = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["rpcEndpoint"] = _rpcEndpoint,
            ["networkId"] = _networkId,
            ["contractId"] = _contractId,
            ["accountId"] = _accountId,
            ["privateKey"] = _privateKey
        };
        if (args != null)
            foreach (var property in args.GetType().GetProperties())
                request[property.Name] = property.GetValue(args);
        if(operation is "verifyNativePreparation" or "recoverNativePreparation" or "preparedNativeOutcome")request.Remove("privateKey");

        var startInfo = new ProcessStartInfo
        {
            FileName = _nodeExecutable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false, true),
            StandardOutputEncoding = new UTF8Encoding(false, true),
            StandardErrorEncoding = new UTF8Encoding(false, true),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_bridgePath);
        startInfo.Environment.Remove("OASIS_NATIVE_PREPARATION_PIPE");
        startInfo.Environment.Remove("OASIS_NATIVE_PREPARATION_MODULE");
        preparationServer?.ConfigureChild(startInfo,Path.Combine(AppContext.BaseDirectory,"NearSdkBridge","prepared-submission.mjs"));
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if(preparationServer!=null||operation is "prepareNativeAction" or "verifyNativePreparation" or "recoverNativePreparation" or "preparedNativeOutcome")
            deadline.CancelAfter(TimeSpan.FromSeconds(45));
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the official NEAR SDK bridge.");
        var serving=preparationServer?.ServeAsync(deadline.Token);
        var servingObserved=false;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(),deadline.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token);
            var stdout=await stdoutTask;var stderr=await stderrTask;
            if(string.IsNullOrWhiteSpace(stdout))throw new InvalidOperationException($"NEAR SDK bridge returned no response (exit {process.ExitCode}): {stderr}");
            using var envelope=JsonDocument.Parse(stdout);
            if(process.ExitCode!=0||!envelope.RootElement.TryGetProperty("ok",out var ok)||!ok.GetBoolean())
            {
                if(serving!=null){
                    deadline.Cancel();
                    try{await serving;}
                    catch(OperationCanceledException cancelled) when(cancelled.CancellationToken==deadline.Token&&!cancellationToken.IsCancellationRequested){}
                    finally{servingObserved=true;}
                }
                var error=envelope.RootElement.TryGetProperty("error",out var value)?value.GetString():stderr;
                if(operation=="putRecordChecked"&&envelope.RootElement.TryGetProperty("checkedWritePhase",out var phase)&&phase.ValueKind==JsonValueKind.String&&phase.GetString()=="before-submission")
                    throw new NearRecordAdmissionException($"NEAR checked-write admission failed: {error}");
                throw new InvalidOperationException($"NEAR SDK operation failed: {error}");
            }
            var result=envelope.RootElement.GetProperty("result");
            if(serving!=null){
                try{await serving;}finally{servingObserved=true;}
                if(preparationServer.PreparedTransactionId==null||result.GetProperty("transactionHash").GetString()!=preparationServer.PreparedTransactionId
                    ||!result.GetProperty("persisted").GetBoolean()||!result.GetProperty("submitted").GetBoolean())
                    throw new InvalidDataException("NEAR submission reply differs from the durably acknowledged original.");
                if(expectedContext!=null){
                    var original=JsonSerializer.SerializeToElement(new{transactionHash=preparationServer.PreparedTransactionId,
                        sender=expectedContext.RootElement.GetProperty("sender").GetString(),intent=expectedContext.RootElement.GetProperty("intent")});
                    _=ValidatePreparedOutcome(original,result.GetProperty("outcome"));
                    if(operation=="putRecordChecked"&&(result.GetProperty("finalExecutionStatus").GetString()!=result.GetProperty("outcome").GetProperty("final_execution_status").GetString()
                        ||!JsonElement.DeepEquals(result.GetProperty("nativeStatus"),result.GetProperty("outcome").GetProperty("status"))))
                        throw new InvalidDataException("Checked-write projection differs from its bound native outcome.");
                }
            }
            return JsonDocument.Parse(result.GetRawText());
        }
        catch(Exception failure) when(submitting&&preparationServer.PreparedTransactionId!=null&&failure is not Core.Interfaces.INativeMutationReconciliationRequired)
        {
            throw new NearMutationUncertainException(preparationServer.PreparedTransactionId,failure);
        }
        finally
        {
            deadline.Cancel();
            if(!process.HasExited)process.Kill(entireProcessTree:true);
            await process.WaitForExitAsync();
            try{await Task.WhenAll(stdoutTask,stderrTask);}catch(Exception error) when(error is IOException or DecoderFallbackException){}
            if(serving!=null){
                try{await serving;}
                catch(OperationCanceledException) when(deadline.IsCancellationRequested){}
                catch(Exception) when(servingObserved){}
            }
        }
    }
}

internal sealed record NearKeyPair(string PublicKey, string PrivateKey, string ImplicitAccountId);
