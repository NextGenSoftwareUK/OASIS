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
                {
                    var providerKey = Avatar.ProviderUniqueStorageKey?.GetValueOrDefault(Core.Enums.ProviderType.Neo4jOASIS) 
                        ?? Avatar.ProviderUniqueStorageKey?.Values?.FirstOrDefault()
                        ?? Avatar.Username 
                        ?? $"Neo4jOASIS:{Avatar.CreatedDate.Ticks}";
                    Avatar.AvatarId = CreateDeterministicGuid($"{Core.Enums.ProviderType.Neo4jOASIS}:{providerKey}");
                }

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
