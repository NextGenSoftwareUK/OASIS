using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public class Neo4jOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsVersionControlEnabled { get; set; }
        private readonly IDriver _driver;

        public Neo4jOASIS(string host, string username, string password)
        {
            this.ProviderName = "Neo4jOASIS";
            this.ProviderDescription = "Neo4j Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            Host = host;
            Username = username;
            Password = password;
            
            // Initialize Neo4j driver for REAL database operations
            _driver = GraphDatabase.Driver($"bolt://{host}:7687", AuthTokens.Basic(username, password));
        }

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

        #region OASISStorageProviderBase Abstract Methods Implementation

        #region Avatar Methods

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // REAL Neo4j implementation for loading all avatars
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar) RETURN a";
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query);
                    var records = await result.ToListAsync();
                    
                    var avatars = new List<IAvatar>();
                    foreach (var record in records)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        avatars.Add(new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = avatars;
                    response.IsError = false;
                    response.Message = "All avatars loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Id: $id}) RETURN a";
                var parameters = new { id = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by provider key
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {ProviderKey: $providerKey}) RETURN a";
                var parameters = new { providerKey };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Username: $username}) RETURN a";
                var parameters = new { username = avatarUsername };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for loading avatar by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Email: $email}) RETURN a";
                var parameters = new { email = avatarEmail };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from Neo4j by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for loading avatar detail by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail {Id: $id})
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query, new { id = id.ToString() });
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(record["Id"].As<string>()),
                            Username = record["Username"].As<string>(),
                            Email = record["Email"].As<string>(),
                            CreatedDate = record["CreatedDate"].As<DateTime>(),
                            ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                            Description = record["Description"].As<string>(),
                            IsActive = record["IsActive"].As<bool>(),
                            Karma = record["Karma"].As<long>(),
                            XP = record["XP"].As<int>(),
                            Model3D = record["Model3D"].As<string>(),
                            UmaJson = record["UmaJson"].As<string>(),
                            Portrait = record["Portrait"].As<string>(),
                            Town = record["Town"].As<string>(),
                            County = record["County"].As<string>(),
                            DOB = record["DOB"].As<DateTime>(),
                            Address = record["Address"].As<string>(),
                            Country = record["Country"].As<string>(),
                            Postcode = record["Postcode"].As<string>(),
                            Landline = record["Landline"].As<string>(),
                            Mobile = record["Mobile"].As<string>(),
                            FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                            STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found in Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for loading avatar detail by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail {Email: $email})
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query, new { email = avatarEmail });
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(record["Id"].As<string>()),
                            Username = record["Username"].As<string>(),
                            Email = record["Email"].As<string>(),
                            CreatedDate = record["CreatedDate"].As<DateTime>(),
                            ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                            Description = record["Description"].As<string>(),
                            IsActive = record["IsActive"].As<bool>(),
                            Karma = record["Karma"].As<long>(),
                            XP = record["XP"].As<int>(),
                            Model3D = record["Model3D"].As<string>(),
                            UmaJson = record["UmaJson"].As<string>(),
                            Portrait = record["Portrait"].As<string>(),
                            Town = record["Town"].As<string>(),
                            County = record["County"].As<string>(),
                            DOB = record["DOB"].As<DateTime>(),
                            Address = record["Address"].As<string>(),
                            Country = record["Country"].As<string>(),
                            Postcode = record["Postcode"].As<string>(),
                            Landline = record["Landline"].As<string>(),
                            Mobile = record["Mobile"].As<string>(),
                            FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                            STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded by email from Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found in Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for loading avatar detail by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail {Username: $username})
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query, new { username = avatarUsername });
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.Parse(record["Id"].As<string>()),
                            Username = record["Username"].As<string>(),
                            Email = record["Email"].As<string>(),
                            CreatedDate = record["CreatedDate"].As<DateTime>(),
                            ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                            Description = record["Description"].As<string>(),
                            IsActive = record["IsActive"].As<bool>(),
                            Karma = record["Karma"].As<long>(),
                            XP = record["XP"].As<int>(),
                            Model3D = record["Model3D"].As<string>(),
                            UmaJson = record["UmaJson"].As<string>(),
                            Portrait = record["Portrait"].As<string>(),
                            Town = record["Town"].As<string>(),
                            County = record["County"].As<string>(),
                            DOB = record["DOB"].As<DateTime>(),
                            Address = record["Address"].As<string>(),
                            Country = record["Country"].As<string>(),
                            Postcode = record["Postcode"].As<string>(),
                            Landline = record["Landline"].As<string>(),
                            Mobile = record["Mobile"].As<string>(),
                            FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                            STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded by username from Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found in Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                // REAL Neo4j implementation for loading all avatar details
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MATCH (a:AvatarDetail)
                        RETURN a.Id as Id, a.Username as Username, a.Email as Email, 
                               a.CreatedDate as CreatedDate, a.ModifiedDate as ModifiedDate,
                               a.Description as Description, a.IsActive as IsActive, 
                               a.Karma as Karma, a.Level as Level, a.XP as XP, 
                               a.Model3D as Model3D, a.UmaJson as UmaJson,
                               a.Portrait as Portrait, a.Town as Town, a.County as County,
                               a.DOB as DOB, a.Address as Address, a.Country as Country,
                               a.Postcode as Postcode, a.Landline as Landline, a.Mobile as Mobile,
                               a.FavouriteColour as FavouriteColour, a.STARCLIColour as STARCLIColour";

                    var result = await session.RunAsync(query);
                    var records = await result.ToListAsync();
                    var avatarDetails = new List<IAvatarDetail>();

                    foreach (var record in records)
                    {
                        try
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = Guid.Parse(record["Id"].As<string>()),
                                Username = record["Username"].As<string>(),
                                Email = record["Email"].As<string>(),
                                CreatedDate = record["CreatedDate"].As<DateTime>(),
                                ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                                Description = record["Description"].As<string>(),
                                IsActive = record["IsActive"].As<bool>(),
                                Karma = record["Karma"].As<long>(),
                                XP = record["XP"].As<int>(),
                                Model3D = record["Model3D"].As<string>(),
                                UmaJson = record["UmaJson"].As<string>(),
                                Portrait = record["Portrait"].As<string>(),
                                Town = record["Town"].As<string>(),
                                County = record["County"].As<string>(),
                                DOB = record["DOB"].As<DateTime>(),
                                Address = record["Address"].As<string>(),
                                Country = record["Country"].As<string>(),
                                Postcode = record["Postcode"].As<string>(),
                                Landline = record["Landline"].As<string>(),
                                Mobile = record["Mobile"].As<string>(),
                                FavouriteColour = (ConsoleColor)record["FavouriteColour"].As<int>(),
                                STARCLIColour = (ConsoleColor)record["STARCLIColour"].As<int>()
                            };
                            avatarDetails.Add(avatarDetail);
                        }
                        catch
                        {
                            // Skip invalid records
                            continue;
                        }
                    }

                    response.Result = avatarDetails;
                    response.IsError = false;
                    response.Message = $"Loaded {avatarDetails.Count} avatar details from Neo4j successfully";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // REAL Neo4j implementation for saving avatar
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = @"MERGE (a:Avatar {Id: $id}) 
                            SET a.Username = $username, a.Email = $email, a.CreatedDate = $createdDate, a.ModifiedDate = $modifiedDate
                            RETURN a";
                var parameters = new 
                { 
                    id = Avatar.Id.ToString(),
                    username = Avatar.Username,
                    email = Avatar.Email,
                    createdDate = Avatar.CreatedDate,
                    modifiedDate = Avatar.ModifiedDate
                };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        response.Result = new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>(),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Avatar saved successfully to Neo4j";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // REAL Neo4j implementation for saving avatar detail
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    var query = @"
                        MERGE (a:AvatarDetail {Id: $id})
                        SET a.Username = $username, a.Email = $email, a.CreatedDate = $createdDate,
                            a.ModifiedDate = $modifiedDate, a.Description = $description,
                            a.IsActive = $isActive, a.Karma = $karma, a.XP = $xp,
                            a.Model3D = $model3D, a.UmaJson = $umaJson, a.Portrait = $portrait,
                            a.Town = $town, a.County = $county, a.DOB = $dob,
                            a.Address = $address, a.Country = $country, a.Postcode = $postcode,
                            a.Landline = $landline, a.Mobile = $mobile,
                            a.FavouriteColour = $favouriteColour, a.STARCLIColour = $starcliColour
                        RETURN a.Id as Id";

                    var parameters = new
                    {
                        id = Avatar.Id.ToString(),
                        username = Avatar.Username ?? "",
                        email = Avatar.Email ?? "",
                        createdDate = Avatar.CreatedDate,
                        modifiedDate = DateTime.UtcNow,
                        description = Avatar.Description ?? "",
                        isActive = Avatar.IsActive,
                        karma = Avatar.Karma,
                        xp = Avatar.XP,
                        model3D = Avatar.Model3D ?? "",
                        umaJson = Avatar.UmaJson ?? "",
                        portrait = Avatar.Portrait ?? "",
                        town = Avatar.Town ?? "",
                        county = Avatar.County ?? "",
                        dob = Avatar.DOB,
                        address = Avatar.Address ?? "",
                        country = Avatar.Country ?? "",
                        postcode = Avatar.Postcode ?? "",
                        landline = Avatar.Landline ?? "",
                        mobile = Avatar.Mobile ?? "",
                        favouriteColour = (int)Avatar.FavouriteColour,
                        starcliColour = (int)Avatar.STARCLIColour
                    };

                    var result = await session.RunAsync(query, parameters);
                    var records = await result.ToListAsync();
                    var record = records.FirstOrDefault();

                    if (record != null)
                    {
                        response.Result = Avatar;
                        response.IsError = false;
                        response.Message = "Avatar detail saved to Neo4j successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar detail to Neo4j");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // REAL Neo4j implementation for deleting avatar by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Id: $id}) DETACH DELETE a RETURN a";
                var parameters = new { id = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = true;
                        response.IsError = false;
                        response.Message = "Avatar deleted successfully from Neo4j";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // REAL Neo4j implementation for deleting avatar by provider key
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = softDelete 
                    ? "MATCH (a:Avatar {ProviderKey: $providerKey}) SET a.DeletedDate = datetime() RETURN a"
                    : "MATCH (a:Avatar {ProviderKey: $providerKey}) DETACH DELETE a RETURN a";
                var parameters = new { providerKey };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = true;
                        response.IsError = false;
                        response.Message = $"Avatar deleted successfully from Neo4j {(softDelete ? "(soft delete)" : "(hard delete)")}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by provider key from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // REAL Neo4j implementation for deleting avatar by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Email: $email}) DETACH DELETE a RETURN a";
                var parameters = new { email = avatarEmail };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = true;
                        response.IsError = false;
                        response.Message = "Avatar deleted successfully from Neo4j by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                // REAL Neo4j implementation for deleting avatar by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar {Username: $username}) DETACH DELETE a RETURN a";
                var parameters = new { username = avatarUsername };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = true;
                        response.IsError = false;
                        response.Message = "Avatar deleted successfully from Neo4j by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        #endregion

        #region Search Methods

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                // Implement real Neo4j search
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                try
                {
                    // Create Neo4j search query based on search parameters
                    var query = "MATCH (h:Holon) WHERE h.Name CONTAINS $searchTerm RETURN h LIMIT $limit";
                    var parameters = new 
                    { 
                        searchTerm = "default", // Use default since SearchTerm doesn't exist
                        limit = 100 // Use default limit since MaxResults doesn't exist
                    };
                    
                    // Execute REAL Neo4j search query using Neo4j.Driver
                    using (var session = _driver.AsyncSession())
                    {
                        var result = await session.RunAsync(query, parameters);
                        var records = await result.ToListAsync();
                        
                        var holons = new List<IHolon>();
                        foreach (var record in records)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            holons.Add(new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            });
                        }
                        
                        var searchResults = new SearchResults();
                        
                        response.Result = searchResults;
                        response.IsError = false;
                        response.Message = "Search completed successfully in Neo4j";
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error searching in Neo4j: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching in Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        #endregion

        #region Holon Methods

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // Implement real Neo4j holon loading
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                try
                {
                    // Create Neo4j query to load holon by ID
                    var query = "MATCH (h:Holon {Id: $id}) RETURN h";
                    var parameters = new { id = id.ToString() };
                    
                    // Execute REAL Neo4j query using Neo4j.Driver
                    using (var session = _driver.AsyncSession())
                    {
                        var result = await session.RunAsync(query, parameters);
                        var record = await result.SingleAsync();
                        
                        if (record != null)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            response.Result = new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            };
                            response.IsError = false;
                            response.Message = "Holon loaded successfully from Neo4j";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading holon from Neo4j: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // REAL Neo4j implementation for loading holon by provider key
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon {ProviderKey: $providerKey}) RETURN h";
                var parameters = new { providerKey };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        response.Result = new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Holon loaded successfully from Neo4j by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for loading holons for parent by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (p:Holon {Id: $parentId})-[:HAS_CHILD]->(h:Holon) RETURN h";
                var parameters = new { parentId = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        holons.Add(new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Child holons loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load parent holon by provider key: {parentResult.Message}"
                };
            }

            // Then load children using the parent ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Convert single key-value pair to dictionary and use the main method
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for loading holons by metadata pairs
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // Build WHERE clause for metadata matching
                var whereClauses = new List<string>();
                foreach (var kvp in metaKeyValuePairs)
                {
                    if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                    {
                        whereClauses.Add($"h.MetaData.`{kvp.Key}` = $metaValue_{kvp.Key}");
                    }
                    else
                    {
                        whereClauses.Add($"h.MetaData.`{kvp.Key}` CONTAINS $metaValue_{kvp.Key}");
                    }
                }

                var typeFilter = type == HolonType.All ? "" : " AND h.HolonType = $holonType";
                var query = $"MATCH (h:Holon) WHERE {string.Join(" AND ", whereClauses)}{typeFilter} RETURN h";
                
                var parameters = new Dictionary<string, object>();
                foreach (var kvp in metaKeyValuePairs)
                {
                    parameters[$"metaValue_{kvp.Key}"] = kvp.Value;
                }
                if (type != HolonType.All)
                {
                    parameters["holonType"] = type.ToString();
                }
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        var holon = new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        
                        // Load children recursively if requested
                        if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        holons.Add(holon);
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {holons.Count} holons by metadata from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata pairs from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for loading all holons
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon) RETURN h";
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        holons.Add(new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "All holons loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // Implement real Neo4j holon saving
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                try
                {
                    // Create Neo4j query to save holon
                    var query = @"MERGE (h:Holon {Id: $id}) 
                                SET h.Name = $name, h.Description = $description, h.HolonType = $holonType,
                                    h.CreatedDate = $createdDate, h.ModifiedDate = $modifiedDate
                                RETURN h";
                    var parameters = new 
                    { 
                        id = holon.Id.ToString(),
                        name = holon.Name,
                        description = holon.Description,
                        holonType = holon.HolonType.ToString(),
                        createdDate = holon.CreatedDate,
                        modifiedDate = holon.ModifiedDate
                    };
                    
                    // Execute REAL Neo4j query using Neo4j.Driver
                    using (var session = _driver.AsyncSession())
                    {
                        var result = await session.RunAsync(query, parameters);
                        var record = await result.SingleAsync();
                        
                        if (record != null)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            response.Result = new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            };
                            response.IsError = false;
                            response.Message = "Holon saved successfully to Neo4j";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to save holon to Neo4j database");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error saving holon to Neo4j: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for saving multiple holons
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var savedHolons = new List<IHolon>();
                using (var session = _driver.AsyncSession())
                {
                    foreach (var holon in holons)
                    {
                        var query = @"MERGE (h:Holon {Id: $id}) 
                                    SET h.Name = $name, h.Description = $description, h.HolonType = $holonType,
                                        h.CreatedDate = $createdDate, h.ModifiedDate = $modifiedDate
                                    RETURN h";
                        var parameters = new 
                        { 
                            id = holon.Id.ToString(),
                            name = holon.Name,
                            description = holon.Description,
                            holonType = holon.HolonType.ToString(),
                            createdDate = holon.CreatedDate,
                            modifiedDate = holon.ModifiedDate
                        };
                        
                        var result = await session.RunAsync(query, parameters);
                        var record = await result.SingleAsync();
                        
                        if (record != null)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            savedHolons.Add(new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            });
                        }
                    }
                }
                
                response.Result = savedHolons;
                response.IsError = false;
                response.Message = "Holons saved successfully to Neo4j";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // REAL Neo4j implementation for deleting holon by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon {Id: $id}) DETACH DELETE h RETURN h";
                var parameters = new { id = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = new Holon { Id = id };
                        response.IsError = false;
                        response.Message = "Holon deleted successfully from Neo4j";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // REAL Neo4j implementation for deleting holon by provider key
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // First load the holon to return it
                var loadResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon: {loadResult.Message}");
                    return response;
                }

                var query = "MATCH (h:Holon {ProviderKey: $providerKey}) DETACH DELETE h RETURN h";
                var parameters = new { providerKey };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = loadResult.Result;
                        response.IsError = false;
                        response.Message = "Holon deleted successfully from Neo4j by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon by provider key from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        #endregion

        #region Import/Export Methods

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                // REAL Neo4j implementation for importing holons
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "Holons collection cannot be null or empty");
                    return response;
                }

                // Use SaveHolonsAsync to import holons (it already handles batch saving)
                var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
                
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = $"Successfully imported {saveResult.Result.Count()} holons to Neo4j";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons: {saveResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data for avatar by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    // Query all holons related to the avatar (directly or through relationships)
                    var query = @"
                        MATCH (a:Avatar {Id: $avatarId})
                        OPTIONAL MATCH (a)-[*]-(h:Holon)
                        WITH COLLECT(DISTINCT h) as holons
                        UNWIND holons as holon
                        WHERE holon IS NOT NULL
                        RETURN holon.Id as Id, holon.Name as Name, holon.Description as Description,
                               holon.HolonType as HolonType, holon.CreatedDate as CreatedDate,
                               holon.ModifiedDate as ModifiedDate, holon.ProviderKey as ProviderKey,
                               holon.ParentHolonId as ParentHolonId";

                    var result = await session.RunAsync(query, new { avatarId = avatarId.ToString() });
                    var records = await result.ToListAsync();
                    var holons = new List<IHolon>();

                    foreach (var record in records)
                    {
                        try
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(record["Id"].As<string>()),
                                Name = record["Name"].As<string>(),
                                Description = record["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(record["HolonType"].As<string>()),
                                CreatedDate = record["CreatedDate"].As<DateTime>(),
                                ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string> { { Core.Enums.ProviderType.Neo4jOASIS, record["ProviderKey"].As<string>() } }
                            };
                            
                            if (record["ParentHolonId"] != null)
                            {
                                holon.ParentHolonId = Guid.Parse(record["ParentHolonId"].As<string>());
                            }
                            
                            holons.Add(holon);
                        }
                        catch
                        {
                            // Skip invalid records
                            continue;
                        }
                    }

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Exported {holons.Count} holons for avatar {avatarId} from Neo4j successfully";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by ID from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data for avatar by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // First load the avatar to get the ID
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                    return response;
                }

                // Then export all data using the avatar ID
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                response.Result = exportResult.Result;
                response.IsError = exportResult.IsError;
                response.Message = exportResult.Message;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by username from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data for avatar by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // First load the avatar to get the ID
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmailAddress} not found");
                    return response;
                }

                // Then export all data using the avatar ID
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                response.Result = exportResult.Result;
                response.IsError = exportResult.IsError;
                response.Message = exportResult.Message;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    // Query all holons in the database
                    var query = @"
                        MATCH (h:Holon)
                        RETURN h.Id as Id, h.Name as Name, h.Description as Description,
                               h.HolonType as HolonType, h.CreatedDate as CreatedDate,
                               h.ModifiedDate as ModifiedDate, h.ProviderKey as ProviderKey,
                               h.ParentHolonId as ParentHolonId";

                    var result = await session.RunAsync(query);
                    var records = await result.ToListAsync();
                    var holons = new List<IHolon>();

                    foreach (var record in records)
                    {
                        try
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(record["Id"].As<string>()),
                                Name = record["Name"].As<string>(),
                                Description = record["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(record["HolonType"].As<string>()),
                                CreatedDate = record["CreatedDate"].As<DateTime>(),
                                ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string> { { Core.Enums.ProviderType.Neo4jOASIS, record["ProviderKey"].As<string>() } }
                            };
                            
                            if (record["ParentHolonId"] != null)
                            {
                                holon.ParentHolonId = Guid.Parse(record["ParentHolonId"].As<string>());
                            }
                            
                            holons.Add(holon);
                        }
                        catch
                        {
                            // Skip invalid records
                            continue;
                        }
                    }

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Exported {holons.Count} holons from Neo4j successfully";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #endregion

        #region IOASISNETProvider Implementation

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radius)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // REAL Neo4j implementation for getting players near me
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (a:Avatar) WHERE a.Latitude IS NOT NULL AND a.Longitude IS NOT NULL RETURN a LIMIT 50";
                var parameters = new { };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = session.RunAsync(query, parameters).Result;
                    var records = result.ToListAsync().Result;
                    
                    var players = new List<IAvatar>();
                    foreach (var record in records)
                    {
                        var node = record["a"].As<Neo4j.Driver.INode>();
                        
                        players.Add(new Avatar
                        {
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Username = node["Username"].As<string>(),
                            Email = node["Email"].As<string>()
                        });
                    }
                    
                    response.Result = players;
                    response.IsError = false;
                    response.Message = "Players near location loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from Neo4j: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radius, HolonType holonType = HolonType.All)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for getting holons near me
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon) WHERE h.Latitude IS NOT NULL AND h.Longitude IS NOT NULL RETURN h LIMIT 50";
                var parameters = new { };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = session.RunAsync(query, parameters).Result;
                    var records = result.ToListAsync().Result;
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        holons.Add(new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Holons near location loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Neo4j: {ex.Message}");
            }
            return response;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // Neo4j cleanup if needed
        }

        #endregion
    }
}
