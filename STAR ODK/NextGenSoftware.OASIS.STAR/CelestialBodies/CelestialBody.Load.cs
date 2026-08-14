using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using System.Drawing;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract partial class CelestialBody<T> where T : ICelestialBody, new()
    {
        public new async Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = await CelestialBodyCore.LoadAsync<T>(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

            if (result != null && result.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialBody.LoadAsync<T> method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Reason: {result.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }
            else
            {
                result.Result = (T)Mapper.ConvertIHolonToICelestialBody(result.Result);
                OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }

            return result;
        }

        public new OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = CelestialBodyCore.Load<T>(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

            if (result != null && result.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialBody.Load<T> method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Reason: {result.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }
            else
            {
                result.Result = (T)Mapper.ConvertIHolonToICelestialBody(result.Result);
                OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = await CelestialBodyCore.LoadZomesAsync(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IZome>> result = CelestialBodyCore.LoadZomes(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = await CelestialBodyCore.LoadZomesAsync<T>(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            OASISResult<IEnumerable<T>> result = CelestialBodyCore.LoadZomes<T>(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs() { Result = OASISResultHelper.CopyResultToIZome(result) });
            return result;
        }

        public new async Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
            IsSaving = true;

            if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                STAR.ShowStatusMessage(Enums.StarStatusMessageType.Processing, $"Saving CelestialBody {this.Name}...");

            //TODO: CURRENTLY ZOMES ARE TREATED SEPERATELY TO CHILDREN BUT ONCE THEY ARE SYNCED/MERGED LIKE CELESTIALSPACE WE CAN REMOVE THIS BLOCK OF CODE BECAUSE THE CelestialBodyCore.SaveAsync CALL BELOW WILL AUTOMATICALLY SAVE ALL CHILDREN (INCLUDING ZOMES) IN HOLONMANAGER.
            /*
            if (saveChildren)
            {
                zomesResult = await SaveZomesAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (!(zomesResult != null && !zomesResult.IsError && zomesResult.Result != null))
                {
                    OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} zomes. Reason: {zomesResult.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                    if (!continueOnError)
                    {
                        IsSaving = false;
                        return result;
                    }
                }
                else
                    result.SavedCount++;
            }*/

            SetParentIds();
            SetMetaData();
            return ProcessSaveResult(result, await CelestialBodyCore.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType));
        }

        public new OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);

            IsSaving = true;

            if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                STAR.ShowStatusMessage(Enums.StarStatusMessageType.Processing, $"Creating CelestialBody {this.Name}...");


            SetParentIds();
            SetMetaData();
            return ProcessSaveResult(result, CelestialBodyCore.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType));

        }

        public new async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            IsSaving = true;

            if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                STAR.ShowStatusMessage(Enums.StarStatusMessageType.Processing, $"Creating CelestialBody {this.Name}...");

            SetParentIds();
            SetMetaData();
            return ProcessSaveResult(await CelestialBodyCore.SaveAsync<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType));

        }

        public new OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            IsSaving = true;

            if (!STAR.IsStarIgnited)
                STAR.ShowStatusMessage(Enums.StarStatusMessageType.Processing, $"Creating CelestialBody {this.Name}...");

            SetParentIds();
            SetMetaData();
            return ProcessSaveResult(CelestialBodyCore.Save<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType));
        }

        public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return await CelestialBodyCore.SaveZomesAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return CelestialBodyCore.SaveZomes(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        //TODO: Is this needed?
        //public async Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        //{
        //    return OASISResultHelperForHolons<IZome, T>.CopyResult(await SaveZomesAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType));
        //}

        //public OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        //{
        //    return OASISResultHelperForHolons<IZome, T>.CopyResult(SaveZomes(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType));
        //}

        ////TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        //public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        //{
        //    OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
        //    OASISResult<IZome> zomeResult = new OASISResult<IZome>();

        //    if (this.CelestialBodyCore.Zomes != null)
        //    {
        //        foreach (IZome zome in this.CelestialBodyCore.Zomes)
        //        {
        //            if (zome.HasHolonChanged())
        //            {
        //                zomeResult = await zome.SaveAsync(saveChildren, recursive, continueOnError);

        //                if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
        //                    result.SavedCount++;
        //                else
        //                {
        //                    result.ErrorCount++;
        //                    OASISErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBody.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
        //                    //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

        //                    if (!continueOnError)
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    if (result.ErrorCount > 0)
        //    {
        //        string message = $"{result.ErrorCount} Error(s) occured in CelestialBody.SaveZomes method whilst saving {CelestialBodyCore.Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

        //        if (result.SavedCount == 0)
        //            OASISErrorHandling.HandleError(ref result, message);
        //        else
        //        {
        //            OASISErrorHandling.HandleWarning(ref result, message);
        //            result.IsSaved = true;
        //        }

        //        OnZomeError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}");
        //    }
        //    else
        //        result.IsSaved = true;

        //    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    return result;
        //}

        //public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        //{
        //    OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
        //    OASISResult<IZome> zomeResult = new OASISResult<IZome>();

        //    if (this.CelestialBodyCore.Zomes != null)
        //    {
        //        foreach (IZome zome in this.CelestialBodyCore.Zomes)
        //        {
        //            if (zome.HasHolonChanged())
        //            {
        //                zomeResult = zome.Save(saveChildren, recursive, continueOnError);

        //                if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
        //                    result.SavedCount++;
        //                else
        //                {
        //                    result.ErrorCount++;
        //                    OASISErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBody.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
        //                    //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

        //                    if (!continueOnError)
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    if (result.ErrorCount > 0)
        //    {
        //        string message = $"{result.ErrorCount} Error(s) occured in CelestialBody.SaveZomes method whilst saving {CelestialBodyCore.Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

        //        if (result.SavedCount == 0)
        //            OASISErrorHandling.HandleError(ref result, message);
        //        else
        //        {
        //            OASISErrorHandling.HandleWarning(ref result, message);
        //            result.IsSaved = true;
        //        }

        //        OnZomeError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}");
        //    }
        //    else
        //        result.IsSaved = true;

        //    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    return result;
        //}

        // Build
        public ICoronalEjection Flare()
        {
            return new CoronalEjection();
            // return Star.Flare(this);
        }
    }
}
