using System;
using Microsoft.Azure.Documents;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class AvatarRepository : CosmosDbRepository<IAvatar> , IAvatarRepository
    {
        public AvatarRepository(ICosmosDbClientFactory factory) : base(factory) { }

        public override string CollectionName { get; } = "avatarItems";
        //public override string GenerateId(Avatar entity) => $"{Guid.NewGuid()}";
        public override Guid GenerateId(IAvatar entity) => Guid.NewGuid();
        //public override PartitionKey ResolvePartitionKey(string entityId) => new PartitionKey(entityId.Split('-')[0]);
        public override PartitionKey ResolvePartitionKey(string entityId) => new PartitionKey(entityId);
    }
}
