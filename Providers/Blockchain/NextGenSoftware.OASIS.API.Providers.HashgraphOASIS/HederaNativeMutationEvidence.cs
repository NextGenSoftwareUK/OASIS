using System;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

/// <summary>Original operation identities and SDK error evidence, not an inferred consensus or fee outcome.</summary>
public sealed record HederaNativeMutationEvidence(string OriginalTransactionId, string ApprovalTransactionId,
    string MutationTransactionId, string Stage, string NativeStatus);

public sealed class HederaNativeMutationReconciliationException : InvalidOperationException, INativeMutationReconciliationRequired
{
    public HederaNativeMutationEvidence Evidence { get; }
    public string OriginalTransactionId => Evidence.OriginalTransactionId;
    internal HederaNativeMutationReconciliationException(string message, HederaNativeMutationEvidence evidence,
        Exception innerException = null) : base(message, innerException) => Evidence = evidence;
}

public sealed class HederaNativeMutationCanceledException : OperationCanceledException, INativeMutationReconciliationRequired
{
    public HederaNativeMutationEvidence Evidence { get; }
    public string OriginalTransactionId => Evidence.OriginalTransactionId;
    internal HederaNativeMutationCanceledException(string message, HederaNativeMutationEvidence evidence,
        Exception innerException, CancellationToken token) : base(message, innerException, token) => Evidence = evidence;
}

public sealed class HederaNativeMutationTimeoutException : TimeoutException, INativeMutationReconciliationRequired
{
    public HederaNativeMutationEvidence Evidence { get; }
    public string OriginalTransactionId => Evidence.OriginalTransactionId;
    internal HederaNativeMutationTimeoutException(string message, HederaNativeMutationEvidence evidence,
        Exception innerException) : base(message, innerException) => Evidence = evidence;
}
