using System;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class HolonRepository : CosmosDbRepository<IHolon>, IHolonRepository
    {
        public HolonRepository(ICosmosDbClientFactory factory) : base(factory) { }

        public override string CollectionName { get; } = "holonItems";
        protected override Type EntityType => typeof(Holon);
        public override Guid GenerateId(IHolon entity) => Guid.NewGuid();
        public override PartitionKey? ResolvePartitionKey(string entityId) => new PartitionKey(entityId.Split(':')[0]);
    }
}
