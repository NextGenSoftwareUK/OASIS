using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

/// <summary>Persistence was entered; inspect the original record before replaying on any provider.</summary>
public sealed class SqliteMutationReconciliationException : Exception,INativeMutationReconciliationRequired
{
    public Guid RecordId {get;}
    public IReadOnlyList<Guid> RecordIds {get;}
    public string OriginalTransactionId=>string.Empty; // SQLite exposes no original transaction identity here; never invent one.
    public SqliteMutationReconciliationException(Guid recordId,Exception cause)
        :this(new[] {recordId},cause) {}
    public SqliteMutationReconciliationException(IEnumerable<Guid> recordIds,Exception cause)
        :base("SQLite persistence outcome requires reconciliation before replay.",cause)
    {
        RecordIds=Array.AsReadOnly(recordIds.Distinct().ToArray());
        RecordId=RecordIds.FirstOrDefault();
    }
}
