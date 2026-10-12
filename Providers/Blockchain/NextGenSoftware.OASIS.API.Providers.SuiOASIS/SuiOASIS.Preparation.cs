using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public partial class SuiOASIS
{
    /// <summary>Explicit admitted native invocation; caller must supply trusted operation/chain/signer context.</summary>
    public async Task<JsonElement> ExecutePreparedNativeAsync(string operation, object args, UsageExecutionHandle execution,
        string clientRequestId, string expectedChainId, string expectedSender, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var server = CreateNativePreparationServer(execution, clientRequestId, expectedChainId, expectedSender);
        return await ((SuiStorageBackend)Backend).Client.InvokePreparedAsync(operation, args, server, token);
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
    {
        var digest = preparation.GetProperty("transactionHash").GetString();
        var sender = preparation.GetProperty("sender").GetString();
        var result = await InvokeSdkAsync("verifyNativePreparation", new { preparation }, token);
        if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty("verified", out var verified)
            || verified.ValueKind != JsonValueKind.True
            || !result.TryGetProperty("transactionHash", out var hash) || hash.ValueKind != JsonValueKind.String || hash.GetString() != digest
            || !result.TryGetProperty("sender", out var identity) || identity.ValueKind != JsonValueKind.String || identity.GetString() != sender)
            throw new InvalidDataException("Official Sui preparation verification did not bind the original digest and signer.");
    }
}
