using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public sealed class CosmosMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string DocumentId {get;}
    public string OriginalTransactionId => string.Empty;
    public CosmosMutationReconciliationException(string documentId,Exception cause)
        :base("Cosmos DB mutation outcome requires reconciliation before replay.",cause) => DocumentId=documentId;
}
