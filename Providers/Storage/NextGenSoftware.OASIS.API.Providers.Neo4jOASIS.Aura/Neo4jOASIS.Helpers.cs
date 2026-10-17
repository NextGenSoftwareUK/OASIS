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
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
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

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
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
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Neo4j Aura";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Load holons by metadata from Neo4j Aura database
                var holons = new List<IHolon>();
                
                using (var session = Driver.AsyncSession())
                {
                    var query = "MATCH (h:Holon) WHERE h.MetaData[$metaKey] = $metaValue RETURN h";
                    var parameters = new Dictionary<string, object>
                    {
                        { "metaKey", metaKey },
                        { "metaValue", metaValue }
                    };
                    
                    var cursor = await session.RunAsync(query, parameters);
                    var records = await cursor.ToListAsync();
                    
                    foreach (var record in records)
                    {
                        var holonNode = record["h"].As<Neo4jNode>();
                        var holon = new Holon
                        {
                            Id = Guid.Parse(holonNode["Id"].As<string>()),
                            Name = holonNode["Name"].As<string>(),
                            Description = holonNode["Description"].As<string>(),
                            MetaData = holonNode["MetaData"].As<Dictionary<string, object>>()
                        };
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons?.Count ?? 0} holons by metadata from Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Load holons by multiple metadata pairs from Neo4j Aura database
                var holons = new List<IHolon>();
                
                using (var session = Driver.AsyncSession())
                {
                    var whereConditions = new List<string>();
                    var parameters = new Dictionary<string, object>();
                    
                    int index = 0;
                    foreach (var pair in metaKeyValuePairs)
                    {
                        var keyParam = $"metaKey{index}";
                        var valueParam = $"metaValue{index}";
                        
                        whereConditions.Add($"h.MetaData[${keyParam}] = ${valueParam}");
                        
                        parameters[keyParam] = pair.Key;
                        parameters[valueParam] = pair.Value;
                        index++;
                    }
                    
                    var whereClause = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All 
                        ? string.Join(" AND ", whereConditions)
                        : string.Join(" OR ", whereConditions);
                    
                    var query = $"MATCH (h:Holon) WHERE {whereClause} RETURN h";
                    
                    var cursor = await session.RunAsync(query, parameters);
                    var records = await cursor.ToListAsync();
                    
                    foreach (var record in records)
                    {
                        var holonNode = record["h"].As<Neo4jNode>();
                        var holon = new Holon
                        {
                            Id = Guid.Parse(holonNode["Id"].As<string>()),
                            Name = holonNode["Name"].As<string>(),
                            Description = holonNode["Description"].As<string>(),
                            MetaData = holonNode["MetaData"].As<Dictionary<string, object>>()
                        };
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons?.Count ?? 0} holons by metadata pairs from Neo4j Aura";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Neo4j Aura: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
    }
}
