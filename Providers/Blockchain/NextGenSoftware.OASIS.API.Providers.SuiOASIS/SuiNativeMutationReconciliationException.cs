using System;
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
