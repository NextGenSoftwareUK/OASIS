using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Model;

namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Context
{
    internal class AvatarContext
    {
        private readonly IMongoDatabase _database = null;

        public AvatarContext(IOptions<Settings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            if (client != null)
                _database = client.GetDatabase(settings.Value.Database);
        }

        public IMongoCollection<Avatar> Notes
        {
            get
            {
                return _database.GetCollection<Avatar>("Avatar");
            }
        }
    }
}