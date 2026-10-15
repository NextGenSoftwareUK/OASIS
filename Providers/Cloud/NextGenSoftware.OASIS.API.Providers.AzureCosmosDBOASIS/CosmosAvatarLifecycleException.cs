using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public sealed class CosmosAvatarLifecycleException : Exception, INativeMutationReconciliationRequired
{
    public Guid AvatarId {get;}
    public IReadOnlyList<string> AcknowledgedContainers {get;}
    public string OriginalTransactionId => string.Empty;
    public CosmosAvatarLifecycleException(Guid id,IEnumerable<string> acknowledged,Exception cause)
        :base("Cosmos avatar lifecycle partially acknowledged across containers; reconcile before replay.",cause)
    {AvatarId=id;AcknowledgedContainers=Array.AsReadOnly(acknowledged.ToArray());}
}
