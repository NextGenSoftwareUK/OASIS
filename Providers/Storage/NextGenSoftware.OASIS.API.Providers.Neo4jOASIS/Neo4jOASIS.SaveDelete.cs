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
        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => DeleteHolonRecordAsync("ProviderKey",providerKey);

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }



        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                // REAL Neo4j implementation for importing holons
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "Holons collection cannot be null or empty");
                    return response;
                }

                // Use SaveHolonsAsync to import holons (it already handles batch saving)
                var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
                
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = $"Successfully imported {saveResult.Result.Count()} holons to Neo4j";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons: {saveResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data for avatar by ID
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                using (var session = _driver.AsyncSession())
                {
                    // Query all holons related to the avatar (directly or through relationships)
                    var query = @"
                        MATCH (a:Avatar {Id: $avatarId})
                        OPTIONAL MATCH (a)-[*]-(h:Holon)
                        WITH COLLECT(DISTINCT h) as holons
                        UNWIND holons as holon
                        WHERE holon IS NOT NULL
                        RETURN holon.Id as Id, holon.Name as Name, holon.Description as Description,
                               holon.HolonType as HolonType, holon.CreatedDate as CreatedDate,
                               holon.ModifiedDate as ModifiedDate, holon.ProviderKey as ProviderKey,
                               holon.ParentHolonId as ParentHolonId";

                    var result = await session.RunAsync(query, new { avatarId = avatarId.ToString() });
                    var records = await result.ToListAsync();
                    var holons = new List<IHolon>();

                    foreach (var record in records)
                    {
                        try
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(record["Id"].As<string>()),
                                Name = record["Name"].As<string>(),
                                Description = record["Description"].As<string>(),
                                HolonType = Enum.Parse<HolonType>(record["HolonType"].As<string>()),
                                CreatedDate = record["CreatedDate"].As<DateTime>(),
                                ModifiedDate = record["ModifiedDate"].As<DateTime>(),
                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string> { { Core.Enums.ProviderType.Neo4jOASIS, record["ProviderKey"].As<string>() } }
                            };
                            
                            if (record["ParentHolonId"] != null)
                            {
                                holon.ParentHolonId = Guid.Parse(record["ParentHolonId"].As<string>());
                            }
                            
                            holons.Add(holon);
                        }
                        catch
                        {
                            // Skip invalid records
                            continue;
                        }
                    }

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Exported {holons.Count} holons for avatar {avatarId} from Neo4j successfully";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by ID from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data for avatar by username
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // First load the avatar to get the ID
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                    return response;
                }

                // Then export all data using the avatar ID
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                response.Result = exportResult.Result;
                response.IsError = exportResult.IsError;
                response.Message = exportResult.Message;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by username from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // REAL Neo4j implementation for exporting all data for avatar by email
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    OASISErrorHandling.HandleError(ref response, "Neo4j connection parameters not configured");
                    return response;
                }

                // First load the avatar to get the ID
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmailAddress} not found");
                    return response;
                }

                // Then export all data using the avatar ID
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                response.Result = exportResult.Result;
                response.IsError = exportResult.IsError;
                response.Message = exportResult.Message;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by email from Neo4j: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => LoadAllHolonsAsync(loadChildren:false,version:version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }




public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radius)
            => ReadNearbyRecordsAsync(geoLat,geoLong,radius,()=>LoadAllAvatarsAsync()).GetAwaiter().GetResult();

public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radius, HolonType holonType = HolonType.All)
            => ReadNearbyRecordsAsync(geoLat,geoLong,radius,()=>LoadAllHolonsAsync(HolonType.All,loadChildren:false),holonType).GetAwaiter().GetResult();



        public void Dispose()
        {
            // Neo4j cleanup if needed
        }

    }
}
