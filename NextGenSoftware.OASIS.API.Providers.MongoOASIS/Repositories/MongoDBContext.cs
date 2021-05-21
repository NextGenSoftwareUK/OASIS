using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class MongoDbContext
    {
        public MongoClient MongoClient { get; set; }
        public IMongoDatabase MongoDB { get; set; }

        public MongoDbContext(string connectionString, string dbName)
        {
            //MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/test?retryWrites=true&w=majority");
            MongoClient = new MongoClient(connectionString);
            MongoDB = MongoClient.GetDatabase(dbName);
            //_mongoDb = mongoClient.GetDatabase("OASISAPI");
        }

        public IMongoCollection<Avatar> Avatar
        {
            get
            {
                return MongoDB.GetCollection<Avatar>("Avatar");
            }
        }

        public IMongoCollection<Holon> Holon
        {
            get
            {
                return MongoDB.GetCollection<Holon>("Holon");
            }
        }

        public IMongoCollection<SearchData> SearchData
        {
            get
            {
                return MongoDB.GetCollection<SearchData>("SearchData");
            }
        }
    }
}
