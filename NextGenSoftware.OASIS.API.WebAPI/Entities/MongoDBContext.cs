using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class MongoDbContext
    {
        private IMongoDatabase _mongoDbOASIS;
        private IMongoDatabase _mongoDbBEB;

        public IMongoDatabase MongoDbBEB 
        {
            get
            {
                return _mongoDbBEB;
            }
        }

        public IMongoDatabase MongoDbOASIS
        {
            get
            {
                return _mongoDbOASIS;
            }
        }

        public MongoDbContext()
        {
            MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/OASISAPI?retryWrites=true&w=majority");
            _mongoDbOASIS = mongoClient.GetDatabase("OASISAPI");

            //mongoClient = new MongoClient("mongodb+srv://dbadmin:mz0u0VKsg0Hi6JOT@beb-wfqsj.mongodb.net/test?authSource=admin&replicaSet=BEB-shard-0&w=majority&readPreference=primary&appname=MongoDB%20Compass&retryWrites=true&ssl=true");
            //mongoClient = new MongoClient("mongodb+srv://dbadmin:mz0u0VKsg0Hi6JOT@beb-wfqsj.mongodb.net/test?retryWrites=true&w=majority");
            mongoClient = new MongoClient("mongodb+srv://dbadmin:mz0u0VKsg0Hi6JOT@beb-wfqsj.mongodb.net/BEB?retryWrites=true&w=majority");
            _mongoDbBEB = mongoClient.GetDatabase("BEB");
        }
        public IMongoCollection<User> User
        {
            get
            {
                return _mongoDbOASIS.GetCollection<User>("User");
            }
        }

        public IMongoCollection<Sequence> Sequence
        {
            get
            {
                return _mongoDbBEB.GetCollection<Sequence>("Sequence");
            }
        }

        public IMongoCollection<Phase> Phase
        {
            get
            {
                return _mongoDbBEB.GetCollection<Phase>("Phase");
            }
        }

        public IMongoCollection<Contract> Contract
        {
            get
            {
                return _mongoDbBEB.GetCollection<Contract>("Contract");
            }
        }

        public IMongoCollection<Contact> Contact
        {
            get
            {
                return _mongoDbBEB.GetCollection<Contact>("Contact");
            }
        }

        public IMongoCollection<Delivery> Delivery
        {
            get
            {
                return _mongoDbBEB.GetCollection<Delivery>("Delivery");
            }
        }

        public IMongoCollection<DeliveryItem> DeliveryItem
        {
            get
            {
                return _mongoDbBEB.GetCollection<DeliveryItem>("DeliveryItem");
            }
        }

        public IMongoCollection<Drawing> Drawing
        {
            get
            {
                return _mongoDbBEB.GetCollection<Drawing>("Drawing");
            }
        }

        public IMongoCollection<File> File
        {
            get
            {
                return _mongoDbBEB.GetCollection<File>("File");
            }
        }

        public IMongoCollection<Handover> Handover
        {
            get
            {
                return _mongoDbBEB.GetCollection<Handover>("Handover");
            }
        }

        public IMongoCollection<Link> Link
        {
            get
            {
                return _mongoDbBEB.GetCollection<Link>("Link");
            }
        }

        public IMongoCollection<Log> Log
        {
            get
            {
                return _mongoDbBEB.GetCollection<Log>("Log");
            }
        }

        public IMongoCollection<Material> Material
        {
            get
            {
                return _mongoDbBEB.GetCollection<Material>("Material");
            }
        }

        public IMongoCollection<Note> Note
        {
            get
            {
                return _mongoDbBEB.GetCollection<Note>("Note");
            }
        }

        public IMongoCollection<Trigger> Trigger
        {
            get
            {
                return _mongoDbBEB.GetCollection<Trigger>("Trigger");
            }
        }
    }
}
