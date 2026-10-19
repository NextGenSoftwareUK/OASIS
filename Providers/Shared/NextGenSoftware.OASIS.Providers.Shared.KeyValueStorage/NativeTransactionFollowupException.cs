using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>A native receipt was acknowledged, but verification or a later step failed.
/// Reconcile that original transaction instead of repeating the mutation.</summary>
public sealed class NativeTransactionFollowupException:Exception,INativeMutationReconciliationRequired
{
    public ProviderType ProviderType {get;}
    public string OriginalTransactionId {get;}
    public NativeTransactionFollowupException(ProviderType provider,string transactionId,Exception cause)
        :base("An acknowledged native transaction requires reconciliation after a follow-up failure.",cause)
    {
        if(string.IsNullOrWhiteSpace(transactionId))throw new ArgumentException("An actual acknowledged transaction identity is required.",nameof(transactionId));
        ProviderType=provider;OriginalTransactionId=transactionId;
    }
}
