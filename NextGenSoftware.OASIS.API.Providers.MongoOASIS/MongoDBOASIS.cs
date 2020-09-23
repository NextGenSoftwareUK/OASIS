using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class MongoDBOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        //MongoDbContext db = new MongoDbContext();
        private MongoDbContext _db = null;
        private AvatarRepository _avatarRepository = null;

        public string ConnectionString { get; set; }
        public string DBName { get; set; }

        public class SearchData
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
            //public int Id { get; set; }
            public string searchData { get; set; }
        }

        public MongoDBOASIS(string connectionString, string dbName)
        {
            ConnectionString = connectionString;
            DBName = dbName;

            _db = new MongoDbContext(connectionString, dbName);
            _avatarRepository = new AvatarRepository(_db);

            this.ProviderName = "MongoDBOASIS";
            this.ProviderDescription = "MongoDB Atlas Provider";
            this.ProviderType = ProviderType.MongoDBOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
        }

        public Task<bool> AddKarmaToAvatarAsync(IAvatar Avatar, int karma)
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
        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            return new Task<IEnumerable<IAvatar>>(() => ConvertMongoEntitysToOASISAvatars(_avatarRepository.GetAvatars().Result));
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            return ConvertMongoEntitysToOASISAvatars(_avatarRepository.GetAvatars().Result);
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            //TODO: Check if this is correct way to handle Task/async...
            return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(providerKey).Result));
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(Id.ToString()).Result));
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(Id.ToString()).Result);
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            //return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username, password).Result));

            Avatar avatar = _avatarRepository.GetAvatar(username, password).Result;
            IAvatar oasisAvatar = ConvertMongoEntityToOASISAvatar(avatar);

            //TODO: {URGENT} The calling method never returns and waits forever, need to fix this ASAP
            return new Task<IAvatar>(() => oasisAvatar);
           // return oasisAvatar;
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            //return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username, password).Result));

            Avatar avatar = _avatarRepository.GetAvatar(username, password).Result;
            IAvatar oasisAvatar = ConvertMongoEntityToOASISAvatar(avatar);

            //TODO: {URGENT} The calling method never returns and waits forever, need to fix this ASAP
            return oasisAvatar;
            // return oasisAvatar;
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
        {
            return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(avatar.Id == Guid.Empty ? 
                _avatarRepository.Add(ConvertOASISAvatarToMongoEntity(avatar)).Result : 
                _avatarRepository.Update(ConvertOASISAvatarToMongoEntity(avatar)).Result));
        }

        //TODO: Move this into Search Reposirary like Avatar is...
        public override async Task<ISearchResults> SearchAsync(string searchTerm)
        {
            try
            {

                //_db.SearchData.Find({ cuisine: "Hamburgers" } );
                //_db.SearchData.Find(new FilterDefinition<SearchData>() { })

                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Eq("searchData", searchTerm);
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm + "/G[a-b].*/i"));
                FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm.ToLower() + "/"));
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.AnyIn("searchData", searchTerm);
                List<SearchData> data = await _db.SearchData.Find(filter).ToListAsync();


                
                //Query.Matches("name", "Joe")

                if (data != null)
                {
                    List<string> results = new List<string>();

                    foreach (SearchData dataObj in data)
                        results.Add( dataObj.searchData );

                    return new SearchResults() {  SearchResultStrings = results };
                }
                else
                    return null;
                
                //System.InvalidOperationException: The serializer for field 'searchData' must implement IBsonArraySerializer and provide item serialization info.
                //return await db.SearchData.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        private IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(List<Avatar> avatars)
        {
            List<IAvatar> oasisAvatars = new List<IAvatar>();

            foreach (Avatar avatar in avatars)
                oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(avatar));

            return oasisAvatars;
        }

        private IAvatar ConvertMongoEntityToOASISAvatar(Avatar avatar)
        {
            Core.Avatar oasisAvatar = new Core.Avatar();

            oasisAvatar.Id = Guid.Parse(avatar.AvatarId);
            oasisAvatar.ProviderKey = avatar.Id;
            oasisAvatar.Address = avatar.Address;
            oasisAvatar.AvatarType = avatar.AvatarType;
            oasisAvatar.Country = avatar.Country;
            oasisAvatar.County = avatar.County;
            oasisAvatar.CreatedByAvatarId = Guid.Parse(avatar.CreatedByAvatarId);
            //oasisAvatar.CreatedDate = Convert.ToDateTime(avatar.CreatedDate);
            oasisAvatar.CreatedDate = avatar.CreatedDate;
            oasisAvatar.DeletedByAvatarId = Guid.Parse(avatar.DeletedByAvatarId);
            //oasisAvatar.DeletedDate = Convert.ToDateTime(avatar.DeletedDate);
            oasisAvatar.DeletedDate = avatar.DeletedDate;
            oasisAvatar.ModifiedByAvatarId = Guid.Parse(avatar.ModifiedByAvatarId);
            //oasisAvatar.DeletedDate = Convert.ToDateTime(avatar.DeletedDate);
            oasisAvatar.DeletedDate = avatar.DeletedDate;
            oasisAvatar.FirstName = avatar.FirstName;
            oasisAvatar.LastName = avatar.LastName;
            oasisAvatar.Address = avatar.Address;
            oasisAvatar.Country = avatar.Country;
            oasisAvatar.County = avatar.County;
            oasisAvatar.Email = avatar.Email;
            oasisAvatar.DOB = avatar.DOB;
            oasisAvatar.Landline = avatar.Landline;
            oasisAvatar.Mobile = avatar.Mobile;
            oasisAvatar.Password = avatar.Password;
            oasisAvatar.Postcode = avatar.Postcode;
            oasisAvatar.Title = avatar.Title;
            oasisAvatar.Town = avatar.Town;
            oasisAvatar.Username = avatar.Username;
            oasisAvatar.AvatarType = avatar.AvatarType;
            oasisAvatar.Version = avatar.Version;

            return oasisAvatar;
        }

        private Avatar ConvertOASISAvatarToMongoEntity(IAvatar avatar)
        {
            Avatar mongoAvatar = new Avatar();

            mongoAvatar.Id = avatar.ProviderKey;
            mongoAvatar.AvatarId = avatar.Id.ToString();
            mongoAvatar.Address = avatar.Address;
            mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.CreatedByAvatarId = avatar.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatar.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatar.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.FirstName = avatar.FirstName;
            mongoAvatar.LastName = avatar.LastName;
            mongoAvatar.Address = avatar.Address;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.Email = avatar.Email;
            mongoAvatar.DOB = avatar.DOB;
            mongoAvatar.Landline = avatar.LastName;
            mongoAvatar.Mobile = avatar.Mobile;
            mongoAvatar.Password = avatar.Password;
            mongoAvatar.Postcode = avatar.Postcode;
            mongoAvatar.Title = avatar.Title;
            mongoAvatar.Town = avatar.Town;
            mongoAvatar.Username = avatar.Username;
            mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.Version = avatar.Version;

            return mongoAvatar;
        }

        public override void ActivateProvider()
        {
            //TODO: {URGENT} Find out how to check if MongoDB is connected, etc here...
            //if (_db.MongoDB.)

            if (_db == null)
            {
                _db = new MongoDbContext(ConnectionString, DBName);
                _avatarRepository = new AvatarRepository(_db);
            }

            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            //TODO: {URGENT} Disconnect, Dispose and release resources here.
            _db.MongoDB = null;
            _db.MongoClient = null;
            _db = null;

            base.DeActivateProvider();
        }
    }
}
