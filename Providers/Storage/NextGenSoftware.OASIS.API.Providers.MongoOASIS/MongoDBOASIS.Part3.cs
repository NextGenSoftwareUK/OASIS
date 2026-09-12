using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using DataHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            if (holons == null)
            {
                result.Message = "Holons is null";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            if (holons.Count() == 0)
            {
                result.Message = "Holons collection is empty.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = SaveHolon(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildren && saveChildrenOnProvider && holonResult.Result.Children != null && holonResult.Result.Children.Count() > 0
                        && (recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth)))
                    {
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(holonResult.Result.Children, saveChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, saveChildrenOnProvider);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result.ToList();
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            result.Result = savedHolons.ToList();

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the MongoDBOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            if (holons == null)
            {
                result.Message = "Holons is null";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            if (holons.Count() == 0)
            {
                result.Message = "Holons collection is empty.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = await SaveHolonAsync(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildren && saveChildrenOnProvider && holonResult.Result.Children != null && holonResult.Result.Children.Count() > 0
                        && (recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth)))
                    {
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = await SaveHolonsAsync(holonResult.Result.Children, saveChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, saveChildrenOnProvider);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result.ToList();
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            result.Result = savedHolons.ToList();

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the SQLLiteDBOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return _holonRepository.Delete(id);
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            return await _holonRepository.DeleteAsync(id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return _holonRepository.Delete(providerKey);
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            return await _holonRepository.DeleteAsync(providerKey);
        }

        public IEnumerable<IHolon> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            return GetHolonsNearMeAsync(geoLat, geoLong, radiusInMeters, Type).Result.Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Use LoadHolonsByMetaData to find holons of the specified type within the radius
                var holonsResult = await LoadHolonsByMetaDataAsync("HolonType", Type.ToString(), Type);
                if (holonsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = holonsResult.Message;
                    return result;
                }
                
                // Filter holons by geo location using the radius calculation
                var nearbyHolons = new List<IHolon>();
                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null && 
                        holon.MetaData.ContainsKey("Latitude") && 
                        holon.MetaData.ContainsKey("Longitude"))
                    {
                        if (double.TryParse(holon.MetaData["Latitude"]?.ToString(), out double holonLat) &&
                            double.TryParse(holon.MetaData["Longitude"]?.ToString(), out double holonLong))
                        {
                            // Calculate distance using Haversine formula
                            double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, holonLat, holonLong);
                            if (distance <= radiusInMeters)
                            {
                                nearbyHolons.Add(holon);
                            }
                        }
                    }
                }
                
                result.Result = nearbyHolons;
                result.IsError = false;
                result.Message = $"Retrieved {nearbyHolons.Count} holons of type {Type} within {radiusInMeters}m of ({geoLat}, {geoLong})";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving holons near me: {ex.Message}");
            }
            return result;
        }

        public IEnumerable<IAvatar> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            return GetAvatarsNearMeAsync(geoLat, geoLong, radiusInMeters).Result.Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // Use LoadHolonsByMetaData to find avatars within the specified radius
                // First, we need to search for avatars with geo coordinates in their metadata
                var avatarsResult = await LoadHolonsByMetaDataAsync("HolonType", "Avatar", HolonType.Avatar);
                if (avatarsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = avatarsResult.Message;
                    return result;
                }
                
                // Filter avatars by geo location using the radius calculation
                var nearbyAvatars = new List<IAvatar>();
                foreach (var holon in avatarsResult.Result)
                {
                    if (holon.MetaData != null && 
                        holon.MetaData.ContainsKey("Latitude") && 
                        holon.MetaData.ContainsKey("Longitude"))
                    {
                        if (double.TryParse(holon.MetaData["Latitude"]?.ToString(), out double avatarLat) &&
                            double.TryParse(holon.MetaData["Longitude"]?.ToString(), out double avatarLong))
                        {
                            // Calculate distance using Haversine formula (simplified)
                            double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, avatarLat, avatarLong);
                            if (distance <= radiusInMeters)
                            {
                                nearbyAvatars.Add(new Avatar
                                {
                                    Id = holon.Id,
                                    Username = holon.MetaData?.ContainsKey("Username") == true ? holon.MetaData["Username"]?.ToString() : holon.Name,
                                    Email = holon.MetaData?.ContainsKey("Email") == true ? holon.MetaData["Email"]?.ToString() : null,
                                    CreatedDate = holon.CreatedDate,
                                    ModifiedDate = holon.ModifiedDate
                                });
                            }
                        }
                    }
                }
                
                result.Result = nearbyAvatars;
                result.IsError = false;
                result.Message = $"Retrieved {nearbyAvatars.Count} avatars within {radiusInMeters}m of ({geoLat}, {geoLong})";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error retrieving avatars near me: {ex.Message}");
            }
            return result;
        }


        //IOASISSuperStar Interface Implementation

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // Mongo currently does not generate native code from STAR metadata.
            return true;
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            return GetAvatarsNearMeAsync(geoLat, geoLong, radiusInMeters).Result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            return GetHolonsNearMeAsync(geoLat, geoLong, radiusInMeters, Type).Result;
        }

        // distance calculation moved to GeoHelper for reuse
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate MongoDB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    var holonEntity = new Holon
                    {
                        Id = holon.Id.ToString(),
                        Name = holon.Name,
                        Description = holon.Description,
                        HolonType = holon.HolonType,
                        CreatedByAvatarId = holon.CreatedByAvatarId.ToString(),
                        CreatedDate = holon.CreatedDate,
                        ModifiedDate = holon.ModifiedDate,
                        Version = holon.Version,
                        IsActive = holon.IsActive,
                        ParentHolonId = holon.ParentHolonId,
                        ParentHolon = holon.ParentHolon,
                        Children = holon.Children,
                        MetaData = holon.MetaData,
                        PreviousVersionId = holon.PreviousVersionId
                    };
                    
                    var saveResult = await _holonRepository.AddAsync(holonEntity);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                    importedCount++;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to MongoDB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to MongoDB: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate MongoDB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar
                var holons = await _holonRepository.GetAllHolonsAsync();
                result.Result = holons.Cast<IHolon>();
                result.IsError = false;
                result.Message = $"Successfully exported {holons?.Count() ?? 0} holons for avatar {avatarId} from MongoDB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from MongoDB: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate MongoDB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar username
                var holons = await _holonRepository.GetAllHolonsAsync();
                result.Result = holons.Cast<IHolon>();
                result.IsError = false;
                result.Message = $"Successfully exported {holons?.Count() ?? 0} holons for avatar {avatarUsername} from MongoDB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from MongoDB: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate MongoDB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar email
                var holons = await _holonRepository.GetAllHolonsAsync();
                result.Result = holons.Cast<IHolon>();
                result.IsError = false;
                result.Message = $"Successfully exported {holons?.Count() ?? 0} holons for avatar {avatarEmailAddress} from MongoDB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from MongoDB: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate MongoDB provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons
                var holons = await _holonRepository.GetAllHolonsAsync();
                result.Result = holons.Cast<IHolon>();
                result.IsError = false;
                result.Message = $"Successfully exported {holons?.Count() ?? 0} holons from MongoDB";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from MongoDB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }
    }
}
