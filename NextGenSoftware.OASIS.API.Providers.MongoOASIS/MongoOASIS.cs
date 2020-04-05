using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS
{
    public class MongoOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        //MongoDbContext db = new MongoDbContext();
        //private string _connectionString = "";
        private MongoDbContext _db = null;

        public class SearchData
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
            //public int Id { get; set; }
            public string searchData { get; set; }
        }

        public MongoOASIS(string connectionString)
        {
            _db = new MongoDbContext(connectionString);
            
            this.ProviderName = "MongoOASIS";
            this.ProviderDescription = "MongoDB Atlas Provider";
            this.ProviderType = ProviderType.MongoDBOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
        }

        public Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma)
        {
            throw new NotImplementedException();
        }

        public List<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override Task<IProfile> LoadProfileAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IProfile> LoadProfileAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override Task<IProfile> LoadProfileAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            throw new NotImplementedException();
        }

        public override async Task<ISearchResults> SearchAsync(string searchTerm)
        {
            try
            {
              FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Eq("searchData", searchTerm);
                SearchData data = await _db.SearchData.Find(filter).FirstOrDefaultAsync();

                if (data != null)
                    return new SearchResults { SearchResult = data.searchData };
                else
                    return null;

                //return await db.SearchData.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }
    }
}
