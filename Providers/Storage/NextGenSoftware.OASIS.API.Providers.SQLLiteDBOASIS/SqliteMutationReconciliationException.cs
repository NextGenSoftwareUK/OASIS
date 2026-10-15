using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

/// <summary>Persistence was entered; inspect the original record before replaying on any provider.</summary>
public sealed class SqliteMutationReconciliationException : Exception,INativeMutationReconciliationRequired
{
    public Guid RecordId {get;}
    public string OriginalTransactionId=>string.Empty; // SQLite exposes no original transaction identity here; never invent one.
    public SqliteMutationReconciliationException(Guid recordId,Exception cause)
        :base("SQLite detail persistence outcome requires reconciliation before replay.",cause) {RecordId=recordId;}
}
