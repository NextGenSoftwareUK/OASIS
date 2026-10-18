using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Persists and submits the exact original once under an admitted execution.
    /// Returns pool acceptance ONLY, never completed transfer success. Read finalized evidence before settlement.
    /// Repeating this call is not a recovery mechanism; reconcile the retained original instead.</summary>
    public async Task<JsonElement> SubmitNativeTokenTransferPreparationAsync(JsonElement preparation,
        UsageExecutionHandle execution,string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var original=preparation.Clone();
        using var server=CreateNativePreparationServer(execution,clientRequestId,expectedSender,original.GetProperty("transactionHash").GetString());
        return await InvokePolkadotSdkAsync(new{operation="submitNativePreparation",endpoint=_rpcEndpoint,
            genesisHash=ReportedGenesisHash,preparation=original},token,server);
    }

    /// <summary>Persist verified original preparation under an existing admitted execution. NEVER broadcasts.</summary>
    public async Task<JsonElement> PersistNativeTokenTransferPreparationAsync(JsonElement preparation,
        UsageExecutionHandle execution,string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var original=preparation.Clone();
        using var server=CreateNativePreparationServer(execution,clientRequestId,expectedSender,original.GetProperty("transactionHash").GetString());
        return await InvokePolkadotSdkAsync(new{operation="persistNativePreparation",endpoint=_rpcEndpoint,
            genesisHash=ReportedGenesisHash,preparation=original},token,server);
    }

    public PreparedSubmissionServer CreateNativePreparationServer(UsageExecutionHandle execution,string clientRequestId,string expectedSender,string expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if(!_isActivated||!IsGenesisHash(ReportedGenesisHash))throw new InvalidOperationException("Activate the verified network before durable preparation.");
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        if(!IsGenesisHash(expectedTransactionId))throw new ArgumentException("Exact original transaction hash is required.",nameof(expectedTransactionId));
        if(execution.RequiresVerifiedNativeFeeMaximums)
            throw new UsageProtocolException("POLKADOT_VERIFIED_FEE_MAXIMUM_REQUIRED",
                "Polkadot partialFee is an estimate, not a signed all-fees ceiling; aggregate sponsored admission is not enabled.",400);
        return new PreparedSubmissionServer(execution.NativePreparationPersistence("PolkadotOASIS",null,clientRequestId,
            ReportedGenesisHash,expectedSender,async(evidence,token)=>{
                token.ThrowIfCancellationRequested();
                var verified=await VerifyNativeTokenTransferPreparationAsync(evidence,token);
                token.ThrowIfCancellationRequested();
                if(verified.IsError)throw verified.Exception??new InvalidOperationException(verified.Message);
            }),expectedTransactionId);
    }
}
