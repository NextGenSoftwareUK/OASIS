using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class GeoHotSpotManager : PublishManagerBase
    {
        public GeoHotSpotManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public GeoHotSpotManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        //TODO: Make all other Managers follow this pattern/design (so use Save instead of Create and Update)
        #region COSMICManagerBase
        public async Task<OASISResult<IGeoHotSpot>> SaveGeoHotSpotAsync(IGeoHotSpot geoHotSpotItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = await SaveHolonAsync<GeoHotSpot>(geoHotSpotItem, avatarId, providerType, "GeoHotSpotManager.SaveGeoHotSpotAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> SaveGeoHotSpot(IGeoHotSpot geoHotSpotItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = SaveHolon<GeoHotSpot>(geoHotSpotItem, avatarId, providerType, "GeoHotSpotManager.SaveGeoHotSpot");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> LoadGeoHotSpotAsync(Guid geoHotSpotItemId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> loadResult = await LoadHolonAsync<GeoHotSpot>(geoHotSpotItemId, providerType, "GeoHotSpotManager.LoadGeoHotSpotAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> LoadGeoHotSpot(Guid geoHotSpotItemId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> loadResult = LoadHolon<GeoHotSpot>(geoHotSpotItemId, providerType, "GeoHotSpotManager.LoadGeoHotSpot");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IGeoHotSpot>>> LoadAllGeoHotSpotsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
            OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = await LoadAllHolonsAsync<GeoHotSpot>(providerType, "GeoHotSpotManager.LoadAllGeoHotSpotsAsync", HolonType.GeoHotSpot);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IGeoHotSpot>> LoadAllGeoHotSpots(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
            OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = LoadAllHolons<GeoHotSpot>(providerType, "GeoHotSpotManager.LoadAllGeoHotSpots", HolonType.GeoHotSpot);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IGeoHotSpot>>> LoadAllGeoHotSpotsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
            OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = await LoadAllHolonsForAvatarAsync<GeoHotSpot>(avatarId, providerType, "GeoHotSpotManager.LoadAllGeoHotSpotsForAvatarAsync", HolonType.GeoHotSpot);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IGeoHotSpot>> LoadAllGeoHotSpotsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
            OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = LoadAllHolonsForAvatar<GeoHotSpot>(avatarId, providerType, "GeoHotSpotManager.LoadAllGeoHotSpotsForAvatarAsync", HolonType.GeoHotSpot);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> DeleteGeoHotSpotAsync(Guid geoHotSpotId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> loadHolonsResult = await DeleteHolonAsync<GeoHotSpot>(geoHotSpotId, softDelete, providerType, "GeoHotSpotManager.DeleteGeoHotSpotAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> DeleteGeoHotSpot(Guid geoHotSpotId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> loadHolonsResult = DeleteHolon<GeoHotSpot>(geoHotSpotId, softDelete, providerType, "GeoHotSpotManager.DeleteGeoHotSpot");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }
        #endregion

        #region PublishManagerBase
        public async Task<OASISResult<IGeoHotSpot>> PublishGeoHotSpotAsync(Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = await PublishHolonAsync<GeoHotSpot>(geoHotSpotId, avatarId, "GeoHotSpotManager.PublishGeoHotSpotAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> PublishGeoHotSpot(Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = PublishHolon<GeoHotSpot>(geoHotSpotId, avatarId, "GeoHotSpotManager.PublishGeoHotSpot", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> PublishGeoHotSpotAsync(IGeoHotSpot geoHotSpot, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = await PublishHolonAsync<GeoHotSpot>(geoHotSpot, avatarId, "GeoHotSpotManager.PublishGeoHotSpotAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> PublishGeoHotSpot(IGeoHotSpot geoHotSpot, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = PublishHolon<GeoHotSpot>(geoHotSpot, avatarId, "GeoHotSpotManager.PublishGeoHotSpot", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> UnpublishGeoHotSpotAsync(Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = await UnpublishHolonAsync<GeoHotSpot>(geoHotSpotId, avatarId, "GeoHotSpotManager.UnpublishGeoHotSpotAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> UnpublishGeoHotSpot(Guid geoHotSpotId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = UnpublishHolon<GeoHotSpot>(geoHotSpotId, avatarId, "GeoHotSpotManager.UnpublishGeoHotSpot", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> UnpublishGeoHotSpotAsync(IGeoHotSpot geoHotSpot, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = await UnpublishHolonAsync<GeoHotSpot>(geoHotSpot, avatarId, "GeoHotSpotManager.UnpublishGeoHotSpotAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IGeoHotSpot> UnpublishGeoHotSpot(IGeoHotSpot geoHotSpot, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            OASISResult<GeoHotSpot> saveResult = UnpublishHolon<GeoHotSpot>(geoHotSpot, avatarId, "GeoHotSpotManager.UnpublishGeoHotSpot", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }
        #endregion
    }
}