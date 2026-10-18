using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private readonly AsyncLocal<NativeTransferExecution> _nativeTransferExecution = new();

    /// <summary>Runs one ordinary native SendToken under an existing admitted execution owner.
    /// Does not create an admission, choose prices or settle billing. Nested/reused scopes are rejected.</summary>
    public async Task<T> ExecutePreparedNativeOperationAsync<T>(Func<Task<T>> operation, UsageExecutionHandle execution,
        string clientRequestId, string expectedSender, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        token.ThrowIfCancellationRequested();
        if (_nativeTransferExecution.Value != null) throw new InvalidOperationException("A Polkadot transfer owner scope cannot be nested.");
        var context = new NativeTransferExecution(execution, clientRequestId, expectedSender, token);
        _nativeTransferExecution.Value = context;
        try { return await operation().ConfigureAwait(false); }
        finally { context.Close(); _nativeTransferExecution.Value = null; }
    }

    private async Task<OASISResult<ITransactionResponse>> SendOwnedNativeTransferAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string submittedOriginal = null;
        try
        {
            var owner = _nativeTransferExecution.Value ?? throw new InvalidOperationException(
                "Polkadot SendToken requires ExecutePreparedNativeOperationAsync with an admitted execution owner; unsigned submission is not supported.");
            owner.Claim();
            owner.Token.ThrowIfCancellationRequested();
            var preparation = await PrepareNativeTokenTransferAsync(request, owner.Token).ConfigureAwait(false);
            if (preparation.IsError) throw preparation.Exception ?? new InvalidOperationException(preparation.Message);
            var original = preparation.Result;
            using var document = JsonDocument.Parse(Convert.FromBase64String(original.GetProperty("signDocBase64").GetString()));
            var signingBlock = document.RootElement.GetProperty("signingPayload").GetProperty("blockNumber").GetString();
            var cursor = Convert.ToUInt64(signingBlock.Substring(2), 16).ToString(CultureInfo.InvariantCulture);
            // Submission already joins its durable owner and marks any post-ACK uncertainty.
            var accepted = await SubmitNativeTokenTransferPreparationAsync(original, owner.Execution,
                owner.ClientRequestId, owner.ExpectedSender, owner.Token).ConfigureAwait(false);
            submittedOriginal = accepted.GetProperty("transactionHash").GetString();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(owner.Token);
            deadline.CancelAfter(TimeSpan.FromMinutes(2));
            while (true)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var observed = await ReadOriginalNativeOutcomeAsync(original, cursor, 16, deadline.Token).ConfigureAwait(false);
                if (observed.IsError) throw observed.Exception ?? new InvalidOperationException(observed.Message);
                var outcome = observed.Result;
                if (outcome.GetProperty("found").GetBoolean())
                {
                    var receipt = outcome.GetProperty("receipt");
                    if (!receipt.GetProperty("dispatchSucceeded").GetBoolean())
                        throw new PolkadotNativeMutationReconciliationException(submittedOriginal,
                            new InvalidOperationException("Finalized native transfer failed dispatch: " + receipt.GetProperty("dispatchError").GetString()));
                    result.Result.TransactionResult = submittedOriginal;
                    result.Message = "Native transfer finalized with successful dispatch and verified native fee evidence; billing settlement is separate.";
                    return result;
                }
                cursor = outcome.GetProperty("nextBlockNumber").GetString();
                // Poll only finalized reads. NEVER retry author_submitExtrinsic or generate new signed bytes.
                if (outcome.GetProperty("searchedThroughSnapshot").GetBoolean())
                    await Task.Delay(TimeSpan.FromSeconds(2), deadline.Token).ConfigureAwait(false);
            }
        }
        catch (Exception failure)
        {
            if (submittedOriginal != null && failure is not INativeMutationReconciliationRequired)
                failure = failure is OperationCanceledException cancelled
                    ? new PolkadotNativeMutationCanceledException(submittedOriginal, cancelled, cancelled.CancellationToken)
                    : new PolkadotNativeMutationReconciliationException(submittedOriginal, failure);
            OASISErrorHandling.HandleError(ref result, failure.Message, failure);
            return result;
        }
    }

    private sealed class NativeTransferExecution
    {
        private int _state;
        private readonly CancellationTokenSource _lifetime;
        public UsageExecutionHandle Execution { get; }
        public string ClientRequestId { get; }
        public string ExpectedSender { get; }
        public CancellationToken Token { get; }
        public NativeTransferExecution(UsageExecutionHandle execution, string clientRequestId, string expectedSender, CancellationToken token)
        {
            (Execution, ClientRequestId, ExpectedSender) = (execution, clientRequestId, expectedSender);
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            Token = _lifetime.Token;
        }
        public void Claim()
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                throw new InvalidOperationException("A native transfer owner scope can execute only one transfer and cannot outlive its operation.");
        }
        public void Close()
        {
            Interlocked.Exchange(ref _state, 2);
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
