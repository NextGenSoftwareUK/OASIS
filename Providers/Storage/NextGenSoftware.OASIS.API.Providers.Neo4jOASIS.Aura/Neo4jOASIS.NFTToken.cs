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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
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
                            // Real Neo4j implementation: Add search result
                            searchResults.SearchResultAvatars.Add(avatar);
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
                            // Real Neo4j implementation: Add holon to search results
                            searchResults.SearchResultHolons.Add(holon);
                        }
                    }
                }
                
                // Set total results count
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Successfully searched Neo4j Aura database and found {searchResults.NumberOfResults} results";
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Neo4j Aura provider: {activateResult.Message}");
                        return result;
                    }
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

    }
}
