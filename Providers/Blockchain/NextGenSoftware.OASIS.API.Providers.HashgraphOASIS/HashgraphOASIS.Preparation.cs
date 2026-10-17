using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider,
    NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader
{
    string NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader.NativeFeeProvider=>"HashgraphOASIS";
    string NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader.NativeFeeChainId=>_hederaNetwork;
    NativeFeeCoverage NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader.DeclaredFeeCoverage=>NativeFeeCoverage.NetFinal;

    /// <summary>Verified indexed parent fees and original query-payment debit only; no query submission,
    /// current-key substitution, external RPC charge, principal transfer or fiat-price inference.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<PreparedNativeFeeEvidence>> ReadPreparedNativeFeeEvidenceAsync(
        UsageProviderAttempt original,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<PreparedNativeFeeEvidence>();
        try{
            var fee=await GetPreparedNativeFeeReceiptAsync(original,token);
            if(fee.IsError)return new(){IsError=true,Message=fee.Message,Exception=fee.Exception,ErrorCode=fee.ErrorCode};
            result.Result=new("HashgraphOASIS",fee.Result.GetProperty("network").GetString(),fee.Result.GetProperty("transactionId").GetString(),
                fee.Result.GetProperty("payer").GetString(),fee.Result.GetProperty("success").GetBoolean(),NativeFeeCoverage.NetFinal,
                new[]{new NativeFeeComponent("tinybar",fee.Result.GetProperty("netUnits").GetString())},
                fee.Result.GetProperty("basis").GetString());
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
    /// <summary>Trusted host supplies original payer authority, allowed nodes and distinct query/fee caps.
    /// Each actual selected payment is independently verified and retained before transport acknowledgement.</summary>
    public PreparedSubmissionServer CreatePaidQueryPreparationServer(UsageExecutionHandle execution,string clientRequestId,
        string expectedSender,string expectedPublicKey,string[] expectedNodes,long maxPaymentTinybars,long maxTransactionFeeTinybars,int maximumRequests=64)
    {
        ArgumentNullException.ThrowIfNull(execution);ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
        if(string.IsNullOrWhiteSpace(clientRequestId)||clientRequestId.Length>128||maxPaymentTinybars<=0||maxTransactionFeeTinybars<=0
            ||expectedNodes==null||expectedNodes.Length==0||expectedNodes.Length>1024)
            throw new ArgumentException("Bounded correlation, positive separate payment/fee caps and explicit authorized query nodes are required.");
        var nodes=(string[])expectedNodes.Clone();
        if(System.Linq.Enumerable.Distinct(nodes,StringComparer.Ordinal).Count()!=nodes.Length
            ||System.Linq.Enumerable.Any(nodes,node=>!System.Text.RegularExpressions.Regex.IsMatch(node??"",@"\A[0-9]+\.[0-9]+\.[0-9]+\z")||node==expectedSender))
            throw new ArgumentException("Distinct numeric authorized query node identities are required.",nameof(expectedNodes));
        var cap=maxPaymentTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var feeCap=maxTransactionFeeTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var next=0;
        // A complete128-character parent correlation still needs room for the physical query ordinal.
        var prefix="query."+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(clientRequestId))).ToLowerInvariant();
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
            var correlation=prefix+"."+Interlocked.Increment(ref next).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await execution.BeginPreparedNativeAttemptAsync("HashgraphOASIS",null,correlation,identity,new UsageNativePreparation {
                ChainId=_hederaNetwork,Sender=expectedSender,SignedTransactionBase64=evidence.GetProperty("signedTransactionBase64").GetString(),SigningAuthority=authority,
                // Original signed payer-to-node debit plus its signed CryptoTransfer fee ceiling.
                // Do not reuse this query-only bound for contract gas or other mutation types.
                FeeMaximums=execution.RequiresVerifiedNativeFeeMaximums?new[]{new NativeFeeMaximum("tinybar",
                    verified.GetProperty("maximumFeeTinybars").GetString(),NativeFeeMaximumCoverage.AllFees)}:null},token);
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
        var query=OriginalQueryContext(authority);
        if(query.HasValue)return query.Value.GetProperty("PublicKey").GetString();
        if(authority==null||authority.Scheme!="hedera-payer-public-key-v1")
            throw new InvalidDataException("Original admitted Hedera payer authority is required; current account keys cannot replace it.");
        authority.Validate();
        return new System.Text.UTF8Encoding(false,true).GetString(Convert.FromBase64String(authority.EvidenceBase64));
    }

    private static JsonElement? OriginalQueryContext(NativeSigningAuthority authority)
    {
        if(authority?.Scheme!="hedera-query-payment-v1")return null;
        authority.Validate();
        using var document=JsonDocument.Parse(Convert.FromBase64String(authority.EvidenceBase64));
        var context=document.RootElement;
        var names=new[]{"PublicKey","NodeAccountId","PaymentTinybars","MaxPaymentTinybars","MaxTransactionFeeTinybars"};
        if(context.ValueKind!=JsonValueKind.Object||context.EnumerateObject().Count()!=names.Length
            ||context.EnumerateObject().Select(item=>item.Name).Distinct(StringComparer.Ordinal).Count()!=names.Length
            ||names.Any(name=>!context.TryGetProperty(name,out var value)||value.ValueKind!=JsonValueKind.String||string.IsNullOrEmpty(value.GetString())))
            throw new InvalidDataException("Complete original admitted query-payment authority is required.");
        return context.Clone();
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
    /// <summary>Own every physical encrypted-storage invocation, including paid reads and mutations.
    /// Admission, pricing and settlement remain the host/Core responsibility.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        var backend=(ConfiguredHederaStorageBackend)Backend;
        var query=backend.QueryPreparation;
        return backend.WithInvocationAsync(operation,expectedSender,
            (action,request,publicKey,invocationToken)=>{
                token.ThrowIfCancellationRequested();
                return ExecutePreparedWithQueriesAsync(action,request,execution,correlation.Next(),expectedSender,publicKey,
                    query.AuthorizedNodeAccountIds,query.MaxPaymentTinybars,query.MaxTransactionFeeTinybars,
                    query.PersistenceTimeoutMs,query.MaximumRequestsPerInvocation,invocationToken);
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
            var query=OriginalQueryContext(preparation.SigningAuthority);
            var signingDocument=JsonSerializer.SerializeToElement(new {
                transactionHash=preparation.ProviderRequestId,chainId=preparation.ChainId,sender=preparation.Sender,
                signedTransactionBase64=preparation.SignedTransactionBase64,purpose=query.HasValue?"HederaQueryPayment":null,
                queryNodeAccountId=query?.GetProperty("NodeAccountId").GetString(),queryPaymentTinybars=query?.GetProperty("PaymentTinybars").GetString() });
            JsonElement identity;
            if(query.HasValue){
                identity=await HederaSdkClient.InvokeAsync("verifyNativeQueryPreparation",new {preparation=signingDocument,expectedPublicKey,
                    expectedNodes=new[]{query.Value.GetProperty("NodeAccountId").GetString()},
                    maxPaymentTinybars=query.Value.GetProperty("MaxPaymentTinybars").GetString(),
                    maxTransactionFeeTinybars=query.Value.GetProperty("MaxTransactionFeeTinybars").GetString()},token);
                if(identity.GetProperty("verified").ValueKind!=JsonValueKind.True||identity.GetProperty("transactionHash").GetString()!=preparation.ProviderRequestId
                    ||identity.GetProperty("sender").GetString()!=preparation.Sender
                    ||identity.GetProperty("queryPaymentTinybars").GetString()!=query.Value.GetProperty("PaymentTinybars").GetString()
                    ||identity.GetProperty("queryNodeAccountId").GetString()!=query.Value.GetProperty("NodeAccountId").GetString())
                    throw new InvalidDataException("Original signed query-payment authority is unverified.");
            }else identity=await VerifyOriginalSignedIdentityAsync(signingDocument,expectedPublicKey,token);
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
            if(query.HasValue){
                var principal=System.Numerics.BigInteger.Parse(query.Value.GetProperty("PaymentTinybars").GetString(),System.Globalization.CultureInfo.InvariantCulture);
                var net=System.Numerics.BigInteger.Zero;var paid=System.Numerics.BigInteger.Zero;
                foreach(var record in fee.Result.GetProperty("records").EnumerateArray()){
                    var charge=System.Numerics.BigInteger.Parse(record.GetProperty("chargedUnits").GetString(),System.Globalization.CultureInfo.InvariantCulture);
                    var payment=record.GetProperty("nativeStatus").GetString()=="SUCCESS"?principal:System.Numerics.BigInteger.Zero;
                    if(!record.TryGetProperty("payerNetUnits",out var observed)||observed.ValueKind!=JsonValueKind.String
                        ||!System.Numerics.BigInteger.TryParse(observed.GetString(),System.Globalization.NumberStyles.AllowLeadingSign,
                            System.Globalization.CultureInfo.InvariantCulture,out var payerNet)||payerNet!=-(charge+payment))
                        throw new InvalidDataException("Indexed query payer debit does not establish the exact original payment plus network fee.");
                    net-=payerNet;paid+=payment;
                }
                var mapped=JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string,JsonElement>>(fee.Result.GetRawText());
                mapped["networkFeeUnits"]=fee.Result.GetProperty("netUnits");
                mapped["queryPaymentUnits"]=JsonSerializer.SerializeToElement(paid.ToString(System.Globalization.CultureInfo.InvariantCulture));
                mapped["netUnits"]=JsonSerializer.SerializeToElement(net.ToString(System.Globalization.CultureInfo.InvariantCulture));
                mapped["basis"]=JsonSerializer.SerializeToElement("mirror-verified-original-query-payer-debit");
                mapped["coverage"]=JsonSerializer.SerializeToElement("indexed-query-payment-parent-records");
                fee.Result=JsonSerializer.SerializeToElement(mapped);
                fee.Message="Verified original query payment debit and indexed network fees; not fiat valuation or subscription settlement.";
            }
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
        var approval=OriginalPreparationPersistence(execution,clientRequestId+":approval",expectedNetwork,expectedSender,expectedPublicKey);
        var mutation=OriginalPreparationPersistence(execution,clientRequestId+":mutation",expectedNetwork,expectedSender,expectedPublicKey);
        return PreparedSubmissionServer.ForOptionalApproval((evidence,token)=>
            evidence.GetProperty("transactionHash").GetString()==approvalId?approval(evidence,token):mutation(evidence,token),approvalId,mutationId);
    }

    /// <summary>Caller must supply a public key independently authorised for the expected account.</summary>
    public PreparedSubmissionServer CreateNativePreparationServer(UsageExecutionHandle execution,
        string clientRequestId, string expectedNetwork, string expectedSender, string expectedPublicKey,string expectedTransactionId=null)
    {
        if(execution==null)throw new ArgumentNullException(nameof(execution));
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPublicKey);
        return new PreparedSubmissionServer(OriginalPreparationPersistence(execution,clientRequestId,expectedNetwork,expectedSender,expectedPublicKey),expectedTransactionId);
    }

    private Func<JsonElement,CancellationToken,Task> OriginalPreparationPersistence(UsageExecutionHandle execution,string correlation,
        string network,string sender,string publicKey)
        =>execution.RequiresVerifiedNativeFeeMaximums
            ?execution.NativePreparationPersistenceWithVerifiedFeeMaximums("HashgraphOASIS",null,correlation,network,sender,
                async(evidence,token)=>{
                    var identity=await VerifyOriginalSignedIdentityAsync(evidence,publicKey,token);
                    if(!identity.TryGetProperty("maximumFeeTinybars",out var maximum)||maximum.ValueKind!=JsonValueKind.String)
                        throw new UsageProtocolException("HEDERA_TOTAL_FEE_MAXIMUM_UNSUPPORTED","Original signed transaction type/shape has no reviewed complete network-fee maximum.",400);
                    return new[]{new NativeFeeMaximum("tinybar",maximum.GetString(),NativeFeeMaximumCoverage.AllFees)};
                },PayerAuthority(publicKey))
            :execution.NativePreparationPersistence("HashgraphOASIS",null,correlation,network,sender,
                (evidence,token)=>VerifyOriginalSignedPreparationAsync(evidence,publicKey,token),PayerAuthority(publicKey));

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
