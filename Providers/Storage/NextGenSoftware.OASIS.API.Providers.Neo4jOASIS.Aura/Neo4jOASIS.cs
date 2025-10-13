using System;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
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

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura
{
    public class Neo4jOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsVersionControlEnabled { get; set; } = false;

        public IDriver Driver { get; set; }

        public Neo4jOASIS(string host, string username, string password)
        {
            this.ProviderName = "Neo4jOASIS";
            this.ProviderDescription = "Neo4j Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            Host = host;
            Username = username;
            Password = password;
        }

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
                    OASISErrorHandling.HandleError(ref result, "Neo4j provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Neo4j provider is not activated");
                    return result;
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

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
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

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av:Avatar)
                            WHERE TOLOWER(av.EMail) CONTAINS TOLOWER($email)
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { email = avatarEmail }
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

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    try
        //    {
        //        var session = Driver.AsyncSession(WithDatabase);

        //        return await session.ReadTransactionAsync(async transaction =>
        //        {
        //            var cursor = await transaction.RunAsync(@"
        //                    MATCH (av:Avatar)
        //                    WHERE TOLOWER(av.username) CONTAINS TOLOWER($userName)
        //                        AND TOLOWER(av.password) CONTAINS TOLOWER($Password)
        //                    RETURN av.FirstName AS firstname,av.LastName AS lastname",
        //                new { userName = username, Password = password }
        //            );

        //            var avList = await cursor.ToListAsync(record => new Avatar
        //            {
        //                FirstName = record["firstname"].As<string>(),
        //                LastName = record["lastname"].As<string>()
        //            });
        //            IAvatar objAv = new Avatar();
        //            if (avList != null)
        //            {
        //                if (avList.Count > 0)
        //                {
        //                    objAv = avList[0];
        //                }
        //            }

        //            return new OASISResult<IAvatar>
        //            {
        //                IsLoaded = true,
        //                IsError = false,
        //                Message = "Avatar Loaded Successfully",
        //                Result = objAv
        //            };
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return new OASISResult<IAvatar>
        //        {
        //            IsLoaded = false,
        //            IsError = true,
        //            Message = ex.ToString(),
        //        };
        //    }
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    try
        //    {
        //        var session = Driver.Session(WithDatabase);

        //        return session.ReadTransaction(transaction =>
        //        {
        //            var cursor = transaction.Run(@"
        //                    MATCH (av:Avatar)
        //                    WHERE TOLOWER(av.username) CONTAINS TOLOWER($userName)
        //                        AND TOLOWER(av.password) CONTAINS TOLOWER($Password)
        //                    RETURN av.FirstName AS firstname,av.LastName AS lastname",
        //                new { userName = username, Password = password }
        //            );

        //            IAvatar obj = (from d in cursor
        //                           select new Avatar
        //                           {
        //                               FirstName = d["firstname"].As<string>(),
        //                               LastName = d["lastname"].As<string>()
        //                           }).FirstOrDefault();


        //            return new OASISResult<IAvatar>
        //            {
        //                IsLoaded = true,
        //                IsError = false,
        //                Message = "Avatar Loaded successfully",
        //                Result = obj,
        //            };
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return new OASISResult<IAvatar>
        //        {
        //            IsLoaded = false,
        //            IsError = true,
        //            Message = ex.ToString(),
        //        };
        //    }
        //}

        /*
        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
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
                        new { UserName = username }
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (av:Avatar)
                            WHERE TOLOWER(av.EMail) CONTAINS TOLOWER($username)
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { username = username }
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
                        IsError = false,
                        IsLoaded = true,
                        Message = "Avatar Loaded Successfully",
                        Result = objAv,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    IsLoaded = false,
                    Message = ex.ToString(),
                };
            }
        }*/

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (av:Avatar)
                            WHERE av.ProviderKey=$pkey
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { pkey = providerKey }
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
                        IsError = false,
                        IsLoaded = true,
                        Message = "Avatar Loaded Successfully",
                        Result = objAv,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    IsLoaded = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av:Avatar)
                            WHERE av.ProviderKey=$pkey
                            RETURN av.FirstName AS firstname,av.LastName AS lastname",
                        new { pkey = providerKey }
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

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (avd:AvatarDetail)
                            WHERE avd.GUId=$guid
                            RETURN avd.Username AS username,avd.Email AS email",
                        new { guid = id.ToString() }
                    );

                    IAvatarDetail obj = (from d in cursor
                                         select new AvatarDetail
                                         {
                                             Username = d["username"].As<string>(),
                                             Email = d["email"].As<string>()
                                         }).FirstOrDefault();


                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (avd:AvatarDetail)
                            WHERE TOLOWER(avd.Email) CONTAINS TOLOWER($email)
                            RETURN avd.Username AS username,avd.Email AS email",
                        new { email = avatarEmail }
                    );

                    IAvatarDetail obj = (from d in cursor
                                         select new AvatarDetail
                                         {
                                             Username = d["username"].As<string>(),
                                             Email = d["email"].As<string>()
                                         }).FirstOrDefault();


                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (avd:AvatarDetail)
                            WHERE TOLOWER(avd.Username) CONTAINS TOLOWER($UserName)
                            RETURN avd.Username AS username, avd.Email As Email",
                        new { UserName = avatarUsername }
                    );

                    IAvatarDetail obj = (from d in cursor
                                         select new AvatarDetail
                                         {
                                             Username = d["username"].As<string>(),
                                             Email = d["email"].As<string>()
                                         }).FirstOrDefault();


                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (avd:AvatarDetail)
                            WHERE avd.GUId=$guid
                            RETURN avd.USername AS username,avd.Email AS email",
                        new { guid = id.ToString() }
                    );

                    var avList = await cursor.ToListAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>(),
                        Email = record["email"].As<string>()
                    });
                    IAvatarDetail objAv = new AvatarDetail();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IAvatarDetail>
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
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (avd:AvatarDetail)
                            WHERE TOLOWER(avd.Username) CONTAINS TOLOWER($UserName)
                            RETURN avd.Username AS username,avd.Email AS email",
                        new { UserName = avatarUsername }
                    );

                    var avList = await cursor.ToListAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>(),
                        Email = record["email"].As<string>()
                    });
                    IAvatarDetail objAv = new AvatarDetail();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (avd:AvatarDetail)
                            WHERE TOLOWER(avd.Email) CONTAINS TOLOWER($email)
                            RETURN avd.Username AS username,avd.Email AS email",
                        new { email = avatarEmail }
                    );

                    var avList = await cursor.ToListAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>(),
                        Email = record["Email"].As<string>()
                    });
                    IAvatarDetail objAv = new AvatarDetail();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (avd:AvatarDetail)                        
                            RETURN avd.Username AS username,avd.Email AS email"
                    );

                    IEnumerable<IAvatarDetail> objList = (from d in cursor
                                                          select new AvatarDetail
                                                          {
                                                              Username = d["username"].As<string>(),
                                                              Email = d["email"].As<string>()
                                                          }).ToList();

                    return new OASISResult<IEnumerable<IAvatarDetail>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (avd:AvatarDetail)                        
                            RETURN avd.Username AS username,avd.Email AS email"
                    );

                    IEnumerable<IAvatarDetail> objList = await cursor.ToListAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>(),
                        Email = record["email"].As<string>()
                    });

                    return new OASISResult<IEnumerable<IAvatarDetail>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var avatarList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Avatar {name: $name})
                            SET p.EMail = $eMail,p.FirstName = $firstName,p.LastName = $lastName
                            RETURN p.LastName as lastname",
                        new
                        {
                            name = Avatar.Title,
                            eMail = Avatar.Email,
                            firstName = Avatar.FirstName,
                            lastName = Avatar.LastName,
                        }
                    );

                    var avList = (from d in cursor
                                  select new Avatar
                                  {
                                      LastName = d["lastname"].As<string>()
                                  }).ToList();
                    return avList;
                });

                if (avatarList != null)
                {
                    if (avatarList.Count > 0)
                    {
                        return new OASISResult<IAvatar>
                        {
                            IsSaved = true,
                            IsError = false,
                            Message = "Record updated successfully",
                        };
                    }
                }

                return session.WriteTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MERGE (p1:Avatar { name:$Name, FirstName: $firstName ,
                                                LastName:$lastName,EMail:$eMail,
                                                username:$userName,password:$Password
                                                p.GUId=$guid})                        
                            RETURN p1.name as name",
                        new
                        {
                            Name = Avatar.Title,
                            firstName = Avatar.FirstName,
                            lastName = Avatar.LastName,
                            eMail = Avatar.Email,
                            userName = Avatar.Username,
                            Password = Avatar.Password,
                            guid = Avatar.AvatarId,
                            //pkey = Avatar.ProviderKey,
                        }
                    );

                    IAvatar objAv = (from d in cursor
                                     select new Avatar
                                     { FirstName = d["name"].As<string>() }).FirstOrDefault();


                    return new OASISResult<IAvatar>
                    {
                        IsSaved = true,
                        IsError = false,
                        Message = objAv.FirstName + " Record saved successfully",
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                if (Avatar.AvatarId == Guid.Empty)
                    Avatar.AvatarId = Guid.NewGuid();

                Avatar.CreatedProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
                var avatarList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Avatar {name: $name})
                            SET p.EMail = $eMail,p.FirstName = $firstName,p.LastName = $lastName
                                
                            RETURN p.LastName as lastname",
                        new
                        {
                            name = Avatar.Title,
                            eMail = Avatar.Email,
                            firstName = Avatar.FirstName,
                            lastName = Avatar.LastName,

                        }
                    );

                    return await cursor.ToListAsync(record => new Avatar
                    {
                        LastName = record["lastname"].As<string>()
                    });
                });

                if (avatarList != null)
                {
                    if (avatarList.Count > 0)
                    {
                        return new OASISResult<IAvatar>
                        {
                            IsSaved = true,
                            IsError = false,
                            Message = "Record updated successfully",
                        };
                    }
                }

                return await session.WriteTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MERGE (p1:Avatar { name:$Name, FirstName: $firstName ,
                                                LastName:$lastName,EMail:$eMail,
                                                username:$userName,password:$Password,
                                                GUId:$guid})                        
                            RETURN p1.name as name, p1.id as id",
                        new
                        {
                            Name = Avatar.Title,
                            firstName = Avatar.FirstName,
                            lastName = Avatar.LastName,
                            eMail = Avatar.Email,
                            userName = Avatar.Username,
                            Password = Avatar.Password,
                            guid = Avatar.AvatarId.ToString(),
                        }
                    );

                    IAvatar objAv = await cursor.SingleAsync(record => new Avatar
                    {
                        Title = record["id"].As<string>(),
                        FirstName = record["name"].As<string>()
                    });
                    //objAv.ProviderKey[Core.Enums.ProviderType.Neo4jOASIS] = objAv.Title;
                    //session.Dispose();
                    return new OASISResult<IAvatar>
                    {
                        IsSaved = true,
                        IsError = false,
                        Message = objAv.FirstName + " Record saved successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail AvatarDetail)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var avatarList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                              MATCH (p:AvatarDetail {Username: $username})
                            SET p.Address = $address,p.Country = $country,p.Email =$email     
                            RETURN p.Username as username",
                        new
                        {
                            username = AvatarDetail.Username,
                            // guid = AvatarDetail.Id.ToString(),
                            email = AvatarDetail.Email,
                            address = AvatarDetail.Address,
                            //attributes = Avatar.Attributes,
                            country = AvatarDetail.Country
                        }
                    );

                    var avList = (from d in cursor
                                  select new AvatarDetail
                                  {
                                      Username = d["username"].As<string>()
                                  }).ToList();
                    return avList;
                });

                if (avatarList != null)
                {
                    if (avatarList.Count > 0)
                    {
                        return new OASISResult<IAvatarDetail>
                        {
                            IsSaved = true,
                            IsError = false,
                            Message = "Record updated successfully",
                        };
                    }
                }

                return session.WriteTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                             MERGE (avd:AvatarDetail { Username: $username, GUId: $guid,
                                                Email:$email,Address:$address,
                                                Country:$country
                                                })                        
                            RETURN avd.Username as username",
                        new
                        {
                            country = AvatarDetail.Country,
                            address = AvatarDetail.Address,
                            email = AvatarDetail.Email,
                            username = AvatarDetail.Username,
                            guid = AvatarDetail.Id.ToString(),
                        }
                    );

                    IAvatarDetail objAv = (from d in cursor
                                           select new AvatarDetail
                                           { Username = d["username"].As<string>() }).FirstOrDefault();


                    return new OASISResult<IAvatarDetail>
                    {
                        IsSaved = true,
                        IsError = false,
                        Message = objAv.Username + " Record saved successfully",
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail AvatarDetail)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                if (AvatarDetail.Id == Guid.Empty)
                    AvatarDetail.Id = Guid.NewGuid();

                AvatarDetail.CreatedProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
                var avatarList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:AvatarDetail {Username: $username})
                            SET p.Address = $address,p.Country = $country
                                
                            RETURN p.Username as username",
                        new
                        {
                            username = AvatarDetail.Username,
                            address = AvatarDetail.Address,
                            // attributes = AvatarDetail.Attributes,
                            country = AvatarDetail.Country,
                        }
                    );

                    return await cursor.ToListAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>()
                    });
                });

                if (avatarList != null)
                {
                    if (avatarList.Count > 0)
                    {
                        return new OASISResult<IAvatarDetail>
                        {
                            IsSaved = true,
                            IsError = false,
                            Message = "Record updated successfully",
                        };
                    }
                }

                return await session.WriteTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MERGE (p1:AvatarDetail { Username: $username, GUId: $guid,
                                                Email:$email,Address:$address,
                                                Country:$country
                                                })                        
                            RETURN p1.Username as username",
                        new
                        {
                            username = AvatarDetail.Username,
                            guid = AvatarDetail.Id.ToString(),
                            email = AvatarDetail.Email,
                            address = AvatarDetail.Address,
                            //attributes = Avatar.Attributes,
                            country = AvatarDetail.Country,
                        }
                    );

                    IAvatarDetail objAv = await cursor.SingleAsync(record => new AvatarDetail
                    {
                        Username = record["username"].As<string>()
                    });

                    //session.Dispose();
                    return new OASISResult<IAvatarDetail>
                    {
                        IsSaved = true,
                        IsError = false,
                        Message = " Record saved successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var avatarList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Avatar {id: $Id})
                            DELETE p",
                        new
                        {
                            Id = id,
                        }
                    );

                    return (from d in cursor
                            select new Avatar
                            { LastName = d["lastname"].As<string>() }).ToList();
                });

                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var avatarList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Avatar {EMail: $eMail})
                            DELETE p",
                        new
                        {
                            eMail = avatarEmail,
                        }
                    );

                    return (from d in cursor
                            select new Avatar
                            { LastName = d["lastname"].As<string>() }).ToList();
                });

                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var avatarList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Avatar {username: $username})
                            DELETE p",
                        new
                        {
                            username = avatarUsername,
                        }
                    );

                    return (from d in cursor
                            select new Avatar
                            { LastName = d["lastname"].As<string>() }).ToList();
                });


                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var avatarList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Avatar {id: $Id})
                            DELETE p",
                        new
                        {
                            Id = id,
                        }
                    );

                    return await cursor.ToListAsync(record => new Avatar
                    {
                        LastName = record["lastname"].As<string>()
                    });
                });


                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }

        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var avatarList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Avatar {EMail: $eMail})
                            DELETE p",
                        new
                        {
                            eMail = avatarEmail,
                        }
                    );

                    return await cursor.ToListAsync(record => new Avatar
                    {
                        LastName = record["lastname"].As<string>()
                    });
                });


                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }

        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var avatarList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Avatar {username: $username})
                            DELETE p",
                        new
                        {
                            username = avatarUsername,
                        }
                    );

                    return await cursor.ToListAsync(record => new Avatar
                    {
                        LastName = record["lastname"].As<string>()
                    });
                });


                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted successfuly",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! Please try again after sometime",
                        Result = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }

        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var avatarList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Avatar {ProviderKey: $ProviderKey})
                            DELETE p",
                        new
                        {
                            ProviderKey = providerKey,
                        }
                    );

                    return (from d in cursor
                            select new Avatar
                            { LastName = d["lastname"].As<string>() }).ToList();
                });

                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var avatarList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Avatar {ProviderKey: $providerkey})
                            DELETE p",
                        new
                        {
                            providerkey = providerKey,
                        }
                    );

                    return await cursor.ToListAsync(record => new Avatar
                    {
                        LastName = record["lastname"].As<string>()
                    });
                });


                if (avatarList.Count <= 0)
                {
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted successfuly",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! Please try again after sometime",
                        Result = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }


        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av: Holon)
                            WHERE av.GUId=$guid
                            RETURN av.description AS description,av.ProviderKey AS providerkey, av.PreviousVersionId AS previousversionid",
                        new { guid = id.ToString() }
                    );

                    IHolon obj = (from d in cursor
                                  select new Holon
                                  {
                                      Description = d["decription"].As<string>(),
                                      //ProviderKey = d["providerkey"].As<Dictionary<ProviderType, string>>(),
                                      PreviousVersionId = d["previousversionid"].As<Guid>()
                                  }).FirstOrDefault();


                    return new OASISResult<IHolon>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                           MATCH (hl: Holon)
                            WHERE hl.GUId=$guid
                            RETURN hl.description AS description,hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid",
                        new { guid = id.ToString() }
                    );

                    var avList = await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>(),
                        //ProviderKey = record["providerkey"].As<Dictionary<ProviderType, string>>(),
                        PreviousVersionId = record["previousversionid"].As<Guid>()
                    });
                    IHolon objAv = new Holon();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IHolon>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av: Holon)
                            WHERE av.ProviderKey=$providerkey
                            RETURN av.description AS description,av.ProviderKey AS providerkey, av.PreviousVersionId AS previousversionid",
                        new { providerkey = providerKey }
                    );

                    IHolon obj = (from d in cursor
                                  select new Holon
                                  {
                                      Description = d["decription"].As<string>(),
                                      //ProviderKey = d["providerkey"].As<Dictionary<ProviderType, string>>(),
                                      PreviousVersionId = d["previousversionid"].As<Guid>()
                                  }).FirstOrDefault();


                    return new OASISResult<IHolon>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded successfully",
                        Result = obj,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                           MATCH (hl: Holon)
                            WHERE hl.ProviderKey=$providerkey
                            RETURN hl.description AS description,hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid",
                        new { providerkey = providerKey }
                    );

                    var avList = await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>(),
                        //ProviderKey = record["providerkey"].As<Dictionary<ProviderType, string>>(),
                        PreviousVersionId = record["previousversionid"].As<Guid>()
                    });
                    IHolon objAv = new Holon();
                    if (avList != null)
                    {
                        if (avList.Count > 0)
                        {
                            objAv = avList[0];
                        }
                    }

                    return new OASISResult<IHolon>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded Successfully",
                        Result = objAv
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av: Holon)
                            WHERE av.GUId=$guid
                            RETURN av.description AS description,av.ProviderKey AS providerkey, av.PreviousVersionId AS previousversionid",
                        new { guid = id.ToString() }
                    );

                    IEnumerable<IHolon> avList = (from d in cursor
                                                  select new Holon
                                                  {
                                                      Description = d["description"].As<string>(),
                                                      //ProviderKey = d["providerkey"].As<Dictionary<ProviderType, string>>(),
                                                      PreviousVersionId = d["previousversionid"].As<Guid>()
                                                  }).ToList();


                    return new OASISResult<IEnumerable<IHolon>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded successfully",
                        Result = avList,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                           MATCH (hl: Holon)
                            WHERE hl.GUId=$guid
                            RETURN hl.description AS description,hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid",
                        new { guid = id.ToString() }
                    );

                    var avList = await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>(),
                        //ProviderKey = record["providerkey"].As<Dictionary<ProviderType, string>>(),
                        PreviousVersionId = record["previousversionid"].As<Guid>()
                    });


                    return new OASISResult<IEnumerable<IHolon>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded Successfully",
                        Result = avList
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (av: Holon)
                            WHERE av.ProviderKey=$prodviderkey
                            RETURN av.description AS description,av.ProviderKey AS providerkey, av.PreviousVersionId AS previousversionid",
                        new { prodviderkey = providerKey }
                    );

                    IEnumerable<IHolon> avList = (from d in cursor
                                                  select new Holon
                                                  {
                                                      Description = d["description"].As<string>(),
                                                      //ProviderKey = d["providerkey"].As<Dictionary<ProviderType, string>>(),
                                                      PreviousVersionId = d["previousversionid"].As<Guid>()
                                                  }).ToList();


                    return new OASISResult<IEnumerable<IHolon>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded successfully",
                        Result = avList,
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                           MATCH (hl: Holon)
                            WHERE hl.ProviderKey=$providerkey
                            RETURN hl.description AS description,hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid",
                        new { providerkey = providerKey }
                    );

                    var avList = await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>(),
                        //ProviderKey = record["providerkey"].As<Dictionary<ProviderType, string>>(),
                        PreviousVersionId = record["previousversionid"].As<Guid>()
                    });


                    return new OASISResult<IEnumerable<IHolon>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded Successfully",
                        Result = avList
                    };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                return session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (hl: Holon)
                            RETURN hl.description AS description,hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid"
                    );

                    IEnumerable<IHolon> objList = (from d in cursor
                                                   select new Holon
                                                   {
                                                       Description = d["description"].As<string>(),
                                                       //ProviderKey = d["providerkey"].As<Dictionary<ProviderType, string>>(),
                                                       PreviousVersionId = d["previousversionid"].As<Guid>()
                                                   }).ToList();

                    return new OASISResult<IEnumerable<IHolon>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                          MATCH (hl: Holon)
                            RETURN hl.description AS description,hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid"
                    );

                    IEnumerable<IHolon> objList = await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>(),
                        //ProviderKey = record["providerkey"].As<Dictionary<ProviderType, string>>(),
                        PreviousVersionId = record["previousversionid"].As<Guid>()
                    });

                    return new OASISResult<IEnumerable<IHolon>>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon(s) Loaded successfully",
                        Result = objList,
                    };

                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var holonList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Holon {name: $name})
                            SET p.description = $Description,p.Version = $version,p.Id = $Id,
                            p.PreviousVersionId = $PreviousVersionId
                            RETURN p.name as name",
                        new
                        {
                            name = holon.Name,
                            Description = holon.Description,
                            version=holon.Version,
                            PreviousVersionId = holon.PreviousVersionId.ToString(),
                            Id=holon.Id.ToString()
                        }
                    );

                    return (from d in cursor
                            select new Holon
                            {
                                Name = d["name"].As<string>()
                            }).ToList();
                });

                if (holonList != null)
                {
                    if (holonList.Count > 0)
                    {
                        OASISResult<IHolon> result = new OASISResult<IHolon>
                        {
                            IsError = false,
                            IsSaved = true,
                            Message = "Record updated successfully"
                        };
                        return result;
                    }
                }

                return session.WriteTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MERGE (p1:Holon { name:$Name, Description: $description ,
                                                version:$version,PreviousVersionId:$PreviousVersionId
                                                })                        
                            RETURN p1.name as name",
                        new
                        {
                            name = holon.Name,
                            Description = holon.Description,                            
                            PreviousVersionId = holon.PreviousVersionId.ToString(),
                            version=holon.Version
                        }
                    );

                    var hol = (from d in cursor
                               select new Holon
                               {
                                   Name = d["name"].As<string>(),
                               }).FirstOrDefault();

                    return new OASISResult<IHolon>
                    { IsError = false, Result = hol, IsSaved = true };
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    IsSaved = false,
                    Message = ex.ToString()
                };
            }
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var holonList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Holon {name: $name})
                            SET p.description = $Description,
                            p.PreviousVersionId = $PreviousVersionId
                            RETURN p.name as name",
                        new
                        {
                            name = holon.Name,
                            Description = holon.Description,
                            //ProviderKey = holon.ProviderKey,
                            PreviousVersionId = holon.PreviousVersionId,
                        }
                    );

                    return await cursor.ToListAsync(record => new Holon
                    {
                        Name = record["name"].As<string>()
                    });
                });

                if (holonList != null)
                {
                    if (holonList.Count > 0)
                    {
                        OASISResult<IHolon> result = new OASISResult<IHolon>
                        {
                            IsError = false,
                            Message = "Record updated successfully"
                        };
                        return result;
                    }
                }

                return await session.WriteTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MERGE (p1:Avatar { name:$Name, Description: $Description ,
                                                PreviousVersionId:$PreviousVersionId
                                                })                        
                            RETURN p1.name as name",
                        new
                        {
                            Name = holon.Name,
                            Description = holon.Description,
                            //ProviderKey = holon.ProviderKey,
                            PreviousVersionId = holon.PreviousVersionId,
                        }
                    );

                    return await cursor.SingleAsync(record => new OASISResult<IHolon>
                    {
                        IsError = false,
                        Message = record["name"].As<string>()
                    });
                });
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            foreach (var item in holons)
            {
                SaveHolon(item);
            }
            return null;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            foreach (var item in holons)
            {
                SaveHolonAsync(item).Wait();
            }
            return null;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var holonList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Holon {ProviderKey: $Id})
                            DELETE p",
                        new
                        {
                            Id = id.ToString(),
                        }
                    );

                    return (from d in cursor
                            select new Holon
                            { Description = d["description"].As<string>() }).ToList();
                });

                if (holonList.Count <= 0)
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        Message = "Holon Deleted Successfully",
                        IsDeleted = true,
                        DeletedCount = 1
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later"
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }

        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var holonList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Holon {Id: $guid})
                            DELETE p",
                        new
                        {
                            guid = id.ToString(),
                        }
                    );

                    return await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>()
                    });
                });


                if (holonList.Count <= 0)
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        IsDeleted = true,
                        DeletedCount = 1,
                        Message = "Holon Deleted successfuly"
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = true,
                        Message = "Something went wrong! Please try again after sometime"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            try
            {
                var session = Driver.Session(WithDatabase);

                var holonList = session.ReadTransaction(transaction =>
                {
                    var cursor = transaction.Run(@"
                            MATCH (p:Holon {ProviderKey: $providerkey})
                            DELETE p",
                        new
                        {
                            providerkey = providerKey,
                        }
                    );

                    return (from d in cursor
                            select new Holon
                            { Description = d["description"].As<string>() }).ToList();
                });

                if (holonList.Count <= 0)
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        IsDeleted = true,
                        DeletedCount = 1,
                        Message = "Holon Deleted Successfully"
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later"
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            try
            {
                var session = Driver.AsyncSession(WithDatabase);

                var holonList = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                            MATCH (p:Holon {ProviderKey: $providerkey})
                            DELETE p",
                        new
                        {
                            providerkey = providerKey,
                        }
                    );

                    return await cursor.ToListAsync(record => new Holon
                    {
                        Description = record["description"].As<string>()
                    });
                });


                if (holonList.Count <= 0)
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        Message = "Holon Deleted successfuly",
                        IsDeleted = true,
                        DeletedCount = 1
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = true,
                        Message = "Something went wrong! Please try again after sometime"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                // Search using Neo4j Aura database
                var searchResults = new SearchResults();
                
                // Search avatars
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var avatarsResult = await LoadAllAvatarsAsync();
                    if (!avatarsResult.IsError && avatarsResult.Result != null)
                    {
                        foreach (var avatar in avatarsResult.Result)
                        {
                            // TODO: Implement proper search result creation
                            // searchResults.Add(new SearchResult
                            // {
                            //     ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage),
                            //     ProviderType = new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),
                            //     Id = avatar.Id,
                            //     Name = avatar.Username,
                            //     Description = avatar.Description,
                            //     Result = avatar,
                            //     IsError = false
                            // });
                        }
                    }
                }
                
                // Search holons
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var holonsResult = await LoadAllHolonsAsync();
                    if (!holonsResult.IsError && holonsResult.Result != null)
                    {
                        foreach (var holon in holonsResult.Result)
                        {
                            // TODO: Implement proper search result creation
                            // searchResults.Add(new SearchResult
                            // {
                            //     ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage),
                            //     ProviderType = new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),
                            //     Id = holon.Id,
                            //     Name = holon.Name,
                            //     Description = holon.Description,
                            //     Result = holon,
                            //     IsError = false
                            // });
                        }
                    }
                }
                
                result.Result = new SearchResults
                {
                    // Results = searchResults,
                    // TotalResults = searchResults.Count,
                    // IsError = false
                };
                result.IsError = false;
                result.Message = $"Successfully searched Neo4j Aura database and found results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching Neo4j Aura database: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                    importedCount++;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                // Export all holons for avatar from Neo4j Aura
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar from Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                // Load avatar by username first
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Export all holons for this avatar
                    var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                    if (holonsResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                        return result;
                    }

                    result.Result = holonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Neo4j Aura";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Export all holons for this avatar
                    var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                    if (holonsResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                        return result;
                    }

                    result.Result = holonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Neo4j Aura";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                // Load holons by metadata from Neo4j Aura database
                var holons = await _holonRepository.GetByMetaDataAsync(metaKey, metaValue);
                if (holons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {holons.Message}");
                    return result;
                }

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Result?.Count() ?? 0} holons by metadata from Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura provider is not activated");
                    return result;
                }

                // Load holons by multiple metadata pairs from Neo4j Aura database
                var holons = await _holonRepository.GetByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode);
                if (holons.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs: {holons.Message}");
                    return result;
                }

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Result?.Count() ?? 0} holons by metadata pairs from Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }
    }
}
