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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) =>
            SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                var id = avatar.AvatarId;
                if (id == Guid.Empty)
                {
                    var providerKey = avatar.ProviderUniqueStorageKey?.GetValueOrDefault(Core.Enums.ProviderType.Neo4jOASIS)
                        ?? avatar.ProviderUniqueStorageKey?.Values?.FirstOrDefault()
                        ?? avatar.Username
                        ?? $"Neo4jOASIS:{avatar.CreatedDate.Ticks}";
                    id = CreateDeterministicGuid($"{Core.Enums.ProviderType.Neo4jOASIS}:{providerKey}");
                }

                var session = Driver.AsyncSession(WithDatabase);
                try
                {
                    await session.WriteTransactionAsync(async transaction =>
                    {
                        var cursor = await transaction.RunAsync(@"
                            MERGE (avatar:Avatar {GUId: $guid})
                            SET avatar.name = $name, avatar.FirstName = $firstName,
                                avatar.LastName = $lastName, avatar.EMail = $email,
                                avatar.username = $username, avatar.password = $password
                            RETURN avatar.GUId",
                            new
                            {
                                guid = id.ToString(),
                                name = avatar.Title,
                                firstName = avatar.FirstName,
                                lastName = avatar.LastName,
                                email = avatar.Email,
                                username = avatar.Username,
                                password = avatar.Password
                            }).ConfigureAwait(false);
                        await cursor.SingleAsync().ConfigureAwait(false);
                        return true;
                    }).ConfigureAwait(false);
                }
                finally
                {
                    await session.CloseAsync().ConfigureAwait(false);
                }

                avatar.AvatarId = id;
                avatar.CreatedProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
                result.Result = avatar;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error saving Neo4j Aura avatar.", ex);
            }

            return result;
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
                {
                    var providerKey = AvatarDetail.ProviderUniqueStorageKey?.GetValueOrDefault(Core.Enums.ProviderType.Neo4jOASIS) 
                        ?? AvatarDetail.ProviderUniqueStorageKey?.Values?.FirstOrDefault()
                        ?? AvatarDetail.Username 
                        ?? $"Neo4jOASIS:{AvatarDetail.CreatedDate.Ticks}";
                    AvatarDetail.Id = CreateDeterministicGuid($"{Core.Enums.ProviderType.Neo4jOASIS}:{providerKey}");
                }

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

    }
}
