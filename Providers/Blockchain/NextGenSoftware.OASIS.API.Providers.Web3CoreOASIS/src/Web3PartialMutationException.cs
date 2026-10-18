using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public sealed class Web3PartialMutationException : Exception, INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> AcknowledgedRecordIds {get;}
    public IReadOnlyList<string> AcknowledgedTransactionIds {get;}
    public string OriginalTransactionId {get;}
    public Web3PartialMutationException(IEnumerable<Guid> ids,IEnumerable<string> transactions,Exception cause)
        :base("Web3Core graph partially acknowledged; reconcile before replay.",cause)
    {
        AcknowledgedRecordIds=Array.AsReadOnly(ids.ToArray());AcknowledgedTransactionIds=Array.AsReadOnly(transactions.ToArray());
        OriginalTransactionId=cause is INativeMutationReconciliationRequired native ? native.OriginalTransactionId:string.Empty;
    }
}
