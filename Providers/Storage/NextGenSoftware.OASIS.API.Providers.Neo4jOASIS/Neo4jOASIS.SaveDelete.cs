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
            if(avatarId==Guid.Empty)return new OASISResult<IEnumerable<IHolon>>{IsError=true,
                Exception=new ArgumentException("A nonempty avatar ID is required.",nameof(avatarId)),
                ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Message="A nonempty avatar ID is required."};
            var response=await ExportAllAsync(version);
            if(response.IsError)return response;
            var owned=response.Result.Where(h=>h.CreatedByAvatarId==avatarId).ToList();
            response.Result=owned;response.LoadedCount=owned.Count;
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
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(avatarResult);
                }

                // Then export all data using the avatar ID
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
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
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(avatarResult);
                }

                // Then export all data using the avatar ID
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
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
