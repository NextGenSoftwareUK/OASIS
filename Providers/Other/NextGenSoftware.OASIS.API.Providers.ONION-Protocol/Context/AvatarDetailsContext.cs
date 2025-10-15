//using Microsoft.Extensions.Options;
//using MongoDB.Driver;
//using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
//using NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Model;

//namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol.Context
//{
//    internal class AvatarDetailsContext
//    {
//        private readonly IMongoDatabase _database = null;

//        public AvatarDetailsContext(IOptions<Settings> settings)
//        {
//            var client = new MongoClient(settings.Value.ConnectionString);
//            if (client != null)
//                _database = client.GetDatabase(settings.Value.Database);
//        }

//        public IMongoCollection<AvatarDetail> AvatarDetails
//        {
//            get
//            {
//                return _database.GetCollection<AvatarDetail>("AvatarDetail");
//            }
//        }
//    }
//}