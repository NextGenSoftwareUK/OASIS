using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider
{
    /// <summary>Read only: verify persisted original signed identity before native fee recovery lookup.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<JsonElement>();
        try{
            ArgumentNullException.ThrowIfNull(attempt);
            if(attempt.Provider!="CosmosBlockChainOASIS"||attempt.NativePreparation==null
                ||string.IsNullOrWhiteSpace(attempt.ProviderRequestId)||attempt.NativePreparation.ChainId!=_sdkBackend.ChainId)
                throw new InvalidDataException("Prepared Cosmos attempt must bind this provider, original hash and configured chain.");
            var preparation=attempt.NativePreparation;
            await VerifyOriginalSignedPreparationAsync(JsonSerializer.SerializeToElement(new{
                transactionHash=attempt.ProviderRequestId,chainId=preparation.ChainId,sender=preparation.Sender,
                signedTransactionBase64=preparation.SignedTransactionBase64,signDocBase64=preparation.SignDocBase64}),token);
            return await GetNativeFeeReceiptAsync(attempt.ProviderRequestId,token);
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
    /// <summary>Bind all storage writes in this admitted operation to the existing durable usage owner.
    /// Each native write has its own original signed identity; this does not settle fees or resolve prior attempts.</summary>
    public async Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation, UsageExecutionHandle execution,
        string clientRequestId, string expectedSender, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(execution);
        if (string.IsNullOrWhiteSpace(clientRequestId) || clientRequestId.Length > 128)
            throw new ArgumentException("Bounded storage client request identity required.", nameof(clientRequestId));
        if (string.IsNullOrWhiteSpace(expectedSender)) throw new ArgumentException("Expected storage signer required.", nameof(expectedSender));
        var sequence = 0;
        return await _sdkBackend.WithStoragePreparationAsync(operation, (_, _) => {
            token.ThrowIfCancellationRequested();
            return CreateNativePreparationServer(execution, clientRequestId + ".storage." + Interlocked.Increment(ref sequence),
                _sdkBackend.ChainId, expectedSender);
        });
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
        return new PreparedSubmissionServer(execution.NativePreparationPersistence(
            "CosmosBlockChainOASIS", null, clientRequestId, expectedChainId, expectedSender, VerifyOriginalSignedPreparationAsync));
    }

    /// <summary>Official offline signed identity verification; no ledger submission or fee permission.</summary>
    public async Task VerifyOriginalSignedPreparationAsync(JsonElement preparation, CancellationToken token = default)
    {
        var result = await _sdkBackend.InvokeAsync("verifyNativePreparation", new { preparation }, token);
        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("verified", out var verified)
            || verified.ValueKind != JsonValueKind.True
            || !result.TryGetProperty("transactionHash", out var hash) || hash.ValueKind != JsonValueKind.String
            || hash.GetString() != preparation.GetProperty("transactionHash").GetString()
            || !result.TryGetProperty("sender", out var sender) || sender.ValueKind != JsonValueKind.String
            || sender.GetString() != preparation.GetProperty("sender").GetString())
            throw new InvalidDataException("Official Cosmos preparation verification did not bind the original digest and signer.");
    }
}
