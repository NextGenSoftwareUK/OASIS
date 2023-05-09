using System;
using Microsoft.Azure.Documents;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public interface IDocumentCollectionContext<in T> where T : IHolonBase
    {
        public string CollectionName { get; }
        public Guid GenerateId(T entity);
        public PartitionKey ResolvePartitionKey(string entityId);
    }
}
