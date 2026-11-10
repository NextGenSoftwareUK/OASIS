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
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

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

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            LoadAvatarDetailBySelectorAsync("detail.GUId = $value", id.ToString());

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) =>
            LoadAvatarDetailBySelectorAsync("toLower(detail.Email) = toLower($value)", avatarEmail);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) =>
            LoadAvatarDetailBySelectorAsync("toLower(detail.Username) = toLower($value)", avatarUsername);

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailBySelectorAsync(string predicate, string value)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var sessionLifetime = session.ConfigureAwait(false);
                result.Result = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync($@"
                        MATCH (detail:AvatarDetail) WHERE {predicate}
                        RETURN detail.GUId AS guid, detail.Username AS username,
                               detail.Email AS email, detail.Address AS address, detail.Country AS country
                        LIMIT 2", new { value }).ConfigureAwait(false);
                    if (!await cursor.FetchAsync().ConfigureAwait(false))
                        return null;

                    var row = cursor.Current;
                    var detail = new AvatarDetail
                    {
                        Id = Guid.Parse(row["guid"].As<string>()),
                        Username = row["username"].As<string>(),
                        Email = row["email"].As<string>(),
                        Address = row["address"].As<string>(),
                        Country = row["country"].As<string>()
                    };
                    if (await cursor.FetchAsync().ConfigureAwait(false))
                        throw new InvalidOperationException("Multiple Aura avatar details match the requested identity.");

                    return detail;
                }).ConfigureAwait(false);
                result.IsLoaded = result.Result != null;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error loading Neo4j Aura avatar detail.", ex);
            }

            return result;
        }
    }
}
