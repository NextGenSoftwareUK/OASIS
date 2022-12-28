using Microsoft.Azure.Documents;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class HolonRepository : CosmosDbRepository<Holon>, IHolonRepository
    {
        public HolonRepository(ICosmosDbClientFactory factory) : base(factory) { }

        public override string CollectionName { get; } = "holonItems";
        //public override string GenerateId(Avatar entity) => $"{Guid.NewGuid()}";
        public override Guid GenerateId(Holon entity) => Guid.NewGuid();
        public override PartitionKey ResolvePartitionKey(string entityId) => new PartitionKey(entityId.Split(':')[0]);
    }
}
