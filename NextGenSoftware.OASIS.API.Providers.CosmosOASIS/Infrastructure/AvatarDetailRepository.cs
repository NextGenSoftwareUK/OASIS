using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entites;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class AvatarDetailRepository : CosmosDbRepository<AvatarDetail>, IAvatarDetailRepository
    {
        public AvatarDetailRepository(ICosmosDbClientFactory factory) : base(factory) { }

        public override string CollectionName { get; } = "avatarDetailItems";
        //public override string GenerateId(Avatar entity) => $"{Guid.NewGuid()}";
        public override Guid GenerateId(AvatarDetail entity) => Guid.NewGuid();
        //public override PartitionKey ResolvePartitionKey(string entityId) => new PartitionKey(entityId.Split('-')[0]);
        public override PartitionKey ResolvePartitionKey(string entityId) => new PartitionKey(entityId);
    }
}
