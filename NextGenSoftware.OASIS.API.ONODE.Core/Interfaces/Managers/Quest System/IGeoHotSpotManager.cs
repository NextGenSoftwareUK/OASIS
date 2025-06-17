using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface IGeoHotSpotManager
    {
        Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpot(Guid avatarId, string name, string description, string fullPathToGeoHotSpotSource, GeoHotSpotTriggeredType triggerType = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation, int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3, double latCoOrds = 0, double longCoOrds = 0, int hotSpotRadiusInMetres = 10, byte[] object3D = null, byte[] image2D = null, int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3, IList<IInventoryItem> rewards = null, IList<string> rewardIds = null, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpotAsync(Guid avatarId, string name, string description, string fullPathToGeoHotSpotSource, GeoHotSpotTriggeredType triggerType = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation, int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3, double latCoOrds = 0, double longCoOrds = 0, int hotSpotRadiusInMetres = 10, byte[] object3D = null, byte[] image2D = null, int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3, IList<IInventoryItem> rewards = null, IList<string> rewardIds = null, ProviderType providerType = ProviderType.Default);
    }
}