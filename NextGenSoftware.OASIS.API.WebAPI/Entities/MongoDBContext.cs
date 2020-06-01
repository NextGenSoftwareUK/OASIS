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

        public IMongoCollection<Phase> Phase
        {
            get
            {
                return _mongoDb.GetCollection<Phase>("Phase");
            }
        }

        public IMongoCollection<Contract> Contract
        {
            get
            {
                return _mongoDb.GetCollection<Contract>("Contract");
            }
        }

        public IMongoCollection<Contact> Contact
        {
            get
            {
                return _mongoDb.GetCollection<Contact>("Contact");
            }
        }

        public IMongoCollection<Delivery> Delivery
        {
            get
            {
                return _mongoDb.GetCollection<Delivery>("Delivery");
            }
        }

        public IMongoCollection<DeliveryItem> DeliveryItem
        {
            get
            {
                return _mongoDb.GetCollection<DeliveryItem>("DeliveryItem");
            }
        }

        public IMongoCollection<Drawing> Drawing
        {
            get
            {
                return _mongoDb.GetCollection<Drawing>("Drawing");
            }
        }

        public IMongoCollection<File> File
        {
            get
            {
                return _mongoDb.GetCollection<File>("File");
            }
        }

        public IMongoCollection<Handover> Handover
        {
            get
            {
                return _mongoDb.GetCollection<Handover>("Handover");
            }
        }

        public IMongoCollection<Link> Link
        {
            get
            {
                return _mongoDb.GetCollection<Link>("Link");
            }
        }

        public IMongoCollection<Log> Log
        {
            get
            {
                return _mongoDb.GetCollection<Log>("Log");
            }
        }

        public IMongoCollection<Material> Material
        {
            get
            {
                return _mongoDb.GetCollection<Material>("Material");
            }
        }

        public IMongoCollection<Note> Note
        {
            get
            {
                return _mongoDb.GetCollection<Note>("Note");
            }
        }

        public IMongoCollection<Trigger> Trigger
        {
            get
            {
                return _mongoDb.GetCollection<Trigger>("Trigger");
            }
        }
    }
}
