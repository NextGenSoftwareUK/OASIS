using System;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

/// <summary>Original CosmJS submission evidence, not an inferred retry or settled native fee.</summary>
public sealed class CosmosNativeMutationReconciliationException : InvalidOperationException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public bool? ExecutionSucceeded { get; }
    internal CosmosNativeMutationReconciliationException(string message, string transactionId, bool? executionSucceeded,
        Exception innerException = null) : base(message, innerException)
    {
        OriginalTransactionId = transactionId;
        ExecutionSucceeded = executionSucceeded;
    }
}

public sealed class CosmosNativeMutationCanceledException : OperationCanceledException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId => null;
    internal CosmosNativeMutationCanceledException(string message, Exception cause, CancellationToken token) : base(message, cause, token) { }
}

public sealed class CosmosNativeMutationTimeoutException : TimeoutException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId => null;
    internal CosmosNativeMutationTimeoutException(string message, Exception cause) : base(message, cause) { }
}
