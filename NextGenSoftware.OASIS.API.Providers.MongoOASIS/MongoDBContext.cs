using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using static NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _mongoDb;
        public MongoDbContext(string connectionString, string dbName)
        {
            //MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/test?retryWrites=true&w=majority");
            MongoClient mongoClient = new MongoClient(connectionString);
            _mongoDb = mongoClient.GetDatabase(dbName);
            //_mongoDb = mongoClient.GetDatabase("OASISAPI");
        }

        public IMongoCollection<Avatar> Avatar
        {
            get
            {
                return _mongoDb.GetCollection<Avatar>("Avatar");
            }
        }

        public IMongoCollection<SearchData> SearchData
        {
            get
            {
                return _mongoDb.GetCollection<SearchData>("SearchData");
            }
        }
    }
}
