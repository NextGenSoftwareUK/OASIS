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
    //public abstract class CelestialBody<T> : CelestialHolon, ICelestialBody where T : ICelestialBody, new()
    public abstract partial class CelestialBody<T> : CelestialHolon, ICelestialBody where T : ICelestialBody, new()
    {
        public ICelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the star/planet/moon/etc (OApp), which links to all the other stars/planets/moons/etc/zomes/holons...
                                                                  
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomeAdded OnZomeAdded;
        public event ZomeRemoved OnZomeRemoved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        public event HolonLoaded OnHolonLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonError OnHolonError;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonsSaved OnHolonsSaved;
        public event HolonsError OnHolonsError;

        //[BsonIgnore]
        public new Guid Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;

                if (CelestialBodyCore != null)
                    CelestialBodyCore.Id = value;
            }
        }

        //[BsonIgnore]
        public new Dictionary<ProviderType, string> ProviderUniqueStorageKey
        {
            get
            {
                return base.ProviderUniqueStorageKey;
            }
            set
            {
                base.ProviderUniqueStorageKey = value;

                if (CelestialBodyCore != null)
                    CelestialBodyCore.ProviderUniqueStorageKey = value;
            }
        }

        public override ReadOnlyCollection<IHolon> AllChildren
        {
            get
            {
                if (CelestialBodyCore != null)
                    return (ReadOnlyCollection<IHolon>)CelestialBodyCore.AllChildren;
                else
                    return Children.AsReadOnly();
            }
        }

        public long Mass { get; set; }
        public long Density { get; set; }
        public long RotationPeriod { get; set; } //How long it takes to rotate on its axis.
        public long OrbitPeriod { get; set; } //How long it takes to orbit its ParentStar.
        public long Weight { get; set; }
        public long GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        //public int OrbitPositionFromParentSuperStar { get; set; } //Only applies to SolarSystems. //TODO: Maybe better to make SolarSystem.ParentStar point to the SuperStar it orbits rather than the Star at the centre of it?
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public long DistanceFromParentStarInMetres { get; set; }
        public long RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NumberActiveAvatars { get; set; }

        public CelestialBody(Guid id, HolonType holonType, bool autoLoad = true) : base(id, holonType)
        {
            Initialize(autoLoad);
        }

        public CelestialBody(string providerKey, ProviderType providerType, HolonType holonType, bool autoLoad = true) : base(providerKey, providerType, holonType)
        {
            Initialize(autoLoad);
        }

        //public CelestialBody(Dictionary<ProviderType, string> providerKeys, HolonType holonType, bool autoLoad = true) : base(providerKeys, holonType)
        //{
        //    Initialize(autoLoad);
        //}

        public CelestialBody(HolonType holonType) : base(holonType)
        {
            Initialize();
        }

        ////TODO: Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic LoadAsync<T> version instead! 
        //[Obsolete("Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic LoadAsync<T> version instead!")]
        //public new async Task<OASISResult<ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = OASISResultHelper.CopyResultToICelestialBody(await CelestialBodyCore.LoadAsync(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType));
        //    //OASISResult<ICelestialBody> result = OASISResultHelper.CopyResultToICelestialBody(await CelestialBodyCore.GlobalHolonData.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType));

        //    if (result != null && result.IsError)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialBody.LoadAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Reason: {result.Message}");
        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = result });
        //    }
        //    else
        //        OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = result });

        //    return result;
        //}

        ////TODO: Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic Load<T> version instead! 
        //[Obsolete("Dont think this method works because impossible to cast to ICelestialBody when we dont know the type, use the generic Load<T> version instead!")]
        //public new OASISResult<ICelestialBody> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<ICelestialBody> result = OASISResultHelper.CopyResultToICelestialBody(CelestialBodyCore.Load(loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType));

        //    if (result != null && result.IsError)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialBody.Load method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Reason: {result.Message}");
        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = result });
        //    }
        //    else
        //        OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = result });

        //    return result;
        //}













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













        //protected async Task<OASISResult<ICelestialBody>> InitializeAsync(bool autoLoad = true)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();

        //    //InitCelestialBodyCore();
        //    //WireUpEvents();

        //    if (!IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
        //    {
        //        result = await LoadAsync<T>();

        //        if (result != null && !result.IsError && result.Result != null)
        //            await base.InitializeAsync();
        //    }
        //    else
        //        OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

        //    return result;
        //}

        //protected OASISResult<ICelestialBody> Initialize(bool autoLoad = true)
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();

        //    //InitCelestialBodyCore();
        //    //WireUpEvents();

        //    if (!IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
        //    {
        //        result = Load<T>();

        //        if (result != null && !result.IsError && result.Result != null)
        //            base.Initialize();
        //    }
        //    else
        //        OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

        //    return result;
        //}

        //protected async Task<OASISResult<T>> InitializeAsync<T>(bool autoLoad = true) where T : ICelestialBody, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();

        //    //InitCelestialBodyCore();
        //    //WireUpEvents();

        //    if (!IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
        //    {
        //        OASISResult<ICelestialBody> celestialBodyResult = await LoadAsync<T>();

        //        OASISResultHelper<ICelestialBody, T>.CopyResult(celestialBodyResult, result);
        //        result.Result = (T)celestialBodyResult.Result;

        //        if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
        //            await base.InitializeAsync();
        //    }
        //    else
        //        OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

        //    return result;
        //}

        //protected OASISResult<T> Initialize<T>(bool autoLoad = true) where T : ICelestialBody, new()
        //{
        //    OASISResult<T> result = new OASISResult<T>();

        //    InitCelestialBodyCore();
        //    WireUpEvents();

        //    if (!IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
        //    {
        //        OASISResult<ICelestialBody> celestialBodyResult = Load<T>();

        //        OASISResultHelper<ICelestialBody, T>.CopyResult(celestialBodyResult, result);
        //        result.Result = (T)celestialBodyResult.Result;

        //        if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
        //            base.Initialize();
        //    }
        //    else
        //        OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

        //    return result;
        //}








































    }
}
