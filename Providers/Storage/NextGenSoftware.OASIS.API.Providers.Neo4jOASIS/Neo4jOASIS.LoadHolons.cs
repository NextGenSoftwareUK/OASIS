using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public partial class Neo4jOASIS
    {
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // Implement real Neo4j holon loading
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                try
                {
                    // Create Neo4j query to load holon by ID
                    var query = "MATCH (h:Holon {Id: $id}) RETURN h";
                    var parameters = new { id = id.ToString() };
                    
                    // Execute REAL Neo4j query using Neo4j.Driver
                    using (var session = _driver.AsyncSession())
                    {
                        var result = await session.RunAsync(query, parameters);
                        var record = await result.SingleAsync();
                        
                        if (record != null)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            response.Result = new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            };
                            response.IsError = false;
                            response.Message = "Holon loaded successfully from Neo4j";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading holon from Neo4j: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // REAL Neo4j implementation for loading holon by provider key
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon {ProviderKey: $providerKey}) RETURN h";
                var parameters = new { providerKey };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        response.Result = new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        response.IsError = false;
                        response.Message = "Holon loaded successfully from Neo4j by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for loading holons for parent by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (p:Holon {Id: $parentId})-[:HAS_CHILD]->(h:Holon) RETURN h";
                var parameters = new { parentId = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        holons.Add(new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Child holons loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load parent holon by provider key: {parentResult.Message}"
                };
            }

            // Then load children using the parent ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Convert single key-value pair to dictionary and use the main method
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for loading holons by metadata pairs
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // Build WHERE clause for metadata matching
                var whereClauses = new List<string>();
                foreach (var kvp in metaKeyValuePairs)
                {
                    if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                    {
                        whereClauses.Add($"h.MetaData.`{kvp.Key}` = $metaValue_{kvp.Key}");
                    }
                    else
                    {
                        whereClauses.Add($"h.MetaData.`{kvp.Key}` CONTAINS $metaValue_{kvp.Key}");
                    }
                }

                var typeFilter = type == HolonType.All ? "" : " AND h.HolonType = $holonType";
                var query = $"MATCH (h:Holon) WHERE {string.Join(" AND ", whereClauses)}{typeFilter} RETURN h";
                
                var parameters = new Dictionary<string, object>();
                foreach (var kvp in metaKeyValuePairs)
                {
                    parameters[$"metaValue_{kvp.Key}"] = kvp.Value;
                }
                if (type != HolonType.All)
                {
                    parameters["holonType"] = type.ToString();
                }
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        var holon = new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        };
                        
                        // Load children recursively if requested
                        if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        holons.Add(holon);
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {holons.Count} holons by metadata from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata pairs from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for loading all holons
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon) RETURN h";
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        holons.Add(new Holon 
                        { 
                            Id = Guid.Parse(node["Id"].As<string>()),
                            Name = node["Name"].As<string>(),
                            Description = node["Description"].As<string>(),
                            HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                            CreatedDate = node["CreatedDate"].As<DateTime>(),
                            ModifiedDate = node["ModifiedDate"].As<DateTime>()
                        });
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "All holons loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // Implement real Neo4j holon saving
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                try
                {
                    // Create Neo4j query to save holon
                    var query = @"MERGE (h:Holon {Id: $id}) 
                                SET h.Name = $name, h.Description = $description, h.HolonType = $holonType,
                                    h.CreatedDate = $createdDate, h.ModifiedDate = $modifiedDate
                                RETURN h";
                    var parameters = new 
                    { 
                        id = holon.Id.ToString(),
                        name = holon.Name,
                        description = holon.Description,
                        holonType = holon.HolonType.ToString(),
                        createdDate = holon.CreatedDate,
                        modifiedDate = holon.ModifiedDate
                    };
                    
                    // Execute REAL Neo4j query using Neo4j.Driver
                    using (var session = _driver.AsyncSession())
                    {
                        var result = await session.RunAsync(query, parameters);
                        var record = await result.SingleAsync();
                        
                        if (record != null)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            response.Result = new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            };
                            response.IsError = false;
                            response.Message = "Holon saved successfully to Neo4j";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to save holon to Neo4j database");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error saving holon to Neo4j: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for saving multiple holons
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var savedHolons = new List<IHolon>();
                using (var session = _driver.AsyncSession())
                {
                    foreach (var holon in holons)
                    {
                        var query = @"MERGE (h:Holon {Id: $id}) 
                                    SET h.Name = $name, h.Description = $description, h.HolonType = $holonType,
                                        h.CreatedDate = $createdDate, h.ModifiedDate = $modifiedDate
                                    RETURN h";
                        var parameters = new 
                        { 
                            id = holon.Id.ToString(),
                            name = holon.Name,
                            description = holon.Description,
                            holonType = holon.HolonType.ToString(),
                            createdDate = holon.CreatedDate,
                            modifiedDate = holon.ModifiedDate
                        };
                        
                        var result = await session.RunAsync(query, parameters);
                        var record = await result.SingleAsync();
                        
                        if (record != null)
                        {
                            var node = record["h"].As<Neo4j.Driver.INode>();
                            savedHolons.Add(new Holon 
                            { 
                                Id = Guid.Parse(node["Id"].As<string>()),
                                Name = node["Name"].As<string>(),
                                Description = node["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(node["HolonType"].As<string>()),
                                CreatedDate = node["CreatedDate"].As<DateTime>(),
                                ModifiedDate = node["ModifiedDate"].As<DateTime>()
                            });
                        }
                    }
                }
                
                response.Result = savedHolons;
                response.IsError = false;
                response.Message = "Holons saved successfully to Neo4j";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                // REAL Neo4j implementation for deleting holon by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                var query = "MATCH (h:Holon {Id: $id}) DETACH DELETE h RETURN h";
                var parameters = new { id = id.ToString() };
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var record = await result.SingleAsync();
                    
                    if (record != null)
                    {
                        response.Result = new Holon { Id = id };
                        response.IsError = false;
                        response.Message = "Holon deleted successfully from Neo4j";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found in Neo4j database");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon from Neo4j: {ex.Message}");
            }
            return response;
        }

    }
}
