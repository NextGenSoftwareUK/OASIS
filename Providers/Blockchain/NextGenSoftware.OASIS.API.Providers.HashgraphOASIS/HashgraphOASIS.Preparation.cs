using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider
{
    /// <summary>Trusted host supplies original payer authority, allowed nodes and distinct query/fee caps.
    /// Each actual selected payment is independently verified and retained before transport acknowledgement.</summary>
    public PreparedSubmissionServer CreatePaidQueryPreparationServer(UsageExecutionHandle execution,string clientRequestId,
        string expectedSender,string expectedPublicKey,string[] expectedNodes,long maxPaymentTinybars,long maxTransactionFeeTinybars,int maximumRequests=64)
    {
        ArgumentNullException.ThrowIfNull(execution);ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
        if(string.IsNullOrWhiteSpace(clientRequestId)||clientRequestId.Length>118||maxPaymentTinybars<=0||maxTransactionFeeTinybars<=0
            ||expectedNodes==null||expectedNodes.Length==0||expectedNodes.Length>1024)
            throw new ArgumentException("Bounded correlation, positive separate payment/fee caps and explicit authorized query nodes are required.");
        var nodes=(string[])expectedNodes.Clone();
        if(System.Linq.Enumerable.Distinct(nodes,StringComparer.Ordinal).Count()!=nodes.Length
            ||System.Linq.Enumerable.Any(nodes,node=>!System.Text.RegularExpressions.Regex.IsMatch(node??"",@"\A[0-9]+\.[0-9]+\.[0-9]+\z")||node==expectedSender))
            throw new ArgumentException("Distinct numeric authorized query node identities are required.",nameof(expectedNodes));
        var cap=maxPaymentTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var feeCap=maxTransactionFeeTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var next=0;
        return PreparedSubmissionServer.ForBoundedRequests(async(evidence,token)=>{
            if(evidence.GetProperty("purpose").GetString()!="HederaQueryPayment"||evidence.GetProperty("chainId").GetString()!=_hederaNetwork
                ||evidence.GetProperty("sender").GetString()!=expectedSender)
                throw new InvalidDataException("Query payment must bind this admitted purpose, network and payer.");
            var identity=evidence.GetProperty("transactionHash").GetString();
            var node=evidence.GetProperty("queryNodeAccountId").GetString();var units=evidence.GetProperty("queryPaymentTinybars").GetString();
            var verified=await HederaSdkClient.InvokeAsync("verifyNativeQueryPreparation",new {preparation=evidence,expectedPublicKey,expectedNodes=nodes,
                maxPaymentTinybars=cap,maxTransactionFeeTinybars=feeCap},token);
            if(verified.GetProperty("verified").ValueKind!=JsonValueKind.True||verified.GetProperty("transactionHash").GetString()!=identity
                ||verified.GetProperty("sender").GetString()!=expectedSender||verified.GetProperty("queryNodeAccountId").GetString()!=node
                ||verified.GetProperty("queryPaymentTinybars").GetString()!=units)
                throw new InvalidDataException("Official SDK verification did not bind the original signed query payment.");
            var authority=new NativeSigningAuthority("hedera-query-payment-v1",Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new {
                PublicKey=expectedPublicKey,NodeAccountId=node,PaymentTinybars=units,MaxPaymentTinybars=cap,MaxTransactionFeeTinybars=feeCap})));
            var correlation=clientRequestId+".query."+Interlocked.Increment(ref next).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await execution.BeginPreparedNativeAttemptAsync("HashgraphOASIS",null,correlation,identity,new UsageNativePreparation {
                ChainId=_hederaNetwork,Sender=expectedSender,SignedTransactionBase64=evidence.GetProperty("signedTransactionBase64").GetString(),SigningAuthority=authority},token);
        },maximumRequests);
    }
    private static NativeSigningAuthority PayerAuthority(string publicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
        var authority=new NativeSigningAuthority("hedera-payer-public-key-v1",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(publicKey)));
        authority.Validate();return authority;
    }

    private static string OriginalPayerKey(NativeSigningAuthority authority)
    {
        if(authority==null||authority.Scheme!="hedera-payer-public-key-v1")
            throw new InvalidDataException("Original admitted Hedera payer authority is required; current account keys cannot replace it.");
        authority.Validate();
        return new System.Text.UTF8Encoding(false,true).GetString(Convert.FromBase64String(authority.EvidenceBase64));
    }

    /// <summary>Read-only recovery using the independently trusted key retained before original submission.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<JsonElement>();
        try {
            var original=NativePreparedAttemptSnapshot.Capture(attempt,"HashgraphOASIS",_hederaNetwork);
            return await GetPreparedNativeFeeReceiptAsync(attempt,OriginalPayerKey(original.SigningAuthority),token);
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
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
            if(preparation.SigningAuthority!=null&&OriginalPayerKey(preparation.SigningAuthority)!=expectedPublicKey)
                throw new InvalidDataException("Recovery key differs from the original admitted Hedera payer authority.");
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
            (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,expectedPublicKey,token),PayerAuthority(expectedPublicKey));
        var mutation=execution.NativePreparationPersistence("HashgraphOASIS",null,clientRequestId+":mutation",expectedNetwork,expectedSender,
            (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,expectedPublicKey,token),PayerAuthority(expectedPublicKey));
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
            (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,expectedPublicKey,token),PayerAuthority(expectedPublicKey)),expectedTransactionId);
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
