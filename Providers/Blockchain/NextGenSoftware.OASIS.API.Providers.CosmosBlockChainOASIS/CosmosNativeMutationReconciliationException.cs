using System;
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
