using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    public partial class AzureCosmosDBOASIS
    {
        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with id {id}";

            try
            {
                holonRepository.DeleteAsync(id).Wait();
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with providerKey {providerKey}";

            try
            {
                holonRepository.DeleteAsync(providerKey).Wait();
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with id {id}";

            try
            {
                await holonRepository.DeleteAsync(id);
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            string reason = "unknown";
            string errorMessage = $"An error occured deleting the holon with providerKey {providerKey}";

            try
            {
                await holonRepository.DeleteAsync(providerKey);
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}.");
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Azure Cosmos DB provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Azure Cosmos DB: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => ReadCurrentListAsync<IAvatarDetail>(()=>avatarDetailRepository,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
            => await ReadCurrentListAsync<IAvatarDetail>(()=>avatarDetailRepository,version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => ReadCurrentListAsync<IAvatar>(()=>avatarRepository,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => await ReadCurrentListAsync<IAvatar>(()=>avatarRepository,version);

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => ReadCurrentIdAsync<IAvatar>(()=>avatarRepository,id,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
            => await ReadCurrentIdAsync<IAvatar>(()=>avatarRepository,Id,version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
            => ReadCurrentSelectorAsync<IAvatar>(()=>avatarRepository,avatarEmail,row=>row.Email,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => await ReadCurrentSelectorAsync<IAvatar>(()=>avatarRepository,avatarEmail,row=>row.Email,version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
            => ReadCurrentSelectorAsync<IAvatar>(()=>avatarRepository,avatarUsername,row=>row.Username,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await ReadCurrentSelectorAsync<IAvatar>(()=>avatarRepository,avatarUsername,row=>row.Username,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => await ReadCurrentIdAsync<IAvatarDetail>(()=>avatarDetailRepository,id,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
            => ReadCurrentSelectorAsync<IAvatarDetail>(()=>avatarDetailRepository,avatarEmail,row=>row.Email,version).GetAwaiter().GetResult();

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
            => await ReadCurrentSelectorAsync<IAvatarDetail>(()=>avatarDetailRepository,avatarEmail,row=>row.Email,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
            => ReadCurrentSelectorAsync<IAvatarDetail>(()=>avatarDetailRepository,avatarUsername,row=>row.Username,version).GetAwaiter().GetResult();

        public async override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
            => await ReadCurrentSelectorAsync<IAvatarDetail>(()=>avatarDetailRepository,avatarUsername,row=>row.Username,version);

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => ReadCurrentKeyAsync<IAvatar>(()=>avatarRepository,providerKey,version).GetAwaiter().GetResult();

        public async override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => await ReadCurrentKeyAsync<IAvatar>(()=>avatarRepository,providerKey,version);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentHolonAsync(id,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentHolonKeyAsync(providerKey,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentHolonAsync(id,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version);

        public async override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentHolonKeyAsync(providerKey,loadChildren,recursive,maxChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentParentAsync(id.ToString(),type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => ReadCurrentParentAsync(providerKey,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentParentAsync(id.ToString(),type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public async override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await ReadCurrentParentAsync(providerKey,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

    }
}
