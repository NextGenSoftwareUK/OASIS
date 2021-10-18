using Microsoft.Azure.Documents;
using NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Entites;

namespace NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Infrastructure
{
    public interface IDocumentCollectionContext<in T> where T : Entity
    {
        string CollectionName { get; }

        string GenerateId(T entity);

        PartitionKey ResolvePartitionKey(string entityId);
    }
}
