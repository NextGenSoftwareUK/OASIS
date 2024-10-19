using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class GeoHotSpotManager : OASISManager
    {
        public GeoHotSpotManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public GeoHotSpotManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

       public async Task<OASISResult<IGeoHotSpot>> CreateGeoHotSpotAsync(bool isARHotSpot, GeoHotSpotTriggeredType triggerType, string threeDObject = null, string twoDSprite = null, ProviderType providerType = ProviderType.Default)
       {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            string errorMessage = "Error occured in GeoHotSpotManager.CreateGeoHotSpotAsync. Reason:";

            try
            {
                if (string.IsNullOrEmpty(threeDObject) && string.IsNullOrEmpty(twoDSprite))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Both the threeDObject param and twoDSprite param are null, you need to specify at least one of these!");
                else
                {
                    result.Result = new GeoHotSpot()
                    {
                        IsARHotSpot = isARHotSpot,
                        TriggerType = triggerType,
                        ThreeDObject = threeDObject,
                        TwoDSprite = twoDSprite
                    };

                    OASISResult<GeoHotSpot> saveResult = await Data.SaveHolonAsync<GeoHotSpot>(result.Result, true, true, 0, true, false, providerType);
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    result.Result = saveResult.Result;

                    //if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    //{
                    //    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    //    result.Result = saveResult.Result;
                    //}
                }
            }
            catch (Exception ex) 
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
       }

        public OASISResult<IGeoHotSpot> CreateGeoHotSpot(bool isARHotSpot, GeoHotSpotTriggeredType triggerType, string threeDObject = null, string twoDSprite = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            string errorMessage = "Error occured in GeoHotSpotManager.CreateGeoHotSpot. Reason:";

            try
            {
                if (string.IsNullOrEmpty(threeDObject) && string.IsNullOrEmpty(twoDSprite))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Both the threeDObject param and twoDSprite param are null, you need to specify at least one of these!");
                else
                {
                    result.Result = new GeoHotSpot()
                    {
                        IsARHotSpot = isARHotSpot,
                        TriggerType = triggerType,
                        ThreeDObject = threeDObject,
                        TwoDSprite = twoDSprite
                    };

                    OASISResult<GeoHotSpot> saveResult = Data.SaveHolon<GeoHotSpot>(result.Result, true, true, 0, true, false, providerType);
                    result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    result.Result = saveResult.Result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> UpdateGeoHotSpotAsync(IGeoHotSpot geoHotSpot, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            string errorMessage = "Error occured in GeoHotSpotManager.UpdateGeoHotSpotAsync. Reason:";

            try
            {
                //TODO: Double check this is done automatically (it is in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                geoHotSpot.ModifiedByAvatarId = avatarId;
                geoHotSpot.ModifiedDate = DateTime.Now;

                OASISResult<GeoHotSpot> saveResult = await geoHotSpot.SaveAsync<GeoHotSpot>(true, true, 0, true, false, providerType);
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                result.Result = saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        public OASISResult<IGeoHotSpot> UpdateGeoHotSpot(IGeoHotSpot geoHotSpot, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            string errorMessage = "Error occured in GeoHotSpotManager.UpdateGeoHotSpot. Reason:";

            try
            {
                //TODO: Double check this is done automatically (it is in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                geoHotSpot.ModifiedByAvatarId = avatarId;
                geoHotSpot.ModifiedDate = DateTime.Now;

                OASISResult<GeoHotSpot> saveResult = geoHotSpot.Save<GeoHotSpot>(true, true, 0, true, false, providerType);
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                result.Result = saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IGeoHotSpot>> LoadGeoHotSpotAsync(Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            string errorMessage = "Error occured in GeoHotSpotManager.LoadGeoHotSpotAsync. Reason:";

            try
            {
                OASISResult<GeoHotSpot> loadResult = await Data.LoadHolonAsync<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                result.Result = loadResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        public OASISResult<IGeoHotSpot> LoadGeoHotSpot(Guid geoHotSpotId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGeoHotSpot> result = new OASISResult<IGeoHotSpot>();
            string errorMessage = "Error occured in GeoHotSpotManager.LoadGeoHotSpotAsync. Reason:";

            try
            {
                OASISResult<GeoHotSpot> loadResult = Data.LoadHolon<GeoHotSpot>(geoHotSpotId, true, true, 0, true, false, HolonType.All, 0, providerType);
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                result.Result = loadResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IGeoHotSpot>>> LoadAllGeoHotSpotsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
            string errorMessage = "Error occured in GeoHotSpotManager.LoadAllGeoHotSpotsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = await Data.LoadHolonsForParentAsync<GeoHotSpot>(avatarId, HolonType.GeoHotSpot, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                result.Result = loadHolonsResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IGeoHotSpot>> LoadAllGeoHotSpotsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
            string errorMessage = "Error occured in GeoHotSpotManager.LoadAllGeoHotSpotsForAvatarAsync. Reason:";

            try
            {
                OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = Data.LoadHolonsForParent<GeoHotSpot>(avatarId, HolonType.GeoHotSpot, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
                result.Result = loadHolonsResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        //public async Task<OASISResult<IEnumerable<IGeoHotSpot>>> LoadAllGeoHotSpotsForQuestAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IGeoHotSpot>> result = new OASISResult<IEnumerable<IGeoHotSpot>>();
        //    string errorMessage = "Error occured in GeoHotSpotManager.LoadAllGeoHotSpotsForAvatarAsync. Reason:";

        //    try
        //    {
        //        OASISResult<IEnumerable<GeoHotSpot>> loadHolonsResult = await Data.LoadHolonsForParentAsync<GeoHotSpot>(avatarId, HolonType.GeoHotSpot, true, true, 0, true, false, 0, HolonType.All, 0, providerType);
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
        //        result.Result = loadHolonsResult.Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
        //    }

        //    return result;
        //}
    }
}
