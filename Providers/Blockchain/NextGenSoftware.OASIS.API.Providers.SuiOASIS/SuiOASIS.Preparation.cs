using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider
{
    /// <summary>Own every physical put/delete, including encrypted storage index writes, in one admitted execution.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
        =>ExecutePreparedNativeOperationAsync(operation,execution,clientRequestId,expectedSender,token);

    /// <summary>Own every physical mutation in an existing public provider operation, including NFT/token multi-stage calls.</summary>
    public Task<T> ExecutePreparedNativeOperationAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        if(string.IsNullOrWhiteSpace(_sdkClient.ChainId))throw new InvalidOperationException("Explicit Sui genesis required for prepared storage.");
        return _sdkClient.WithStoragePreparationAndCancellationAsync(operation,()=>{
            token.ThrowIfCancellationRequested();
            return CreateNativePreparationServer(execution,correlation.Next(),_sdkClient.ChainId,expectedSender);
        },token);
    }
    /// <summary>Read-only original-attempt evidence for recovery; does not value, settle or resubmit it.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<JsonElement>();
        try{
            var preparation=NativePreparedAttemptSnapshot.Capture(attempt,"SuiOASIS",_sdkClient.ChainId);
            var identity=await VerifyOriginalSignedIdentityAsync(JsonSerializer.SerializeToElement(new{
                transactionHash=preparation.ProviderRequestId,chainId=preparation.ChainId,sender=preparation.Sender,
                signedTransactionBase64=preparation.SignedTransactionBase64,signature=preparation.Signature}),token);
            var receipt=await GetNativeFeeReceiptAsync(preparation.ProviderRequestId,token);
            if(receipt.IsError)return receipt;
            if(!identity.TryGetProperty("gasPayer",out var payer)||payer.ValueKind!=JsonValueKind.String
                ||string.IsNullOrWhiteSpace(payer.GetString())
                ||!SameSuiAddress(payer.GetString(),receipt.Result.GetProperty("payer").GetString()))
                throw new InvalidDataException("Sui fee receipt payer does not match the original signed gas owner.");
            return receipt;
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
    /// <summary>Explicit admitted native invocation; caller must supply trusted operation/chain/signer context.</summary>
    public async Task<JsonElement> ExecutePreparedNativeAsync(string operation, object args, UsageExecutionHandle execution,
        string clientRequestId, string expectedChainId, string expectedSender, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var server = CreateNativePreparationServer(execution, clientRequestId, expectedChainId, expectedSender);
        return await _sdkClient.InvokePreparedAsync(operation, args, server, token);
    }

    public PreparedSubmissionServer CreateNativePreparationServer(UsageExecutionHandle execution,
        string clientRequestId, string expectedChainId, string expectedSender)
    {
        if (execution == null) throw new ArgumentNullException(nameof(execution));
        return new PreparedSubmissionServer(execution.NativePreparationPersistence(
            "SuiOASIS", null, clientRequestId, expectedChainId, expectedSender, VerifyOriginalSignedPreparationAsync));
    }

    /// <summary>Fail-closed callback for original signed identity verification; performs no ledger submission.</summary>
    public async Task VerifyOriginalSignedPreparationAsync(JsonElement preparation, CancellationToken token = default)
        =>_ = await VerifyOriginalSignedIdentityAsync(preparation,token);

    private async Task<JsonElement> VerifyOriginalSignedIdentityAsync(JsonElement preparation,CancellationToken token)
    {
        var digest = preparation.GetProperty("transactionHash").GetString();
        var sender = preparation.GetProperty("sender").GetString();
        var result = await InvokeSdkAsync("verifyNativePreparation", new { preparation }, token);
        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("verified", out var verified)
            || verified.ValueKind != JsonValueKind.True
            || !result.TryGetProperty("transactionHash", out var hash) || hash.ValueKind != JsonValueKind.String || hash.GetString() != digest
            || !result.TryGetProperty("sender", out var identity) || identity.ValueKind != JsonValueKind.String || identity.GetString() != sender)
            throw new InvalidDataException("Official Sui preparation verification did not bind the original digest and signer.");
        return result;
    }
}
