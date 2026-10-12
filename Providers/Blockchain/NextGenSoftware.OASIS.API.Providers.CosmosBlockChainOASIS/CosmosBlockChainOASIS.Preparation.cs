using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
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
