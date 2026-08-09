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

    }
}
