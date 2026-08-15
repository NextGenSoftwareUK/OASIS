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

    }
}
