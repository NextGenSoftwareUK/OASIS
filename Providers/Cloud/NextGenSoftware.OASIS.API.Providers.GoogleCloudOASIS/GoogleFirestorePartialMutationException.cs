using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public sealed class GoogleFirestorePartialMutationException : Exception, INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> AcknowledgedRecordIds {get;}
    public string OriginalTransactionId => string.Empty;
    public GoogleFirestorePartialMutationException(IEnumerable<Guid> acknowledged,Exception cause)
        :base("Firestore batch partially acknowledged; reconcile before replay.",cause)
        => AcknowledgedRecordIds=Array.AsReadOnly(acknowledged.ToArray());
}
