using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider,
    NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader
{
    string NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader.NativeFeeProvider=>"CosmosBlockChainOASIS";
    string NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader.NativeFeeChainId=>_sdkBackend.ChainId;
    NativeFeeCoverage NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader.DeclaredFeeCoverage=>CurrentFeeCoverage;
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<PreparedNativeFeeEvidence>> ReadPreparedNativeFeeEvidenceAsync(UsageProviderAttempt original,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<PreparedNativeFeeEvidence>();
        try{
            var coverage=CurrentFeeCoverage;
            var verified=await ReadOriginalSignedFeeAsync(original,token);
            var fee=verified.Receipt;
            if(fee.IsError)return new(){IsError=true,Message=fee.Message,Exception=fee.Exception,ErrorCode=fee.ErrorCode};
            var basis="observed-ante-fee-deduction";
            NativeFeeAuditSnapshot audit=null;
            if(_reviewedDeployment!=null){
                if(!verified.Identity.TryGetProperty("hasSignedTip",out var tip)||tip.ValueKind!=JsonValueKind.False)
                    throw new InvalidDataException("The reviewed no-post-handler Cosmos fee model requires independently verified absence of a signed tip.");
                var observation=await _reviewedDeployment.ValidateAsync(fee.Result.GetProperty("height").GetInt64(),token);
                audit=_reviewedDeployment.CaptureAudit(observation,fee.Result.GetProperty("transactionHash").GetString(),fee.Result.GetProperty("height").GetInt64(),
                    fee.Result.GetProperty("success").GetBoolean(),fee.Result.GetProperty("executionCode").GetUInt32());
                basis=_reviewedDeployment.Review.EvidenceBasis;
            }
            var components=new System.Collections.Generic.List<NativeFeeComponent>();
            foreach(var coin in fee.Result.GetProperty("fees").EnumerateArray())components.Add(new(coin.GetProperty("denom").GetString(),coin.GetProperty("amount").GetString()));
            result.Result=new("CosmosBlockChainOASIS",fee.Result.GetProperty("chainId").GetString(),fee.Result.GetProperty("transactionHash").GetString(),
                fee.Result.GetProperty("payer").GetString(),fee.Result.GetProperty("success").GetBoolean(),coverage,
                components.ToArray(),basis,coverage==NativeFeeCoverage.NetFinal&&components.Count==0){Audit=audit};
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
    /// <summary>Read only: verify persisted original signed identity before native fee recovery lookup.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<JsonElement>();
        try{
            return (await ReadOriginalSignedFeeAsync(attempt,token)).Receipt;
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }

    private async Task<(NextGenSoftware.OASIS.Common.OASISResult<JsonElement> Receipt,JsonElement Identity)> ReadOriginalSignedFeeAsync(
        UsageProviderAttempt attempt,CancellationToken token)
    {
            var preparation=NativePreparedAttemptSnapshot.Capture(attempt,"CosmosBlockChainOASIS",_sdkBackend.ChainId);
            var identity=await VerifyOriginalSignedIdentityAsync(JsonSerializer.SerializeToElement(new{
                transactionHash=preparation.ProviderRequestId,chainId=preparation.ChainId,sender=preparation.Sender,
                signedTransactionBase64=preparation.SignedTransactionBase64,signDocBase64=preparation.SignDocBase64}),token);
            var receipt=await GetNativeFeeReceiptAsync(preparation.ProviderRequestId,token);
            if(receipt.IsError)return (receipt,identity);
            if(!identity.TryGetProperty("feePayer",out var payer)||payer.ValueKind!=JsonValueKind.String
                ||string.IsNullOrWhiteSpace(payer.GetString())||payer.GetString()!=receipt.Result.GetProperty("payer").GetString())
                throw new InvalidDataException("Cosmos fee receipt payer does not match the original signed fee authority.");
            return (receipt,identity);
    }
    /// <summary>Bind all storage writes in this admitted operation to the existing durable usage owner.
    /// Each native write has its own original signed identity; this does not settle fees or resolve prior attempts.</summary>
    public async Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation, UsageExecutionHandle execution,
        string clientRequestId, string expectedSender, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(execution);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        if (string.IsNullOrWhiteSpace(expectedSender)) throw new ArgumentException("Expected storage signer required.", nameof(expectedSender));
        return await _sdkBackend.WithStoragePreparationAndCancellationAsync(operation, (_, _) => {
            token.ThrowIfCancellationRequested();
            return CreateNativePreparationServer(execution, correlation.Next(),
                _sdkBackend.ChainId, expectedSender);
        },token);
    }

    /// <summary>Explicit prepared native invocation; caller supplies admitted execution and trusted chain/signer context.</summary>
    public async Task<JsonElement> ExecutePreparedNativeAsync(string operation, object args, UsageExecutionHandle execution,
        string clientRequestId, string expectedChainId, string expectedSender, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var server = CreateNativePreparationServer(execution, clientRequestId, expectedChainId, expectedSender);
        return await _sdkBackend.InvokePreparedAsync(operation, args, server, token);
    }

    public PreparedSubmissionServer CreateNativePreparationServer(UsageExecutionHandle execution,
        string clientRequestId, string expectedChainId, string expectedSender)
    {
        if (execution == null) throw new ArgumentNullException(nameof(execution));
        if(execution.RequiresVerifiedNativeFeeMaximums){
            if(!_sdkBackend.HasReviewedStandardSdkFeeModel||expectedChainId!=_sdkBackend.ChainId)
                throw new UsageProtocolException("COSMOS_TOTAL_FEE_MODEL_REQUIRED","Aggregate admission requires a reviewed standard SDK fee model bound to this exact Cosmos chain.",400);
            return new PreparedSubmissionServer(execution.NativePreparationPersistenceWithVerifiedFeeMaximums(
                "CosmosBlockChainOASIS",null,clientRequestId,expectedChainId,expectedSender,VerifyOriginalSignedFeeMaximumsAsync));
        }
        return new PreparedSubmissionServer(execution.NativePreparationPersistence(
            "CosmosBlockChainOASIS", null, clientRequestId, expectedChainId, expectedSender, VerifyOriginalSignedPreparationAsync));
    }

    /// <summary>Official offline signed identity verification; no ledger submission or fee permission.</summary>
    public async Task VerifyOriginalSignedPreparationAsync(JsonElement preparation, CancellationToken token = default)
        =>_ = await VerifyOriginalSignedIdentityAsync(preparation,token);

    private async Task<NativeFeeMaximum[]> VerifyOriginalSignedFeeMaximumsAsync(JsonElement preparation,CancellationToken token)
    {
        var identity=await VerifyOriginalSignedIdentityAsync(preparation,token);
        var maxima=new System.Collections.Generic.List<NativeFeeMaximum>();
        foreach(var coin in identity.GetProperty("declaredFeeAndTip").EnumerateArray())
            maxima.Add(new(coin.GetProperty("denom").GetString(),coin.GetProperty("amount").GetString(),NativeFeeMaximumCoverage.AllFees));
        return maxima.ToArray();
    }

    private async Task<JsonElement> VerifyOriginalSignedIdentityAsync(JsonElement preparation,CancellationToken token)
    {
        var result = await _sdkBackend.InvokeAsync("verifyNativePreparation", new { preparation }, token);
        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("verified", out var verified)
            || verified.ValueKind != JsonValueKind.True
            || !result.TryGetProperty("transactionHash", out var hash) || hash.ValueKind != JsonValueKind.String
            || hash.GetString() != preparation.GetProperty("transactionHash").GetString()
            || !result.TryGetProperty("sender", out var sender) || sender.ValueKind != JsonValueKind.String
            || sender.GetString() != preparation.GetProperty("sender").GetString())
            throw new InvalidDataException("Official Cosmos preparation verification did not bind the original digest and signer.");
        return result;
    }
}
