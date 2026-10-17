using System;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class AvatarDetailRepository : CosmosDbRepository<IAvatarDetail>, IAvatarDetailRepository
    {
        public AvatarDetailRepository(ICosmosDbClientFactory factory) : base(factory) { }

        public override string CollectionName { get; } = "avatarDetailItems";
        public override Guid GenerateId(IAvatarDetail entity) => Guid.NewGuid();
        public override PartitionKey? ResolvePartitionKey(string entityId) => new PartitionKey(entityId);
    }
}
