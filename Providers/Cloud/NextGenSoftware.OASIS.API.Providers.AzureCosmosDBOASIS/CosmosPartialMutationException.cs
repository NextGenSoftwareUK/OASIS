using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public sealed class CosmosPartialMutationException : Exception, INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> AcknowledgedRecordIds {get;}
    public string OriginalTransactionId => string.Empty;
    public CosmosPartialMutationException(IEnumerable<Guid> acknowledged,Exception cause)
        :base("Cosmos DB graph partially acknowledged; reconcile before replay.",cause)
        => AcknowledgedRecordIds=Array.AsReadOnly(acknowledged.ToArray());
}
