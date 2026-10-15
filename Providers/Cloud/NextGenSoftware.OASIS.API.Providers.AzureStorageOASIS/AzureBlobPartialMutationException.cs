using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public sealed class AzureBlobPartialMutationException : Exception, INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> AcknowledgedRecordIds {get;}
    public string OriginalTransactionId => string.Empty;
    public AzureBlobPartialMutationException(IEnumerable<Guid> acknowledged,Exception cause)
        :base("Azure batch partially acknowledged; reconcile before replay.",cause)
        => AcknowledgedRecordIds=Array.AsReadOnly(acknowledged.ToArray());
}
