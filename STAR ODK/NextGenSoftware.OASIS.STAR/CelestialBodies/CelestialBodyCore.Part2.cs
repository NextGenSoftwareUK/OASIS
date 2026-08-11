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
    public abstract partial class CelestialBodyCore<T>
    {

        public OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = "Error in CelestialBodyCore.SaveZomes<T> calling base.SaveHolons<T>. Reason:";

            try
            {
                if (this.Zomes != null)
                    result = GlobalHolonData.SaveHolons<T>((IEnumerable<T>)this.Zomes, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                    OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IZome>> AddZomeAsync(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.AddZomeAsync calling zome.SaveAsync method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                SetParentsAndAddZome(zome);

                if (saveZome)
                {
                    result = await zome.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result != null && result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                    }
                    else
                        OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = result });
                }
                else
                    OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public OASISResult<IZome> AddZome(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.AddZome calling zome.Save method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                SetParentsAndAddZome(zome);

                if (saveZome)
                {
                    result = zome.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result != null && result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                    }
                    else
                        OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = result });
                }
                else
                    OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<T>> AddZomeAsync<T>(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.AddZomeAsync<T> calling zome.SaveAsync<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                SetParentsAndAddZome(zome);

                if (saveZome)
                {
                    result = await zome.SaveAsync<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result != null && result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                    }
                    else
                        OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
                }
                else
                    OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

        public OASISResult<T> AddZome<T>(IZome zome, bool saveZome = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.AddZome<T> calling zome.Save<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                SetParentsAndAddZome(zome);

                if (saveZome)
                {
                    result = zome.Save<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result != null && result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                    }
                    else
                        OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
                }
                else
                    OnZomeAdded?.Invoke(this, new ZomeAddedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IZome>> RemoveZomeAsync(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZomeAsync calling zome.SaveAsync method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                BlankParentsAndRemoveZome(zome);

                if (deleteZome)
                {
                    OASISResult<IHolon> deleteResult = await zome.DeleteAsync(avatarId, softDelete, providerType);

                    if (deleteResult != null && deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {deleteResult.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                    }
                    else
                        OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = result });
                }
                else
                    OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public OASISResult<IZome> RemoveZome(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZome calling zome.Save method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                BlankParentsAndRemoveZome(zome);

                if (deleteZome)
                {
                    OASISResult<IHolon> deleteResult = zome.Delete(avatarId, softDelete, providerType);

                    if (deleteResult != null && deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {deleteResult.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = result.Exception });
                    }
                    else
                        OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = result });
                }
                else
                    OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = result, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<T>> RemoveZomeAsync<T>(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZomeAsync<T> calling zome.SaveAsync<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                BlankParentsAndRemoveZome(zome);

                if (deleteZome)
                {
                    OASISResult<IHolon> deleteResult = await zome.DeleteAsync(avatarId, softDelete, providerType);

                    if (deleteResult != null && deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {deleteResult.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                    }
                    else
                        OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
                }
                else
                    OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

        public OASISResult<T> RemoveZome<T>(IZome zome, Guid avatarId, bool deleteZome = true, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = string.Concat("Error in CelestialBodyCore.RemoveZome<T> calling zome.Save<T> method with ", LoggingHelper.GetHolonInfoForLogging(zome, "zome"), ". Reason:");

            try
            {
                if (deleteZome)
                {
                    OASISResult<IHolon> deleteResult = zome.Delete(avatarId, softDelete, providerType);

                    if (deleteResult != null && deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {deleteResult.Message}");
                        OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = result.Exception });
                    }
                    else
                        OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
                }
                else
                    OnZomeRemoved?.Invoke(this, new ZomeRemovedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = result.Message, Result = OASISResultHelper.CopyResultToIZome(result), Exception = ex });
            }

            return result;
        }

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

        protected virtual async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsAsync(IEnumerable<IHolon> holons, HolonType holonType, bool refresh = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons == null || refresh)
                //result = await base.LoadHolonsForParentAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);
                result = await base.LoadChildHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);
            else
            {
                result.Message = "Refresh not required";
                result.Result = holons;
            }

            return result;
        }
    }
}
