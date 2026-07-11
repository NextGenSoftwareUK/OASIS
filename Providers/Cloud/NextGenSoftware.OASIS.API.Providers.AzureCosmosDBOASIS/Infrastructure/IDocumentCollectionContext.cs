using System;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public interface IDocumentCollectionContext<in T> where T : IHolonBase
    {
        string CollectionName { get; }
        Guid GenerateId(T entity);
        PartitionKey? ResolvePartitionKey(string entityId);
    }
}
