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
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonRootAsync("Id", id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadHolonRootAsync("ProviderKey", providerKey, loadChildren, recursive, maxChildDepth, continueOnError);

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            var failures = new List<string>();
            try
            {
                // REAL Neo4j implementation for loading holons for parent by ID
                if (maxChildDepth < 0 || curentChildDepth < 0 || !Enum.IsDefined(typeof(HolonType), type))
                    throw new ArgumentException("Defined holon type and nonnegative read depths are required.");
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return MarkNativePartialRead(response, failures);
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
                        var child = ReadHolonNode(node);
                        if (child.Id == id || child.ParentHolonId != id || holons.Any(existing => existing.Id == child.Id))
                            throw new StorageLifecycleConflictException("Neo4j native child identity or parent edge is inconsistent.");
                        if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth + 1 < maxChildDepth))
                            await HydrateNativeChildrenAsync(session, child, true, maxChildDepth, curentChildDepth + 1, new HashSet<Guid> { id, child.Id }, continueOnError, failures);
                        holons.Add(child);
                    }
                    
                    response.Result = holons.Where(child => type == HolonType.All || child.HolonType == type).ToList();
                    response.IsLoaded = true;
                    response.IsError = false;
                    response.Message = "Child holons loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent from Neo4j: {ex.Message}");
            }
            return MarkNativePartialRead(response, failures);
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
            var failures = new List<string>();
            try
            {
                // REAL Neo4j implementation for loading holons by metadata pairs
                if (maxChildDepth < 0 || curentChildDepth < 0)
                    throw new ArgumentException("Nonnegative read depths are required.");
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return MarkNativePartialRead(response, failures);
                }

                var planned = MetadataLookupQuery(metaKeyValuePairs, metaKeyValuePairMatchMode, type);
                var query = planned.Query;
                var parameters = planned.Parameters;
                
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, parameters);
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        var holon = ReadHolonNode(node);
                        
                        if (loadChildren)
                            await HydrateNativeChildrenAsync(session, holon, recursive, maxChildDepth, curentChildDepth, new HashSet<Guid> { holon.Id }, continueOnError, failures);
                        holons.Add(holon);
                    }
                    
                    response.Result = holons;
                    response.IsLoaded = true;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {holons.Count} holons by metadata from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata pairs from Neo4j: {ex.Message}");
            }
            return MarkNativePartialRead(response, failures);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            var failures = new List<string>();
            try
            {
                // REAL Neo4j implementation for loading all holons
                if (maxChildDepth < 0 || curentChildDepth < 0 || !Enum.IsDefined(typeof(HolonType), type))
                    throw new ArgumentException("Defined holon type and nonnegative read depths are required.");
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return MarkNativePartialRead(response, failures);
                }

                var query = "MATCH (h:Holon) WHERE $type = 'All' OR h.HolonType = $type RETURN h";
                using (var session = _driver.AsyncSession())
                {
                    var result = await session.RunAsync(query, new { type = type.ToString() });
                    var records = await result.ToListAsync();
                    
                    var holons = new List<IHolon>();
                    foreach (var record in records)
                    {
                        var node = record["h"].As<Neo4j.Driver.INode>();
                        var holon = ReadHolonNode(node);
                        if (loadChildren)
                            await HydrateNativeChildrenAsync(session, holon, recursive, maxChildDepth, curentChildDepth, new HashSet<Guid> { holon.Id }, continueOnError, failures);
                        holons.Add(holon);
                    }
                    
                    response.Result = holons;
                    response.IsLoaded = true;
                    response.IsError = false;
                    response.Message = "All holons loaded successfully from Neo4j";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from Neo4j: {ex.Message}");
            }
            return MarkNativePartialRead(response, failures);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveOneHolonSnapshotAsync(holon, saveChildren, recursive, maxChildDepth);

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonSnapshotsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth);

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
            => DeleteHolonRecordAsync("Id",id.ToString());

    }
}
