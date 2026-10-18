using System;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

/// <summary>Original SDK execution evidence; neither a replacement submission nor a fee-settlement result.</summary>
public sealed class SuiNativeMutationReconciliationException : InvalidOperationException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public bool? ExecutionSucceeded { get; }

    internal SuiNativeMutationReconciliationException(string message, string originalTransactionId,
        bool? executionSucceeded, Exception innerException = null) : base(message, innerException)
    {
        OriginalTransactionId = originalTransactionId;
        ExecutionSucceeded = executionSucceeded;
    }
}

public sealed class SuiNativeMutationCanceledException : OperationCanceledException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    internal SuiNativeMutationCanceledException(string message, Exception innerException, CancellationToken token, string originalTransactionId = null)
        : base(message, innerException, token) { OriginalTransactionId = originalTransactionId; }
}

public sealed class SuiNativeMutationTimeoutException : TimeoutException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    internal SuiNativeMutationTimeoutException(string message, Exception innerException, string originalTransactionId = null) : base(message, innerException) { OriginalTransactionId = originalTransactionId; }
}
