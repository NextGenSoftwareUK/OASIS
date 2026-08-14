using System;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4jNode = Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura
{
    public partial class Neo4jOASIS
    {

        /*
        private async Task<OASISResult<bool>> ConnectAsync()
        {
            OASISResult<bool>
            try
            {
                Driver = GraphDatabase.Driver(Host, AuthTokens.Basic(Username, Password));

                await Driver.VerifyConnectivityAsync();
                return true;
            }
            catch (Exception ex)
            {
                string error = ex.ToString();
                return false;
            }
        }
        private async Task DisconnectAsync()
        {
            //TODO: Find if there is a disconnect/shutdown function?
            await Driver.CloseAsync();
            Driver = null;
        }*/

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                Driver = GraphDatabase.Driver(Host, AuthTokens.Basic(Username, Password));
                Driver.VerifyConnectivityAsync().Wait();

                result.Result = true;
                IsProviderActivated = true;
                //result = base.ActivateProvider();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknwon error occured whilst activating neo4j provider: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                Driver = GraphDatabase.Driver(Host, AuthTokens.Basic(Username, Password));
                await Driver.VerifyConnectivityAsync();
                //result = await base.ActivateProviderAsync();

                result.Result = true;
                IsProviderActivated = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknwon error occured whilst activating neo4j provider: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Driver != null)
                    Driver.CloseAsync().Wait();

                Driver = null;
                //result = base.DeActivateProvider();

                result.Result = true;
                IsProviderActivated = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknwon error occured whilst dactivating neo4j provider: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Driver != null)
                    await Driver.CloseAsync();
                
                Driver = null;
                //result = await base.DeActivateProviderAsync();

                result.Result = true;
                IsProviderActivated = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknwon error occured whilst dactivating neo4j provider: {ex}");
            }

            return result;
        }

        private static void WithDatabase(SessionConfigBuilder sessionConfigBuilder)
        {
            //var neo4jVersion = System.Environment.GetEnvironmentVariable("NEO4J_VERSION") ?? "";
            //if (!neo4jVersion.StartsWith("4"))
            //{
            //    return;
            //}
            sessionConfigBuilder.WithDatabase("neo4j");
        }
        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Neo4j: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Neo4j: {ex.Message}", ex);
            }
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // Neo4j provider does not generate native code from STAR metadata.
            return true;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (av:Avatar)                        
                            RETURN av.FirstName AS firstname,av.LastName AS lastname"
                    );

                    IEnumerable<IAvatar> objList = await cursor.ToListAsync(record => new Avatar
                    {
                        FirstName = record["firstname"].As<string>(),
                        LastName = record["lastname"].As<string>()
                    });

                    return new OASISResult<IEnumerable<IAvatar>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av:Avatar)                        
                            RETURN av.FirstName AS firstname,av.LastName AS lastname"
                    );

                    IEnumerable<IAvatar> objList = (from d in cursor
                                                    select new Avatar
                                                    {
                                                        FirstName = d["firstname"].As<string>(),
                                                        LastName = d["lastname"].As<string>()
                                                    }).ToList();

                    return new OASISResult<IEnumerable<IAvatar>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av:Avatar)
                            WHERE TOLOWER(av.username) CONTAINS TOLOWER($UserName)
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { UserName = avatarUsername }
                    );

                    IAvatar obj = (from d in cursor
                                   select new Avatar
                                   {
                                       FirstName = d["firstname"].As<string>(),
                                       LastName = d["lastname"].As<string>()
                                   }).FirstOrDefault();


                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (av:Avatar)
                            WHERE av.GUId=$guid
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { guid = Id.ToString() }
                    );

                    var avList = await cursor.ToListAsync(record => new Avatar
                    {
                        FirstName = record["firstname"].As<string>(),
                        LastName = record["lastname"].As<string>()
                    });
                    IAvatar objAv = new Avatar();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (av:Avatar)
                            WHERE TOLOWER(av.EMail) CONTAINS TOLOWER($email)
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { email = avatarEmail }
                    );

                    var avList = await cursor.ToListAsync(record => new Avatar
                    {
                        FirstName = record["firstname"].As<string>(),
                        LastName = record["lastname"].As<string>()
                    });
                    IAvatar objAv = new Avatar();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (av:Avatar)
                            WHERE TOLOWER(av.username) CONTAINS TOLOWER($UserName)
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { UserName = avatarUsername }
                    );

                    var avList = await cursor.ToListAsync(record => new Avatar
                    {
                        FirstName = record["firstname"].As<string>(),
                        LastName = record["lastname"].As<string>()
                    });
                    IAvatar objAv = new Avatar();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av:Avatar)
                            WHERE av.GUId=$guid
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { guid = Id.ToString() }
                    );

                    IAvatar obj = (from d in cursor
                                   select new Avatar
                                   {
                                       FirstName = d["firstname"].As<string>(),
                                       LastName = d["lastname"].As<string>()
                                   }).FirstOrDefault();


                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

    }
}
