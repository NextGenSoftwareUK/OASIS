using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;


namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IGeoHotSpotManager : ISTARNETManagerBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot>
    {
        //Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpot(Guid avatarId, string name, string description, string fullPathToGeoHotSpotSource, GeoHotSpotTriggeredType triggerType = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation, int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3, double latCoOrds = 0, double longCoOrds = 0, int hotSpotRadiusInMetres = 10, byte[] object3D = null, byte[] image2D = null, int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3, IList<IInventoryItem> rewards = null, IList<string> rewardIds = null, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpotAsync(Guid avatarId, string name, string description, string fullPathToGeoHotSpotSource, GeoHotSpotTriggeredType triggerType = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation, int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3, double latCoOrds = 0, double longCoOrds = 0, int hotSpotRadiusInMetres = 10, byte[] object3D = null, byte[] image2D = null, int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3, IList<IInventoryItem> rewards = null, IList<string> rewardIds = null, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}