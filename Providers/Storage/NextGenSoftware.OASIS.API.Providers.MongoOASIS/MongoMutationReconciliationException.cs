using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>A non-transactional Mongo mutation may have completed. Reconcile before replay.</summary>
public sealed class MongoMutationReconciliationException(Guid recordId,Exception cause)
    : InvalidOperationException("MongoDB mutation outcome requires reconciliation before replay.",cause),INativeMutationReconciliationRequired
{
    public Guid RecordId {get;}=recordId;
    public string OriginalTransactionId=>string.Empty; // No original server transaction identity is exposed here.
}
