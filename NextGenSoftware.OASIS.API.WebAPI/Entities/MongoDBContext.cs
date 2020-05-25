using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _mongoDb;
        public MongoDbContext()
        {
            //MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/test?retryWrites=true&w=majority");
            //_mongoDb = mongoClient.GetDatabase("OASISAPI");

            MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:mz0u0VKsg0Hi6JOT@beb-wfqsj.mongodb.net/test?authSource=admin&replicaSet=BEB-shard-0&w=majority&readPreference=primary&appname=MongoDB%20Compass&retryWrites=true&ssl=true");
            _mongoDb = mongoClient.GetDatabase("BEB");
        }
        public IMongoCollection<User> User
        {
            get
            {
                return _mongoDb.GetCollection<User>("User");
            }
        }

        public IMongoCollection<Sequence> Sequence
        {
            get
            {
                return _mongoDb.GetCollection<Sequence>("Sequence");
            }
        }
    }
}
