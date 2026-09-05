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

    }
}
