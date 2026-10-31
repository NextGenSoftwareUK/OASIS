using System;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

/// <summary>Preserves original identity after an acknowledged preparation may have been submitted.
/// This is not proof of chain execution, nor permission to replay the operation.</summary>
public sealed class PolkadotNativeMutationReconciliationException : InvalidOperationException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    internal PolkadotNativeMutationReconciliationException(string originalId, Exception cause)
        : base("Original Polkadot submission requires reconciliation before any replay.", cause)
        => OriginalTransactionId = originalId;
}

public sealed class PolkadotNativeMutationCanceledException : OperationCanceledException, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    internal PolkadotNativeMutationCanceledException(string originalId, Exception cause, CancellationToken token)
        : base("Original Polkadot submission was interrupted; reconcile before any replay.", cause, token)
        => OriginalTransactionId = originalId;
}
