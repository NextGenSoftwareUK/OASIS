using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract partial class CelestialBodyCore<T> : ZomeBase, ICelestialBodyCore where T : ICelestialBody, new()
    {








        //TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        //public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
        //    string errorMessage = "Error in CelestialBodyCore.SaveZomes calling base.SaveHolons. Reason:";

        //    if (this.Zomes != null)
        //    {
        //        OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Zomes, true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //        result = OASISResultHelper.CopyResultToIZome(holonsResult);

        //        //TODO: May be useful to use this logic in HolonManager?
        //        //foreach (IZome zome in this.Zomes)
        //        //{
        //        //    if (zome.HasHolonChanged())
        //        //    {
        //        //        OASISResult<IZome> holonResult = await zome.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

        //        //        if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
        //        //        {
        //        //            result.SavedCount++;
        //        //            savedZomes.Add(holonResult.Result);
        //        //        }
        //        //        else
        //        //        {
        //        //            result.ErrorCount++;
        //        //            OASISErrorHandling.HandleWarning(ref result, $"There was an error in the CelestialBodyCore.SaveZomesAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {holonResult.Message}", true, false, false, true, false);

        //        //            if (!continueOnError)
        //        //                break;
        //        //        }
        //        //    }
        //        //}
        //    }

        //    //TODO: May be useful to use this logic in HolonManager?
        //    //if (result.ErrorCount > 0)
        //    //{
        //    //    string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomesAsync method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

        //    //    if (result.SavedCount == 0)
        //    //        OASISErrorHandling.HandleError(ref result, message);
        //    //    else
        //    //    {
        //    //        OASISErrorHandling.HandleWarning(ref result, message);
        //    //        result.IsSaved = true;
        //    //    }

        //    //    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
        //    //}
        //    //else
        //    //    result.IsSaved = true;

        //    //if (result.SavedCount > 0)
        //    //{ 
        //    //    result.Result = savedZomes;
        //    //    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    //}

        //    if (result != null && !result.IsError && result.Result != null)
        //        OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    else
        //    {
        //        OASISErrorHandling.HandleError(ref result, string.Concat("Error in CelestialBodyCore.SaveZomesAsync method caling base.SaveHolonsAsync. Reason: ", result.Message));
        //        OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
        //    }

        //    return result;
        //}













        //TODO: ALL THESE METHODS ARE NOW REDUNDANT BECAUSE LOAD/SAVE METHODS ON HOLONBASE WILL LOAD/SAVE EXCEPT MAYBE WE DO NEED TO CAST THE RESULT TO ICELESTIALBODY AND ALSO RAISE DIFFERENT EVENTS?
        //      SAME WITH ZOMEBASE...

        ////TODO: Why are we passing in savingHolon here? Shouldnt this just be saving the current celestialbody/holon?
        //public async Task<OASISResult<ICelestialBody>> SaveCelestialBodyAsync(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    string errorMessage = string.Concat("Error in CelestialBodyCore.SaveCelestialBodyAsync calling base.SaveHolonAsync method with ", LoggingHelper.GetHolonInfoForLogging(savingHolon, "holon"), ". Reason:");

        //    try
        //    {
        //        OASISResult<IHolon> holonResult = await base.SaveHolonAsync(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

        //        if (result != null && result.IsError)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
        //            OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = result.Exception });
        //        }
        //        else
        //            OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = ex });
        //    }

        //    return result;
        //}

        //public OASISResult<IHolon> SaveCelestialBody(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        //{
        //    //TODO: Not sure if this is a good way of doing this?
        //    return SaveCelestialBodyAsync(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, providerType).Result;
        //}

        //public async Task<OASISResult<T>> SaveCelestialBodyAsync<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    return await base.SaveHolonAsync<T>(savingHolon, false, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
        //}

        //public OASISResult<T> SaveCelestialBody<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    //TODO: Not sure if this is a good way of doing this?
        //    return SaveCelestialBodyAsync<T>(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, providerType).Result;
        //}

        //public async Task<OASISResult<T>> LoadCelestialBodyAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    return OASISResultHelperForHolons<IHolon, T>.CopyResult(await base.LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        //}

        //public OASISResult<T> LoadCelestialBody<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        //{
        //    return OASISResultHelperForHolons<IHolon, T>.CopyResult(base.LoadHolon(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        //}

        //public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = await base.LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

        //    OASISResultHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
        //    result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);

        //    return result;
        //}

        //public OASISResult<ICelestialBody> LoadCelestialBody(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = base.LoadHolon(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

        //    OASISResultHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
        //    result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
        //    result.Result = (ICelestialBody)holonResult.Result;

        //    return result;
        //}

        //public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = await base.LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version);

        //    OASISResultHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
        //    result.Result = Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(holonResult.Result, (CelestialBody)result.Result);

        //    return result;
        //}


        //MOVE TO HolonManager because this is a gernic method and does not directly apply to CelestialBodyCore.
        //protected virtual async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentCelestialBody, IHolon holon, List<IHolon> holons, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    if (holons == null)
        //        holons = new List<IHolon>();

        //    else if (holons.Any(x => x.Name == holon.Name))
        //    {
        //        result.IsError = true;
        //        result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
        //        return result;
        //    }

        //    holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

        //    if (holon.ParentOmniverseId == Guid.Empty)
        //    {
        //        holon.ParentOmniverseId = parentCelestialBody.ParentOmniverseId;
        //        holon.ParentOmniverse = parentCelestialBody.ParentOmniverse;
        //    }

        //    if (holon.ParentMultiverseId == Guid.Empty)
        //    {
        //        holon.ParentMultiverseId = parentCelestialBody.ParentMultiverseId;
        //        holon.ParentMultiverse = parentCelestialBody.ParentMultiverse;
        //    }

        //    if (holon.ParentUniverseId == Guid.Empty)
        //    {
        //        holon.ParentUniverseId = parentCelestialBody.ParentUniverseId;
        //        holon.ParentUniverse = parentCelestialBody.ParentUniverse;
        //    }

        //    if (holon.ParentDimensionId == Guid.Empty)
        //    {
        //        holon.ParentDimensionId = parentCelestialBody.ParentDimensionId;
        //        holon.ParentDimension = parentCelestialBody.ParentDimension;
        //    }

        //    if (holon.ParentGalaxyClusterId == Guid.Empty)
        //    {
        //        holon.ParentGalaxyClusterId = parentCelestialBody.ParentGalaxyClusterId;
        //        holon.ParentGalaxyCluster = parentCelestialBody.ParentGalaxyCluster;
        //    }

        //    if (holon.ParentGalaxyId == Guid.Empty)
        //    {
        //        holon.ParentGalaxyId = parentCelestialBody.ParentGalaxyId;
        //        holon.ParentGalaxy = parentCelestialBody.ParentGalaxy;
        //    }

        //    if (holon.ParentSolarSystemId == Guid.Empty)
        //    {
        //        holon.ParentSolarSystemId = parentCelestialBody.ParentSolarSystemId;
        //        holon.ParentSolarSystem = parentCelestialBody.ParentSolarSystem;
        //    }

        //    if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentGreatGrandSuperStarId = parentCelestialBody.ParentGreatGrandSuperStarId;
        //        holon.ParentGreatGrandSuperStar = parentCelestialBody.ParentGreatGrandSuperStar;
        //    }

        //    if (holon.ParentGrandSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentGrandSuperStarId = parentCelestialBody.ParentGrandSuperStarId;
        //        holon.ParentGrandSuperStar = parentCelestialBody.ParentGrandSuperStar;
        //    }

        //    if (holon.ParentSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentSuperStarId = parentCelestialBody.ParentSuperStarId;
        //        holon.ParentSuperStar = parentCelestialBody.ParentSuperStar;
        //    }

        //    if (holon.ParentStarId == Guid.Empty)
        //    {
        //        holon.ParentStarId = parentCelestialBody.ParentStarId;
        //        holon.ParentStar = parentCelestialBody.ParentStar;
        //    }

        //    if (holon.ParentPlanetId == Guid.Empty)
        //    {
        //        holon.ParentPlanetId = parentCelestialBody.ParentPlanetId;
        //        holon.ParentPlanet = parentCelestialBody.ParentPlanet;
        //    }

        //    if (holon.ParentMoonId == Guid.Empty)
        //    {
        //        holon.ParentMoonId = parentCelestialBody.ParentMoonId;
        //        holon.ParentMoon = parentCelestialBody.ParentMoon;
        //    }

        //    if (holon.ParentCelestialSpaceId == Guid.Empty)
        //    {
        //        holon.ParentCelestialSpaceId = parentCelestialBody.ParentCelestialSpaceId;
        //        holon.ParentCelestialSpace = parentCelestialBody.ParentCelestialSpace;
        //    }

        //    if (holon.ParentCelestialBodyId == Guid.Empty)
        //    {
        //        holon.ParentCelestialBodyId = parentCelestialBody.ParentCelestialBodyId;
        //        holon.ParentCelestialBody = parentCelestialBody.ParentCelestialBody;
        //    }

        //    if (holon.ParentZomeId == Guid.Empty)
        //    {
        //        holon.ParentZomeId = parentCelestialBody.ParentZomeId;
        //        holon.ParentZome = parentCelestialBody.ParentZome;
        //    }

        //    if (holon.ParentHolonId == Guid.Empty)
        //    {
        //        holon.ParentHolonId = parentCelestialBody.ParentHolonId;
        //        holon.ParentHolon = parentCelestialBody.ParentHolon;
        //    }

        //    switch (parentCelestialBody.HolonType)
        //    {
        //        case HolonType.GreatGrandSuperStar:
        //            holon.ParentGreatGrandSuperStarId = parentCelestialBody.Id;
        //            holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.GrandSuperStar:
        //            holon.ParentGrandSuperStarId = parentCelestialBody.Id;
        //            holon.ParentGrandSuperStar = (IGrandSuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.SuperStar:
        //            holon.ParentSuperStarId = parentCelestialBody.Id;
        //            holon.ParentSuperStar = (ISuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Multiverse:
        //            holon.ParentMultiverseId = parentCelestialBody.Id;
        //            holon.ParentMultiverse = (IMultiverse)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Universe:
        //            holon.ParentUniverseId = parentCelestialBody.Id;
        //            holon.ParentUniverse = (IUniverse)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Dimension:
        //            holon.ParentDimensionId = parentCelestialBody.Id;
        //            holon.ParentDimension = (IDimension)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.GalaxyCluster:
        //            holon.ParentGalaxyClusterId = parentCelestialBody.Id;
        //            holon.ParentGalaxyCluster = (IGalaxyCluster)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Galaxy:
        //            holon.ParentGalaxyId = parentCelestialBody.Id;
        //            holon.ParentGalaxy = (IGalaxy)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.SolarSystem:
        //            holon.ParentSolarSystemId = parentCelestialBody.Id;
        //            holon.ParentSolarSystem = (ISolarSystem)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Star:
        //            holon.ParentStarId = parentCelestialBody.Id;
        //            holon.ParentStar = (IStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Planet:
        //            holon.ParentPlanetId = parentCelestialBody.Id;
        //            holon.ParentPlanet = (IPlanet)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Moon:
        //            holon.ParentMoonId = parentCelestialBody.Id;
        //            holon.ParentMoon = (IMoon)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Zome:
        //            holon.ParentZomeId = parentCelestialBody.Id;
        //            holon.ParentZome = (IZome)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Holon:
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = parentCelestialBody;
        //            break;
        //    }

        //    holons.Add(holon);

        //    //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
        //    //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

        //    if (saveHolon)
        //    {
        //        result = await base.SaveHolonAsync(holon, false, true, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
        //        result.IsSaved = true;
        //    }
        //    else
        //    {
        //        result.Message = "Holon was not saved due to saveHolon being set to false.";
        //        result.IsSaved = false;
        //        result.Result = holon;
        //    }

        //    return result;
        //}





    }
}
