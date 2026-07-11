using System;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class AvatarRepository : CosmosDbRepository<IAvatar>, IAvatarRepository
    {
        public AvatarRepository(ICosmosDbClientFactory factory) : base(factory) { }

        public override string CollectionName { get; } = "avatarItems";
        public override Guid GenerateId(IAvatar entity) => Guid.NewGuid();
        public override PartitionKey? ResolvePartitionKey(string entityId) => new PartitionKey(entityId);
    }
}
