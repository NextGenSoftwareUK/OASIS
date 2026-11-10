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
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonBySelectorAsync("hl.GUId = $value", id.ToString("D"));

        private async Task<OASISResult<IHolon>> LoadHolonBySelectorAsync(string predicate, string value)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var lifetime = session.ConfigureAwait(false);
                var rows = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync($@"
                        MATCH (hl:Holon) WHERE {predicate}
                        RETURN hl.GUId AS id, hl.name AS name, hl.description AS description,
                               hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid
                        LIMIT 2", new { value }).ConfigureAwait(false);
                    return await cursor.ToListAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (rows.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Neo4j Aura holon was not found.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }
                if (rows.Count > 1)
                    throw new StorageIdentityAmbiguousException();

                var row = rows[0];
                var holon = new Holon
                {
                    Id = Guid.Parse(row["id"].As<string>()),
                    Name = row["name"].As<string>(),
                    Description = row["description"].As<string>(),
                    PreviousVersionId = row["previousversionid"] is string previous ? Guid.Parse(previous) : Guid.Empty
                };
                if (row["providerkey"] is string key)
                    holon.ProviderUniqueStorageKey[ProviderType.Value] = key;

                result.Result = holon;
                result.IsLoaded = true;
                result.LoadedCount = 1;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error loading Neo4j Aura holon.", ex);
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonBySelectorAsync("hl.ProviderKey = $value", providerKey);

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
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var session = Driver.AsyncSession(WithDatabase);
                await using var lifetime = session.ConfigureAwait(false);
                var holons = await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                        MATCH (hl:Holon)
                        WHERE $allTypes OR hl.HolonType = $type
                        RETURN hl.GUId AS id, hl.name AS name, hl.description AS description,
                               hl.ProviderKey AS providerkey, hl.PreviousVersionId AS previousversionid,
                               hl.HolonType AS type, hl.ParentHolonId AS parent
                        ORDER BY hl.GUId", new { allTypes = type == HolonType.All, type = (int)type }).ConfigureAwait(false);
                    return await cursor.ToListAsync(ReadListedHolon).ConfigureAwait(false);
                }).ConfigureAwait(false);

                result.Result = holons;
                result.IsLoaded = true;
                result.LoadedCount = holons.Count;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Error listing Neo4j Aura holons.", ex);
            }
            return result;
        }

        private IHolon ReadListedHolon(IRecord row)
        {
            var holon = new Holon
            {
                Id = Guid.Parse(row["id"].As<string>()),
                Name = row["name"].As<string>(),
                Description = row["description"].As<string>(),
                PreviousVersionId = row["previousversionid"] is string previous ? Guid.Parse(previous) : Guid.Empty,
                ParentHolonId = row["parent"] is string parent ? Guid.Parse(parent) : Guid.Empty
            };
            if (row["type"] is long type)
                holon.HolonType = (HolonType)type;
            if (row["providerkey"] is string key)
                holon.ProviderUniqueStorageKey[ProviderType.Value] = key;
            return holon;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider)
                .GetAwaiter().GetResult();

    }
}
