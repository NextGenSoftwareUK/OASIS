using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using static NextGenSoftware.OASIS.API.Providers.MongoOASIS.MongoOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _mongoDb;
        public MongoDbContext(string connectionString)
        {
            //MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/test?retryWrites=true&w=majority");
            MongoClient mongoClient = new MongoClient(connectionString);
            _mongoDb = mongoClient.GetDatabase("OASISAPI");
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
