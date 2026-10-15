using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public sealed class CosmosMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string DocumentId {get;}
    public bool Acknowledged {get;}
    public string OriginalTransactionId => string.Empty;
    public CosmosMutationReconciliationException(string documentId,Exception cause,bool acknowledged=false)
        :base("Cosmos DB mutation outcome requires reconciliation before replay.",cause)
    {DocumentId=documentId;Acknowledged=acknowledged;}
}
