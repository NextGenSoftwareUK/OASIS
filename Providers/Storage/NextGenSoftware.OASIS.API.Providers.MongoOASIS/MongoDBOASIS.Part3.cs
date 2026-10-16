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

        // Each bulk request is one mutation unit: failed writes must not leave earlier roots committed.
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveGraphsAtomicAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, saveChildrenOnProvider).GetAwaiter().GetResult();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveGraphsAtomicAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, saveChildrenOnProvider);
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

        public Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
            => ReadNearbyRecordsAsync(geoLat, geoLong, radiusInMeters,
                () => LoadAllHolonsAsync(HolonType.All, loadChildren: false), Type);

        public IEnumerable<IAvatar> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            return GetAvatarsNearMeAsync(geoLat, geoLong, radiusInMeters).Result.Result;
        }

        public Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long geoLat, long geoLong, int radiusInMeters)
            => ReadNearbyRecordsAsync(geoLat, geoLong, radiusInMeters, () => LoadAllAvatarsAsync());

        private async Task<OASISResult<IEnumerable<T>>> ReadNearbyRecordsAsync<T>(long latitude, long longitude, int radius,
            Func<Task<OASISResult<IEnumerable<T>>>> load, HolonType type = HolonType.All) where T : IHolonBase
        {
            var result = new OASISResult<IEnumerable<T>>();
            try
            {
                var query = new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(latitude, longitude, radius);
                if (!Enum.IsDefined(typeof(HolonType), type)) throw new ArgumentOutOfRangeException(nameof(type));
                var loaded = await load();
                if (loaded.IsError) return loaded;
                result = NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<T>>(loaded);
                result.Result = query.Select(loaded.Result, row => row.IsActive && !row.IsDeleted && (type == HolonType.All || row.HolonType == type));
                result.IsLoaded = true;
            }
            catch (Exception cause)
            {
                result.Result = null;
                result.IsLoaded = false;
                OASISErrorHandling.HandleError(ref result, cause.Message, cause);
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

                if (holons == null) throw new ArgumentNullException(nameof(holons));
                var importRoots = holons.ToList();
                foreach (var importRoot in importRoots)
                {
                    var graphError = ValidateMongoChildGraph(importRoot);
                    if (graphError != null)
                    {
                        result.IsError = true;
                        result.ErrorCode = graphError.ErrorCode;
                        result.Exception = graphError.Exception;
                        result.Message = graphError.Message;
                        return result;
                    }
                }
                return await _holonRepository.ImportAtomicAsync(importRoots.Select(holon => DataHelper.ConvertOASISHolonToMongoEntity(holon)), PreserveCreationAuditFields).ConfigureAwait(false);
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
