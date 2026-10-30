using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS;

internal sealed class PocketBaseMutationReconciliationException(string key, string operation, Exception cause)
    : Exception("PocketBase write outcome requires reconciliation of the proposed native record before replay.", cause), INativeMutationReconciliationRequired
{
    public string Key { get; } = key;
    public string Operation { get; } = operation;
    public string OriginalTransactionId => string.Empty;
}
