using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>A submitted commit requires reconciliation before cross-provider replay.</summary>
public sealed class MongoTransactionReconciliationException : InvalidOperationException, INativeMutationReconciliationRequired
{
    public string SessionId { get; }
    public long TransactionNumber { get; }
    public string OriginalTransactionId => SessionId + ":" + TransactionNumber;

    public MongoTransactionReconciliationException(string sessionId, long transactionNumber, Exception original)
        : base("MongoDB transaction commit failed; reconcile the original transaction before replay.", original)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Original session identity is required.", nameof(sessionId));
        if (transactionNumber < 0) throw new ArgumentOutOfRangeException(nameof(transactionNumber));
        SessionId = sessionId;
        TransactionNumber = transactionNumber;
    }
}
