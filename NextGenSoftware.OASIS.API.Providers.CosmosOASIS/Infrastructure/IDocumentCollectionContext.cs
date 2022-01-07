using Microsoft.Azure.Documents;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entites;
using System;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public interface IDocumentCollectionContext<in T> where T : Entity
    {
        string CollectionName { get; }

        Guid GenerateId(T entity);

        PartitionKey ResolvePartitionKey(string entityId);
    }
}
