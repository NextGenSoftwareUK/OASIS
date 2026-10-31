using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public sealed class DynamoDBPartialMutationException : Exception, INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> AcknowledgedRecordIds {get;}
    public string OriginalTransactionId => string.Empty; // Ordinary item acknowledgements do not supply transaction hashes.
    public DynamoDBPartialMutationException(IEnumerable<Guid> acknowledged,Exception cause)
        :base("DynamoDB batch partially acknowledged; reconcile before replay.",cause)
        => AcknowledgedRecordIds=Array.AsReadOnly(acknowledged.ToArray());
}
