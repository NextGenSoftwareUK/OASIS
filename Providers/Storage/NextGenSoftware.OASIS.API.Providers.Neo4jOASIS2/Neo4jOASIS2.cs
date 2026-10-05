//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using Neo4j.Driver;
//using System.Reflection;
//using Neo4jOgm.Repository;
//using Neo4jOgm;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels;
//using Neo4jOgm.Domain;
//using Neo4jClient;
////using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Repositories;

//namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
//{
//    public class Neo4jOASIS2 : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
//    {
//        public GraphClient GraphClient { get; set; }
//        public string Host { get; set; }
//        public string Username { get; set; }
//        public string Password { get; set; }
//        public NeoRepository NeoRepository { get; set; }
//       // private AvatarRepository avatarRepository;


//        public Neo4jOASIS2(string host, string username, string password)
//        {
//            this.ProviderName = "Neo4jOASIS";
//            this.ProviderDescription = "Neo4j Provider";
//            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
//            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
//            Host = host;
//            Username = username;
//            Password = password;

//        }

//        private bool Connect()
//        {
//            var context = new NeoContext(Assembly.GetExecutingAssembly());
//            var authToken = AuthTokens.Basic(Username, Password);
//            var driver = GraphDatabase.Driver(Host, authToken);
//           // NeoRepository = new NeoRepository(driver, "neo4j", context);
//          //  avatarRepository = new AvatarRepository(NeoRepository);

//            return true;
//        }

//        private bool Disconnect()
//        {
//            NeoRepository = null;
//            return true;

//        }

//        //private void _graphClient_OperationCompleted(object sender, OperationCompletedEventArgs e)
//        //{

//        //}

//        public override bool DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            //GraphClient.Cypher.OptionalMatch("(avatar:Avatar)-[r]-()")
//            //    .Where((Avatar avatar) => avatar.Id == id)
//            //    .Delete("r,avatar")
//            //    .ExecuteWithoutResultsAsync();

//            return true;

//            //public void DeletePerson(string personName)
//            //{
//            //    this.graphClient.Cypher.OptionalMatch("(person:Person)-[r]-()")
//            //        .Where((Person person) => person.name == personName)
//            //        .Delete("r,person")
//            //        .ExecuteWithoutResults();
//            //}
//        }

//        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
//        {


//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteHolon(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override bool DeleteHolon(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<IPlayer> GetPlayersNearMe()
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IAvatar> LoadAllAvatars()
//        {
//            throw new NotImplementedException();
//            // return avatarRepository.GetAvatars();
//        }

//        public override IAvatar LoadAvatarByEmail(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatarByUsername(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatar LoadAvatar(Guid Id)
//        {
//           throw new NotImplementedException(); 
//        }

//        public override IAvatar LoadAvatar(string username, string password)
//        {
//            throw new NotImplementedException();

//            //try
//            //{
//            //    Avatar avatar = avatarRepository.GetAvatar(username, password);
//            //    return avatar;
//            //}
//            //catch (InvalidOperationException) //thrown when nothing found
//            //{
//            //    return null;
//            //}
//        }

//        public override IAvatar LoadAvatar(string username)
//        {
//            throw new NotImplementedException();

//            //try
//            //{
//            //    Avatar avatar = avatarRepository.GetAvatar(username);
//            //    return avatar;
//            //}
//            //catch (InvalidOperationException) //thrown when nothing found
//            //{
//            //    return null;
//            //}
//        }

//        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
//        {
//            throw new NotImplementedException();

//            //try
//            //{
//            //    //var people = _graphClient.Cypher
//            //    //  .Match("(p:Person)")
//            //    //  .Return(p => p.As<Person>())
//            //    //  .Results;


//            //    Avatar avatar =
//            //        _graphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})")
//            //       .WithParam("nameParam", username)
//            //      .Return(p => p.As<Avatar>())
//            //        .ResultsAsync.Result.Single();

//            //    return Task<avatar>;
//            //}
//            //catch (InvalidOperationException) //thrown when nothing found
//            //{
//            //    return null;
//            //}
//        }

//        public override IAvatar LoadAvatarByProviderKey(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatar> LoadAvatarByProviderKeyAsync(string providerKey)
//        {
//            throw new NotImplementedException();
//        }


//        public override IAvatar SaveAvatar(IAvatar avatar)
//        {

//            //if (avatar.Id == Guid.Empty)
//            //{

//            //    Avatar converted = (Avatar)avatar;
//            //    var cre = avatarRepository.Add(converted);

//            //}
//            //else
//            //{
//            //    Avatar converted = (Avatar)avatar;
//            //    avatarRepository.Update(converted);
//            //}

//            return avatar;
//        }

//        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
//        {
//            throw new NotImplementedException();
//        }

 
//        public override Task<ISearchResults> SearchAsync(ISearchParams searchParams)
//        {
//            throw new NotImplementedException();
//        }

//        public override void ActivateProvider()
//        {
//            Connect();
//            base.ActivateProvider();
//        }

//        public override void DeActivateProvider()
//        {
//            Disconnect();
//            base.DeActivateProvider();
//        }

//        public override IHolon LoadHolon(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IHolon> LoadHolonAsync(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override IHolon LoadHolon(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IHolon> LoadHolonAsync(string providerKey)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All)
//        {
//            throw new NotImplementedException();
//        }


//        public override IAvatarDetail LoadAvatarDetail(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
//        {
//            throw new NotImplementedException();
//        }

//        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
//        {
//            throw new NotImplementedException();
//        }

//        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
