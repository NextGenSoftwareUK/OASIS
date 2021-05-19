using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class MongoDBOASIS : OASISStorageBase, IOASISStorage, IOASISNET, IOASISSuperStar
    {
        public MongoDbContext Database { get; set; }
        private AvatarRepository _avatarRepository = null;
        private HolonRepository _holonRepository = null;

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

            this.ProviderName = "MongoDBOASIS";
            this.ProviderDescription = "MongoDB Atlas Provider";
            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public Task<bool> AddKarmaToAvatarAsync(IAvatar Avatar, int karma)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
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
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(Id).Result);
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

        public override IAvatar LoadAvatar(string username)
        {
            //return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username, password).Result));

          //  Thread.Sleep(5000);

            Avatar avatar = _avatarRepository.GetAvatar(username).Result;
            IAvatar oasisAvatar = ConvertMongoEntityToOASISAvatar(avatar);

            //TODO: {URGENT} The calling method never returns and waits forever, need to fix this ASAP
            return oasisAvatar;
            // return oasisAvatar;
        }

        public override IAvatar SaveAvatar(IAvatar avatar)
        {
            return ConvertMongoEntityToOASISAvatar(avatar.Id == Guid.Empty ?
                _avatarRepository.Add(ConvertOASISAvatarToMongoEntity(avatar)).Result :
                _avatarRepository.Update(ConvertOASISAvatarToMongoEntity(avatar)).Result);
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            return _avatarRepository.Delete(id, softDelete).Result;
        }

        public override async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await _avatarRepository.Delete(id, softDelete);
        }

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
        {
            //return new Task<IAvatar>(() => ConvertMongoEntityToOASISAvatar(avatar.Id == Guid.Empty ? 
            //    _avatarRepository.Add(ConvertOASISAvatarToMongoEntity(avatar)).Result : 
            //    _avatarRepository.Update(ConvertOASISAvatarToMongoEntity(avatar)).Result));

            return  ConvertMongoEntityToOASISAvatar(avatar.Id == Guid.Empty ?
               _avatarRepository.Add(ConvertOASISAvatarToMongoEntity(avatar)).Result :
               _avatarRepository.Update(ConvertOASISAvatarToMongoEntity(avatar)).Result);
        }

        //TODO: Move this into Search Reposirary like Avatar is...
        //TODO: {URGENT} FIX BEB SEARCH TO WORK WITH ISearchParams instead of string as it use to be!
        public override async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            try
            {

                //_db.SearchData.Find({ cuisine: "Hamburgers" } );
                //_db.SearchData.Find(new FilterDefinition<SearchData>() { })

                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Eq("searchData", searchTerm);
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm + "/G[a-b].*/i"));
                FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.Regex("searchData", new BsonRegularExpression("/" + searchTerm.SearchQuery.ToLower() + "/"));
                //FilterDefinition<SearchData> filter = Builders<SearchData>.Filter.AnyIn("searchData", searchTerm);
                IEnumerable<SearchData> data = await Database.SearchData.Find(filter).ToListAsync();


                
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

        private IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(IEnumerable<Avatar> avatars)
        {
            List<IAvatar> oasisAvatars = new List<IAvatar>();

            foreach (Avatar avatar in avatars)
                oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(avatar));

            return oasisAvatars;
        }

        private IEnumerable<IHolon> ConvertMongoEntitysToOASISHolons(IEnumerable<Holon> holons)
        {
            List<IHolon> oasisHolons = new List<IHolon>();

            foreach (Holon holon in holons)
                oasisHolons.Add(ConvertMongoEntityToOASISHolon(holon));

            return oasisHolons;
        }

        private IAvatar ConvertMongoEntityToOASISAvatar(Avatar avatar)
        {
            if (avatar == null)
                return null;

            Core.Holons.Avatar oasisAvatar = new Core.Holons.Avatar();

            oasisAvatar.Id = Guid.Parse(avatar.AvatarId);

            //oasisAvatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
            oasisAvatar.ProviderKey = avatar.ProviderKey;
            oasisAvatar.ProviderMetaData = avatar.ProviderMetaData;
            oasisAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            oasisAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            oasisAvatar.ProviderUsername = avatar.ProviderUsername;
            oasisAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            oasisAvatar.XP = avatar.XP;
            oasisAvatar.FavouriteColour = avatar.FavouriteColour;
            oasisAvatar.STARCLIColour = avatar.STARCLIColour;
            oasisAvatar.Skills = avatar.Skills;
            oasisAvatar.Spells = avatar.Spells;
            oasisAvatar.Stats = avatar.Stats;
            oasisAvatar.SuperPowers = avatar.SuperPowers;
            oasisAvatar.GeneKeys = avatar.GeneKeys;
            oasisAvatar.HumanDesign = avatar.HumanDesign;
            oasisAvatar.Gifts = avatar.Gifts;
            oasisAvatar.Chakras = avatar.Chakras;
            oasisAvatar.Aura = avatar.Aura;
            oasisAvatar.Achievements = avatar.Achievements;
            oasisAvatar.Inventory = avatar.Inventory;
            oasisAvatar.CreatedOASISType = avatar.CreatedOASISType;

            // If the mongo Key has not been set then set it now (unique id for mongo)
            if (!oasisAvatar.ProviderKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                oasisAvatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

            oasisAvatar.Description = avatar.Description;
            oasisAvatar.HolonType = avatar.HolonType;
            oasisAvatar.CreatedProviderType = new Core.Helpers.EnumValue<ProviderType>(avatar.CreatedProviderType);

            oasisAvatar.ChangesSaved = avatar.ChangesSaved;
            oasisAvatar.ParentHolonId = avatar.ParentHolonId;
            oasisAvatar.ParentHolon = avatar.ParentHolon;
            oasisAvatar.ParentZomeId = avatar.ParentZomeId;
            oasisAvatar.ParentZome = avatar.ParentZome;
            oasisAvatar.ParentStarId = avatar.ParentStarId;
            oasisAvatar.ParentStar = avatar.ParentStar;
            oasisAvatar.ParentPlanetId = avatar.ParentPlanetId;
            oasisAvatar.ParentPlanet = avatar.ParentPlanet;
            oasisAvatar.ParentMoonId = avatar.ParentMoonId;
            oasisAvatar.ParentMoon = avatar.ParentMoon;
            oasisAvatar.Children = avatar.Children;
            oasisAvatar.Nodes = avatar.Nodes;

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
            oasisAvatar.ModifiedDate = avatar.ModifiedDate;
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
            oasisAvatar.Karma = avatar.Karma;
            oasisAvatar.KarmaAkashicRecords = avatar.KarmaAkashicRecords;
            oasisAvatar.Version = avatar.Version;
            oasisAvatar.AcceptTerms = avatar.AcceptTerms;
            oasisAvatar.JwtToken = avatar.JwtToken;
            oasisAvatar.PasswordReset = avatar.PasswordReset;
            oasisAvatar.RefreshToken = avatar.RefreshToken;
            oasisAvatar.RefreshTokens = avatar.RefreshTokens;
            oasisAvatar.ResetToken = avatar.ResetToken;
            oasisAvatar.ResetTokenExpires = avatar.ResetTokenExpires;
            oasisAvatar.VerificationToken = avatar.VerificationToken;
            oasisAvatar.Verified = avatar.Verified;
            oasisAvatar.CreatedProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
            oasisAvatar.IsActive = avatar.IsActive;
         //   oasisAvatar.ChangesSaved = true;

          //  oasisAvatar.SetKarmaForDataObject(avatar.Karma);
            return oasisAvatar;
        }

        private Avatar ConvertOASISAvatarToMongoEntity(IAvatar avatar)
        {
            if (avatar == null)
                return null;

            Avatar mongoAvatar = new Avatar();

            //TODO: Refactor to move this top block into generic base holon conversion code.
            if (avatar.ProviderKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS];

            mongoAvatar.AvatarId = avatar.Id.ToString();
           // mongoAvatar.Name = avatar.Name;
            mongoAvatar.Description = avatar.Description;
            mongoAvatar.HolonType = avatar.HolonType;

            mongoAvatar.ProviderKey = avatar.ProviderKey;
            mongoAvatar.MetaData = avatar.MetaData;
            mongoAvatar.ProviderMetaData = avatar.ProviderMetaData;
            mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.XP = avatar.XP;
            mongoAvatar.FavouriteColour = avatar.FavouriteColour;
            mongoAvatar.STARCLIColour = avatar.STARCLIColour;
            mongoAvatar.Skills = avatar.Skills;
            mongoAvatar.Spells = avatar.Spells;
            mongoAvatar.Stats = avatar.Stats;
            mongoAvatar.SuperPowers = avatar.SuperPowers;
            mongoAvatar.GeneKeys = avatar.GeneKeys;
            mongoAvatar.HumanDesign = avatar.HumanDesign;
            mongoAvatar.Gifts = avatar.Gifts;
            mongoAvatar.Chakras = avatar.Chakras;
            mongoAvatar.Aura = avatar.Aura;
            mongoAvatar.Achievements = avatar.Achievements;
            mongoAvatar.Inventory = avatar.Inventory;
            mongoAvatar.CreatedOASISType = avatar.CreatedOASISType;

            //if (avatar.ProviderKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
            //    mongoAvatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

            if (avatar.CreatedProviderType != null)
                mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            mongoAvatar.ChangesSaved = avatar.ChangesSaved;
            mongoAvatar.ParentHolonId = avatar.ParentHolonId;
            mongoAvatar.ParentHolon = avatar.ParentHolon;
            mongoAvatar.ParentZomeId = avatar.ParentZomeId;
            mongoAvatar.ParentZome = avatar.ParentZome;
            mongoAvatar.ParentStarId = avatar.ParentStarId;
            mongoAvatar.ParentStar = avatar.ParentStar;
            mongoAvatar.ParentPlanetId = avatar.ParentPlanetId;
            mongoAvatar.ParentPlanet = avatar.ParentPlanet;
            mongoAvatar.ParentMoonId = avatar.ParentMoonId;
            mongoAvatar.ParentMoon = avatar.ParentMoon;
            //mongoAvatar.ParentCelestialBodyId = avatar.ParentCelestialBodyId;
            //mongoAvatar.ParentCelestialBody = avatar.ParentCelestialBody;
            mongoAvatar.Children = avatar.Children;
            mongoAvatar.Nodes = avatar.Nodes;

            mongoAvatar.Address = avatar.Address;
            mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.CreatedByAvatarId = avatar.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatar.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatar.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatar.ModifiedDate;
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.FirstName = avatar.FirstName;
            mongoAvatar.LastName = avatar.LastName;
            mongoAvatar.Address = avatar.Address;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.Email = avatar.Email;
            mongoAvatar.DOB = avatar.DOB;
            mongoAvatar.Landline = avatar.Landline;
            mongoAvatar.Mobile = avatar.Mobile;
            mongoAvatar.Password = avatar.Password;
            mongoAvatar.Postcode = avatar.Postcode;
            mongoAvatar.Title = avatar.Title;
            mongoAvatar.Town = avatar.Town;
            mongoAvatar.Username = avatar.Username;
            mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.Version = avatar.Version;
           // mongoAvatar.Karma = avatar.Karma;
            mongoAvatar.KarmaAkashicRecords = avatar.KarmaAkashicRecords;
            mongoAvatar.AcceptTerms = avatar.AcceptTerms;
            mongoAvatar.JwtToken = avatar.JwtToken;
            mongoAvatar.PasswordReset = avatar.PasswordReset;
            mongoAvatar.RefreshToken = avatar.RefreshToken;
            mongoAvatar.RefreshTokens = avatar.RefreshTokens;
            mongoAvatar.ResetToken = avatar.ResetToken;
            mongoAvatar.ResetTokenExpires = avatar.ResetTokenExpires;
            mongoAvatar.VerificationToken = avatar.VerificationToken;
            mongoAvatar.Verified = avatar.Verified;
            mongoAvatar.IsActive = avatar.IsActive;

            //mongoAvatar.Karma = avatar.Karma;

            mongoAvatar.Karma = avatar.Karma;

            return mongoAvatar;
        }

        private IHolon ConvertMongoEntityToOASISHolon(Holon holon)
        {
            if (holon == null)
                return null;

            IHolon oasisHolon = new Core.Holons.Holon();

            //oasisHolon.Id = Guid.Parse(holon.Id);
            oasisHolon.Id = holon.HolonId;

            oasisHolon.ProviderKey = holon.ProviderKey;

            if (!oasisHolon.ProviderKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                oasisHolon.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = holon.Id;

            oasisHolon.MetaData = holon.MetaData;
            oasisHolon.ProviderMetaData = holon.ProviderMetaData;

            oasisHolon.Name = holon.Name;
            oasisHolon.Description = holon.Description;
            oasisHolon.HolonType = holon.HolonType;
            oasisHolon.CreatedProviderType = new Core.Helpers.EnumValue<ProviderType>(holon.CreatedProviderType);

            oasisHolon.ChangesSaved = holon.ChangesSaved;
            oasisHolon.ParentHolonId = holon.ParentHolonId;
            oasisHolon.ParentHolon = holon.ParentHolon;
            oasisHolon.ParentZomeId = holon.ParentZomeId;
            oasisHolon.ParentZome = holon.ParentZome;
            oasisHolon.ParentStarId = holon.ParentStarId;
            oasisHolon.ParentStar = holon.ParentStar;
            oasisHolon.ParentPlanetId = holon.ParentPlanetId;
            oasisHolon.ParentPlanet = holon.ParentPlanet;
            oasisHolon.ParentMoonId = holon.ParentMoonId;
            oasisHolon.ParentMoon = holon.ParentMoon;
            oasisHolon.Children = holon.Children;
            oasisHolon.Nodes = holon.Nodes;
            
            oasisHolon.CreatedByAvatarId = Guid.Parse(holon.CreatedByAvatarId);
            //oasisHolon.CreatedByAvatarId = holon.CreatedByAvatarId;
            oasisHolon.CreatedDate = holon.CreatedDate;
            oasisHolon.DeletedByAvatarId = Guid.Parse(holon.DeletedByAvatarId);
            //oasisHolon.DeletedByAvatarId = holon.DeletedByAvatarId;
            oasisHolon.DeletedDate = holon.DeletedDate;
            oasisHolon.ModifiedByAvatarId = Guid.Parse(holon.ModifiedByAvatarId);
            //oasisHolon.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            oasisHolon.ModifiedDate = holon.ModifiedDate;
            oasisHolon.DeletedDate = holon.DeletedDate;
            oasisHolon.Version = holon.Version;
            oasisHolon.CreatedProviderType.Value = Core.Enums.ProviderType.MongoDBOASIS;
            oasisHolon.IsActive = holon.IsActive;

            return oasisHolon;
        }

        private Holon ConvertOASISHolonToMongoEntity(IHolon holon)
        {
            if (holon == null)
                return null;

            Holon mongoHolon = new Holon();

            mongoHolon.HolonId = holon.Id;
            mongoHolon.Name = holon.Name;
            mongoHolon.Description = holon.Description;
            mongoHolon.HolonType = holon.HolonType;

            mongoHolon.MetaData = holon.MetaData;
            mongoHolon.ProviderMetaData = holon.ProviderMetaData;

            // if (holon.ProviderKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
            //   mongoHolon.ProviderKey = holon.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS];
            mongoHolon.ProviderKey = holon.ProviderKey;

            if (holon.CreatedProviderType != null)
                mongoHolon.CreatedProviderType = holon.CreatedProviderType.Value;

            mongoHolon.ChangesSaved = holon.ChangesSaved;
            mongoHolon.ParentHolonId = holon.ParentHolonId;
            mongoHolon.ParentHolon = holon.ParentHolon;
            mongoHolon.ParentZomeId = holon.ParentZomeId;
            mongoHolon.ParentZome = holon.ParentZome;
            mongoHolon.ParentStarId = holon.ParentStarId;
            mongoHolon.ParentStar = holon.ParentStar;
            mongoHolon.ParentPlanetId = holon.ParentPlanetId;
            mongoHolon.ParentPlanet = holon.ParentPlanet;
            mongoHolon.ParentMoonId = holon.ParentMoonId;
            mongoHolon.ParentMoon = holon.ParentMoon;
            mongoHolon.Children = holon.Children;
            mongoHolon.Nodes = holon.Nodes;

            mongoHolon.CreatedByAvatarId = holon.CreatedByAvatarId.ToString();
            //mongoHolon.CreatedByAvatarId = holon.CreatedByAvatarId;
            mongoHolon.CreatedDate = holon.CreatedDate;
            mongoHolon.DeletedByAvatarId = holon.DeletedByAvatarId.ToString();
            //mongoHolon.DeletedByAvatarId = holon.DeletedByAvatarId;
            mongoHolon.DeletedDate = holon.DeletedDate;
            mongoHolon.ModifiedByAvatarId = holon.ModifiedByAvatarId.ToString();
            //mongoHolon.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            mongoHolon.ModifiedDate = holon.ModifiedDate;
            mongoHolon.DeletedDate = holon.DeletedDate;
            mongoHolon.Version = holon.Version;
            mongoHolon.IsActive = holon.IsActive;

            return mongoHolon;
        }

        public override void ActivateProvider()
        {
            //TODO: {URGENT} Find out how to check if MongoDB is connected, etc here...
            if (Database == null)
            {
                Database = new MongoDbContext(ConnectionString, DBName);
                _avatarRepository = new AvatarRepository(Database);
                _holonRepository = new HolonRepository(Database);
            }

            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            //TODO: {URGENT} Disconnect, Dispose and release resources here.
            Database.MongoDB = null;
            Database.MongoClient = null;
            Database = null;

            base.DeActivateProvider();
        }

        public override IHolon LoadHolon(Guid id, HolonType type = HolonType.Holon)
        {
            return ConvertMongoEntityToOASISHolon(_holonRepository.GetHolon(id).Result);
        }

        public override IHolon LoadHolon(string providerKey, HolonType type = HolonType.Holon)
        {
            return ConvertMongoEntityToOASISHolon(_holonRepository.GetHolon(providerKey).Result);
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.Holon)
        {
            return ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsForParent(id).Result);
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }
        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
        {
            return ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolons().Result);
        }

        public override IHolon SaveHolon(IHolon holon)
        {
            return ConvertMongoEntityToOASISHolon(holon.Id == Guid.Empty ?
               _holonRepository.Add(ConvertOASISHolonToMongoEntity(holon)).Result :
               _holonRepository.Update(ConvertOASISHolonToMongoEntity(holon)).Result);
        }

        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon)
        {
            //TODO: Need to make this a true async method!
            return Task.Run(() => ConvertMongoEntityToOASISHolon(_holonRepository.GetHolon(id).Result));
        }

        public override async Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override async Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            return ConvertMongoEntityToOASISHolon(holon.Id == Guid.Empty ?
              _holonRepository.Add(ConvertOASISHolonToMongoEntity(holon)).Result :
              _holonRepository.Update(ConvertOASISHolonToMongoEntity(holon)).Result);
        }

        public override async Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
        {
            List<IHolon> savedHolons = new List<IHolon>();
            IHolon savedHolon;

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                savedHolon = await SaveHolonAsync(holon);
                savedHolon.Children = await SaveHolonsAsync(holon.Children);
                savedHolons.Add(savedHolon);
            }

            return savedHolons;
        }

        public override async Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(providerKey).Result);
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(providerKey).Result);
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _avatarRepository.Delete(providerKey, softDelete).Result;
        }

        public override async Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _avatarRepository.Delete(providerKey, softDelete);
        }

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            return _holonRepository.Delete(id, softDelete).Result;
        }

        public override async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            return await _holonRepository.Delete(id, softDelete);
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            return _holonRepository.Delete(providerKey, softDelete).Result;
        }

        public override async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            return await _holonRepository.Delete(providerKey, softDelete);
        }


        //IOASISSuperStar Interface Implementation

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }
    }
}
