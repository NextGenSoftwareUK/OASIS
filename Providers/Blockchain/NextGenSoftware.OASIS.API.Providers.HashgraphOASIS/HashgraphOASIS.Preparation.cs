using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider
{
    /// <summary>Own each physical encrypted-storage mutation; does not admit, bill or recover the execution.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        return ((ConfiguredHederaStorageBackend)Backend).WithPreparationAsync(operation,expectedSender,
            (network,sender,publicKey,id)=>{
                token.ThrowIfCancellationRequested();
                if(network!=_hederaNetwork||sender!=expectedSender)
                    throw new InvalidDataException("Prepared Hedera storage identity differs from admitted network/operator.");
                return CreateNativePreparationServer(execution,correlation.Next(),
                    network,sender,publicKey,id);
            },token);
    }

    /// <summary>Read-only original fee recovery. Public key must be independently authorized for the original payer.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,string expectedPublicKey,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<JsonElement>();
        try {
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
            var preparation=NativePreparedAttemptSnapshot.Capture(attempt,"HashgraphOASIS",_hederaNetwork);
            var identity=await VerifyOriginalSignedIdentityAsync(JsonSerializer.SerializeToElement(new {
                transactionHash=preparation.ProviderRequestId,chainId=preparation.ChainId,sender=preparation.Sender,
                signedTransactionBase64=preparation.SignedTransactionBase64 }),expectedPublicKey,token);
            if(!identity.TryGetProperty("signedTransactionHashes",out var signedHashes)||signedHashes.ValueKind!=JsonValueKind.Array
                ||signedHashes.GetArrayLength()==0||signedHashes.GetArrayLength()>1024)
                throw new InvalidDataException("Verified original Hedera native transaction hashes are required.");
            var allowed=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach(var hash in signedHashes.EnumerateArray()){
                if(hash.ValueKind!=JsonValueKind.String||!System.Text.RegularExpressions.Regex.IsMatch(hash.GetString()??"",@"\A[0-9a-f]{96}\z")||!allowed.Add(hash.GetString()!))
                    throw new InvalidDataException("Bounded unique original Hedera signed hashes are required.");
            }
            var fee=await GetNativeFeeReceiptAsync(preparation.ProviderRequestId,token);
            if(fee.IsError)return fee;
            foreach(var record in fee.Result.GetProperty("records").EnumerateArray())
                if(!allowed.Contains(record.GetProperty("transactionHash").GetString()!))
                    throw new InvalidDataException("Indexed Hedera fee record does not match an original SDK signed transaction hash.");
            return fee;
        } catch(Exception failure) { NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure); }
        return result;
    }

    /// <summary>Native preparation; caller supplies admission and independently trusted signer context.</summary>
    public async Task<JsonElement> ExecutePreparedNativeAsync(string action,object request,UsageExecutionHandle execution,
        string clientRequestId,string expectedNetwork,string expectedSender,string expectedPublicKey,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(execution==null)throw new ArgumentNullException(nameof(execution));
        using var document=JsonDocument.Parse(JsonSerializer.Serialize(request));
        var original=document.RootElement.GetProperty("transactionId").GetString();
        ArgumentException.ThrowIfNullOrWhiteSpace(original);
        using var server=action is "nftEscrowLock" or "tokenEscrowLock"
            ? CreateEscrowPreparationServer(execution,clientRequestId,expectedNetwork,expectedSender,expectedPublicKey,
                document.RootElement.GetProperty("approvalTransactionId").GetString(),original)
            : CreateNativePreparationServer(execution,clientRequestId,expectedNetwork,expectedSender,expectedPublicKey,original);
        return await HederaSdkClient.InvokePreparedAsync(action,request,server,token);
    }

    /// <summary>Optional approval and required mutation persist as distinct attempts owned by the same admitted execution.</summary>
    public PreparedSubmissionServer CreateEscrowPreparationServer(UsageExecutionHandle execution,string clientRequestId,
        string expectedNetwork,string expectedSender,string expectedPublicKey,string approvalId,string mutationId)
    {
        if(execution==null)throw new ArgumentNullException(nameof(execution));
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
        if(string.IsNullOrWhiteSpace(clientRequestId)||clientRequestId.Length>119)
            throw new ArgumentException("Escrow request correlation must leave space for bounded stage suffixes.",nameof(clientRequestId));
        var approval=execution.NativePreparationPersistence("HashgraphOASIS",null,clientRequestId+":approval",expectedNetwork,expectedSender,
            (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,expectedPublicKey,token));
        var mutation=execution.NativePreparationPersistence("HashgraphOASIS",null,clientRequestId+":mutation",expectedNetwork,expectedSender,
            (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,expectedPublicKey,token));
        return PreparedSubmissionServer.ForOptionalApproval((evidence,token)=>
            evidence.GetProperty("transactionHash").GetString()==approvalId?approval(evidence,token):mutation(evidence,token),approvalId,mutationId);
    }

    /// <summary>Caller must supply a public key independently authorised for the expected account.</summary>
    public PreparedSubmissionServer CreateNativePreparationServer(UsageExecutionHandle execution,
        string clientRequestId, string expectedNetwork, string expectedSender, string expectedPublicKey,string expectedTransactionId=null)
    {
        if(execution==null)throw new ArgumentNullException(nameof(execution));
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
        return new PreparedSubmissionServer(execution.NativePreparationPersistence("HashgraphOASIS",null,clientRequestId,expectedNetwork,expectedSender,
            (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,expectedPublicKey,token)),expectedTransactionId);
    }

    public async Task VerifyOriginalSignedPreparationAsync(JsonElement preparation,string expectedPublicKey,CancellationToken token=default)
        =>_=await VerifyOriginalSignedIdentityAsync(preparation,expectedPublicKey,token);

    private async Task<JsonElement> VerifyOriginalSignedIdentityAsync(JsonElement preparation,string expectedPublicKey,CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
        var result=await HederaSdkClient.InvokeAsync("verifyNativePreparation",new{preparation,expectedPublicKey},token);
        if(result.ValueKind!=JsonValueKind.Object||!result.TryGetProperty("verified",out var verified)||verified.ValueKind!=JsonValueKind.True
            ||!result.TryGetProperty("transactionHash",out var hash)||hash.ValueKind!=JsonValueKind.String||hash.GetString()!=preparation.GetProperty("transactionHash").GetString()
            ||!result.TryGetProperty("sender",out var sender)||sender.ValueKind!=JsonValueKind.String||sender.GetString()!=preparation.GetProperty("sender").GetString())
            throw new InvalidDataException("Official Hedera verification did not bind the original signed identity and payer.");
        return result;
    }
}
