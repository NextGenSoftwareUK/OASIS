using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class GeoHotSpotManager : STARNETManagerBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot, GeoHotSpotDNA>, IGeoHotSpotManager
    public class GeoHotSpotManager : STARNETManagerBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot, STARNETDNA>, IGeoHotSpotManager
    {
        public GeoHotSpotManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(GeoHotSpot),
            HolonType.GeoHotSpot,
            HolonType.InstalledGeoHotSpot,
            "GeoHotSpot",
            //"GeoHotSpotId",
            "STARNETHolonId",
            "GeoHotSpotName",
            "GeoHotSpotType",
            "ogeohotspot",
            "oasis_geohotspots",
            "GeoHotSpotDNA.json",
            "GeoHotSpotDNAJSON")
        { }

        public GeoHotSpotManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(GeoHotSpot),
            HolonType.GeoHotSpot,
            HolonType.InstalledGeoHotSpot,
            "GeoHotSpot",
            //"GeoHotSpotId",
            "STARNETHolonId",
            "GeoHotSpotName",
            "GeoHotSpotType",
            "ogeohotspot",
            "oasis_geohotspots",
            "GeoHotSpotDNA.json",
            "GeoHotSpotDNAJSON")
        { }

        //public async Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpotAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoHotSpotSource,
        //    GeoHotSpotTriggeredType triggerType = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation,
        //    int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3,
        //    double latCoOrds = 0,
        //    double longCoOrds = 0,
        //    int hotSpotRadiusInMetres = 10,
        //    byte[] object3D = null,
        //    byte[] image2D = null,
        //    int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3,
        //    IList<IInventoryItem> rewards = null,
        //    IList<string> rewardIds = null,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, triggerType, fullPathToGeoHotSpotSource, new Dictionary<string, object>()
        //    {
        //        //We could also pass in metaData this way if we wanted but because we are setting them on the GeoHotSpot object below these will automatically be converted to MetaData on the holon anyway! ;-)
        //        //    { "TriggerType", triggerType },
        //        //    { "TimeInSecondsNeedToBeAtLocationToTriggerHotSpot", timeInSecondsNeedToBeAtLocationToTriggerHotSpot },
        //        //    { "Lat", latCoOrds },
        //        //    { "Long", longCoOrds },
        //        //    { "HotSpotRadiusInMetres", hotSpotRadiusInMetres },
        //        //    { "Object3D", object3D },
        //        //    { "Image3D", image2D },
        //        //    { "TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot", timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot },
        //        //    { "Rewards", rewards },
        //        //    { "RewardIds", rewardIds }
        //    },
        //    new GeoHotSpot()
        //    {
        //        TriggerType = triggerType,
        //        TimeInSecondsNeedToBeAtLocationToTriggerHotSpot = timeInSecondsNeedToBeAtLocationToTriggerHotSpot,
        //        Lat = latCoOrds,
        //        Long = longCoOrds,
        //        HotSpotRadiusInMetres = hotSpotRadiusInMetres,
        //        Object3D = object3D,
        //        Image2D = image2D,
        //        TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot,
        //        Rewards = rewards,
        //        RewardIds = rewardIds
        //    }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public async Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpot(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoHotSpotSource,
        //    GeoHotSpotTriggeredType triggerType = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation,
        //    int timeInSecondsNeedToBeAtLocationToTriggerHotSpot = 3,
        //    double latCoOrds = 0,
        //    double longCoOrds = 0,
        //    int hotSpotRadiusInMetres = 10,
        //    byte[] object3D = null,
        //    byte[] image2D = null,
        //    int timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = 3,
        //    IList<IInventoryItem> rewards = null,
        //    IList<string> rewardIds = null,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, triggerType, fullPathToGeoHotSpotSource, new Dictionary<string, object>()
        //    {
        //        //We could also pass in metaData this way if we wanted but because we are setting them on the GeoHotSpot object below these will automatically be converted to MetaData on the holon anyway! ;-)
        //        //    { "TriggerType", triggerType },
        //        //    { "TimeInSecondsNeedToBeAtLocationToTriggerHotSpot", timeInSecondsNeedToBeAtLocationToTriggerHotSpot },
        //        //    { "Lat", latCoOrds },
        //        //    { "Long", longCoOrds },
        //        //    { "HotSpotRadiusInMetres", hotSpotRadiusInMetres },
        //        //    { "Object3D", object3D },
        //        //    { "Image3D", image2D },
        //        //    { "TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot", timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot },
        //        //    { "Rewards", rewards },
        //        //    { "RewardIds", rewardIds }
        //    },
        //    new GeoHotSpot()
        //    {
        //        TriggerType = triggerType,
        //        TimeInSecondsNeedToBeAtLocationToTriggerHotSpot = timeInSecondsNeedToBeAtLocationToTriggerHotSpot,
        //        Lat = latCoOrds,
        //        Long = longCoOrds,
        //        HotSpotRadiusInMetres = hotSpotRadiusInMetres,
        //        Object3D = object3D,
        //        Image2D = image2D,
        //        TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot = timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot,
        //        Rewards = rewards,
        //        RewardIds = rewardIds
        //    }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //private OASISResult<IGeoHotSpot> ProcessResult(OASISResult<GeoHotSpot> operationResult)
        //{
        //    OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
        //    result.Result = (IGeoHotSpot)operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}