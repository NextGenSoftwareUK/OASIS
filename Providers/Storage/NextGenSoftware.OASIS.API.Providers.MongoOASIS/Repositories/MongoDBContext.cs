using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class MongoDbContext
    {
        // MongoClient must be a singleton — creating multiple instances causes connection pool exhaustion
        private static readonly Dictionary<string, MongoClient> _clients = new();
        private static readonly object _lock = new();

        public MongoClient MongoClient { get; set; }
        public IMongoDatabase MongoDB { get; set; }

        public MongoDbContext(string connectionString, string dbName)
        {
            lock (_lock)
            {
                if (!_clients.TryGetValue(connectionString, out var client))
                {
                    var settings = MongoClientSettings.FromConnectionString(connectionString);
                    settings.MaxConnectionPoolSize = 30;
                    client = new MongoClient(settings);
                    _clients[connectionString] = client;
                }
                MongoClient = client;
            }
            MongoDB = MongoClient.GetDatabase(dbName);
        }

        public IMongoCollection<AvatarDetail> AvatarDetail => MongoDB.GetCollection<AvatarDetail>("AvatarDetail");
       // public IMongoCollection<AvatarThumbnail> AvatarThumbnail => MongoDB.GetCollection<AvatarThumbnail>("Avatar");
        public IMongoCollection<Avatar> Avatar => MongoDB.GetCollection<Avatar>("Avatar");
        //public IMongoCollection<IHolon> Holon => MongoDB.GetCollection<IHolon>("Holon");
        public IMongoCollection<Holon> Holon => MongoDB.GetCollection<Holon>("Holon");
        public IMongoCollection<SearchData> SearchData => MongoDB.GetCollection<SearchData>("SearchData");
    }
}