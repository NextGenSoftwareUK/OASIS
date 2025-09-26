//using Neo4jClient;
//using NextGenSoftware.OASIS.API.Core;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Helpers;

//namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
//{
//    public class Neo4jOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider
//    {
//        public GraphClient GraphClient { get; set; }
//        public string Host { get; set; }
//        public string Username { get; set; }
//        public string Password { get; set; }
//        public bool IsVersionControlEnabled { get; set; }

//        public Neo4jOASIS(string host, string username, string password)
//        {
//            this.ProviderName = "Neo4jOASIS";
//            this.ProviderDescription = "Neo4j Provider";
//            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
//            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

//            Host = host;
//            Username = username;
//            Password = password;
//        }

//        private async Task<bool> Connect()
//        {
//            GraphClient = new GraphClient(new Uri(Host), Username, Password);
//            GraphClient.OperationCompleted += _graphClient_OperationCompleted;
//            await GraphClient.ConnectAsync();
//            return true;
//        }

//        private async Task Disconnect()
//        {
//            //TODO: Find if there is a disconnect/shutdown function?
//            GraphClient.Dispose();
//            GraphClient.OperationCompleted -= _graphClient_OperationCompleted;
//            GraphClient = null;
//        }

//        private void _graphClient_OperationCompleted(object sender, OperationCompletedEventArgs e)
//        {

//        }

//        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            GraphClient.Cypher.OptionalMatch("(avatar:Avatar)-[r]-()")
//                .Where((Avatar avatar) => avatar.Id == id)
//                .Delete("r,avatar")
//                .ExecuteWithoutResultsAsync();

//            return new OASISResult<bool>(true);

//            //public void DeletePerson(string personName)
//            //{
//            //    this.graphClient.Cypher.OptionalMatch("(person:Person)-[r]-()")
//            //        .Where((Person person) => person.name == personName)
//            //        .Delete("r,person")
//            //        .ExecuteWithoutResults();
//            //}
//        }

//        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
//        //{
//        //    try
//        //    {
//        //        //var people = _graphClient.Cypher
//        //        //  .Match("(p:Person)")
//        //        //  .Return(p => p.As<Person>())
//        //        //  .Results;


//        //        Avatar avatar =
//        //            GraphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})") //TODO: Need to add password to match query...
//        //           .WithParam("nameParam", username)
//        //          .Return(p => p.As<Avatar>())
//        //            .ResultsAsync.Result.Single();

//        //        return new OASISResult<IAvatar>(avatar);
//        //    }
//        //    catch (InvalidOperationException) //thrown when nothing found
//        //    {
//        //        return null;
//        //    }
//        //}

//        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
//        {
//            Avatar avatar =
//                   GraphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})")
//                  .WithParam("nameParam", username)
//                 .Return(p => p.As<Avatar>())
//                   .ResultsAsync.Result.Single();

//            return new OASISResult<IAvatar>(avatar);
//        }

//        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
//        //{
//        //    throw new NotImplementedException();

//        //    //try
//        //    //{
//        //    //    //var people = _graphClient.Cypher
//        //    //    //  .Match("(p:Person)")
//        //    //    //  .Return(p => p.As<Person>())
//        //    //    //  .Results;


//        //    //    Avatar avatar =
//        //    //        _graphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})")
//        //    //       .WithParam("nameParam", username)
//        //    //      .Return(p => p.As<Avatar>())
//        //    //        .ResultsAsync.Result.Single();

//        //    //    return Task<avatar>;
//        //    //}
//        //    //catch (InvalidOperationException) //thrown when nothing found
//        //    //{
//        //    //    return null;
//        //    //}
//        //}

//        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
//        {
//            if (avatar.Id == Guid.Empty)
//            {
//                // _graphClient.ExecutionConfiguration.EncryptionLevel = Neo4j.Driver.EncryptionLevel.Encrypted;

//                avatar.Id = Guid.NewGuid();

//                //_graphClient.Cypher
//                //    .Unwind(persons, "person")
//                //    .Merge("(p:Person { Id: person.Id })")
//                //    .OnCreate()
//                //    .Set("p = person")
//                //    .ExecuteWithoutResults();

//                GraphClient.Cypher
//                   .Merge("(a:Avatar { Id: avatar.Id })") //Only create if doesn't alreadye exists.
//                   .OnCreate()
//                   .Set("a = avatar") //Once created, set the properties.
//                   .ExecuteWithoutResultsAsync();
//            }
//            else
//            {
//                GraphClient.Cypher
//                   .Match("(a:Avatar)")
//                   .Where((Avatar a) => a.Id == avatar.Id)
//                   .Set("a = avatar") //Set the properties.
//                   .ExecuteWithoutResultsAsync();


//                /*
//                ITransactionalGraphClient txClient = _graphClient;

//                using (var tx = txClient.BeginTransaction())
//                {
//                    txClient.Cypher
//                        .Match("(m:Movie)")
//                        .Where((Movie m) => m.title == originalMovieName)
//                        .Set("m.title = {newMovieNameParam}")
//                        .WithParam("newMovieNameParam", newMovieName)
//                        .ExecuteWithoutResults();

//                    txClient.Cypher
//                        .Match("(m:Movie)")
//                        .Where((Movie m) => m.title == newMovieName)
//                        .Create("(p:Person {name: {actorNameParam}})-[:ACTED_IN]->(m)")
//                        .WithParam("actorNameParam", newActorName)
//                        .ExecuteWithoutResults();

//                    tx.CommitAsync();
//                }*/
//            }

//            return new OASISResult<IAvatar>(avatar);
//        }

//        public override OASISResult<bool> ActivateProvider()
//        {
//            Connect();
//            return base.ActivateProvider();
//        }

//        public override OASISResult<bool> DeActivateProvider()
//        {
//            Disconnect();
//            return base.DeActivateProvider();
//        }

//        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
//        {
//            throw new NotImplementedException();
//        }

//        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<bool>> Import(IEnumerable<IHolon> holons)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAll(int version = 0)
//        {
//            throw new NotImplementedException();
//        }

//        //public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
//        //{
//        //    throw new NotImplementedException();
//        //}
//    }
//}
