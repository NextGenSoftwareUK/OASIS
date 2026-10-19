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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

internal sealed class NearSdkService
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
        using var response = await InvokeAsync("put", new { key, value=encrypted });
        return response.RootElement.GetProperty("transactionHash").GetString();
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
            using var response=await InvokeAsync("putRecordChecked",new{args});
            return ReadCheckedRecordOutcome(response.RootElement);
        }
        catch(NearRecordAdmissionException){throw;}
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
        using var response = await InvokeAsync("delete", new { key });
        return response.RootElement.GetProperty("transactionHash").GetString();
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

    public async Task<NearKeyPair> DerivePublicKeyAsync(string secretKey)
    {
        using var response = await InvokeAsync("derivePublicKey", new { secretKey });
        return new NearKeyPair(
            response.RootElement.GetProperty("publicKey").GetString(),
            secretKey,
            response.RootElement.GetProperty("implicitAccountId").GetString());
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
        using var response = await InvokeAsync("transfer", new
        {
            receiverId,
            amountYocto,
            accountId = signerAccountId ?? _accountId,
            privateKey = signerPrivateKey ?? _privateKey
        });
        return response.RootElement.GetProperty("transactionHash").GetString();
    }

    public async Task<string> CallAsync(string methodName, object args, string targetContractId = null,
        string depositYocto = "0", string signerAccountId = null, string signerPrivateKey = null)
    {
        EnsureWritable(signerAccountId, signerPrivateKey);
        using var response = await InvokeAsync("call", new
        {
            methodName,
            args,
            targetContractId = targetContractId ?? _contractId,
            depositYocto,
            accountId = signerAccountId ?? _accountId,
            privateKey = signerPrivateKey ?? _privateKey
        });
        return response.RootElement.GetProperty("transactionHash").GetString();
    }

    public async Task<JsonElement> ViewAsync(string methodName, object args, string targetContractId = null)
    {
        using var response = await InvokeAsync("view", new { methodName, args, targetContractId = targetContractId ?? _contractId });
        return response.RootElement.Clone();
    }

    public async Task<string> GetTransactionStatusAsync(string transactionHash, string signerAccountId = null,
        CancellationToken cancellationToken = default)
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
        return ClassifyTransactionOutcome(outcome);
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

    private async Task<JsonDocument> InvokeAsync(string operation, object args = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the official NEAR SDK bridge.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request));
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        try{await process.WaitForExitAsync(cancellationToken);}
        catch(OperationCanceledException)
        {
            // Only status observations supply cancellation. Never cancel/replay a submitted mutation.
            if(!process.HasExited)process.Kill(entireProcessTree:true);
            await process.WaitForExitAsync();
            await Task.WhenAll(stdoutTask,stderrTask);
            throw;
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException($"NEAR SDK bridge returned no response (exit {process.ExitCode}): {stderr}");
        using var envelope = JsonDocument.Parse(stdout);
        if (process.ExitCode != 0 || !envelope.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var error = envelope.RootElement.TryGetProperty("error", out var value) ? value.GetString() : stderr;
            if(operation=="putRecordChecked"&&envelope.RootElement.TryGetProperty("checkedWritePhase",out var phase)&&phase.ValueKind==JsonValueKind.String&&phase.GetString()=="before-submission")
                throw new NearRecordAdmissionException($"NEAR checked-write admission failed: {error}");
            throw new InvalidOperationException($"NEAR SDK operation failed: {error}");
        }
        return JsonDocument.Parse(envelope.RootElement.GetProperty("result").GetRawText());
    }
}

internal sealed record NearKeyPair(string PublicKey, string PrivateKey, string ImplicitAccountId);
