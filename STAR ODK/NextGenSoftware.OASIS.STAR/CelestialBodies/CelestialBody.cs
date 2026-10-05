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
    public abstract class CelestialBody<T> : CelestialHolon, ICelestialBody where T : ICelestialBody, new()
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

        // Activate & Launch - Launch & activate the planet (OApp) by shining the star's light upon it...
        public void Shine()
        {
            // Star.Shine(this);
        }

        // Deactivate the planet (OApp)
        public void Dim()
        {
            //Star.Dim(this);
        }

        // Deploy the planet (OApp)
        public void Seed()
        {
            //Star.Seed(this);
        }

        // Run Tests
        public void Twinkle()
        {
            //Star.Twinkle(this);
        }

        // Highlight the Planet (OApp) in the OApp Store (StarNET). *Admin Only*
        public void Radiate()
        {
            //Star.Radiate(this);
        }

        // Show how much light the planet (OApp) is emitting into the solar system (StarNET/HoloNET)
        public void Emit()
        {
            // Star.Emit(this);
        }

        // Show stats of the Planet (OApp).
        public void Reflect()
        {
            // Star.Reflect(this);
        }

        // Upgrade/update a Planet (OApp).
        public void Evolve()
        {
            //Star.Evolve(this);
        }

        // Import/Export hApp, dApp & others.
        public void Mutate()
        {
            // Star.Mutate(this);
        }

        // Send/Receive Love
        public void Love()
        {
            // Star.Love(this);
        }

        // Reserved For Future Use...
        public void Super()
        {
            //Star.Super(this);
        }

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

        //protected async Task<OASISResult<ICelestialBody>> InitializeAsync(bool autoLoad = true)
        protected async Task<OASISResult<T>> InitializeAsync(bool autoLoad = true)
        {
            //OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<T> result = new OASISResult<T>();

            InitCelestialBodyCore();
            WireUpEvents();

            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                result = await LoadAsync<T>();

                if (result != null && !result.IsError && result.Result != null)
                    await base.InitializeAsync();
            }
            //else
            //    OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

            return result;
        }

        //protected OASISResult<ICelestialBody> Initialize(bool autoLoad = true)
        protected OASISResult<T> Initialize(bool autoLoad = true)
        {
            //OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<T> result = new OASISResult<T>();

            InitCelestialBodyCore();
            WireUpEvents();

            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                result = Load<T>();

                if (result != null && !result.IsError && result.Result != null)
                    base.Initialize();
            }
            //else
            //    OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

            return result;
        }

        /*
        protected async Task<OASISResult<T>> InitializeAsync<T>(bool autoLoad = true) where T : ICelestialBody, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            InitCelestialBodyCore();
            WireUpEvents();

            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialBody> celestialBodyResult = await LoadAsync<T>();

                OASISResultHelper<ICelestialBody, T>.CopyResult(celestialBodyResult, result);
                result.Result = (T)celestialBodyResult.Result;

                if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
                    await base.InitializeAsync();
            }
            //else
            //    OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

            return result;
        }

        protected OASISResult<T> Initialize<T>(bool autoLoad = true) where T : ICelestialBody, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            InitCelestialBodyCore();
            WireUpEvents();

            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialBody> celestialBodyResult = Load<T>();

                OASISResultHelper<ICelestialBody, T>.CopyResult(celestialBodyResult, result);
                result.Result = (T)celestialBodyResult.Result;

                if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
                    base.Initialize();
            }
            //else
            //    OASISErrorHandling.HandleWarning(ref result, "Warning in Initialize method in CelestialBody: Neither the Id or ProviderUniqueStorageKey have been set, at least one needs to be set.");

            return result;
        }*/

        private void InitCelestialBodyCore()
        {
            // The Id/ProviderUniqueStorageKey will be set from LoadCelestialBody/SetProperties.
            switch (this.HolonType)
            {
                case HolonType.Moon:
                    CelestialBodyCore = new MoonCore((IMoon)this);
                    break;

                case HolonType.Planet:
                    CelestialBodyCore = new PlanetCore((IPlanet)this);
                    break;

                case HolonType.Star:
                    CelestialBodyCore = new StarCore((IStar)this);
                    break;

                case HolonType.SuperStar:
                    CelestialBodyCore = new SuperStarCore((ISuperStar)this);
                    break;

                case HolonType.GrandSuperStar:
                    CelestialBodyCore = new GrandSuperStarCore((IGrandSuperStar)this);
                    break;

                case HolonType.GreatGrandSuperStar:
                    CelestialBodyCore = new GreatGrandSuperStarCore((IGreatGrandSuperStar)this);
                    break;
            }

            CelestialBodyCore.Id = this.Id;
            CelestialBodyCore.ProviderUniqueStorageKey = this.ProviderUniqueStorageKey;
        }

        private void WireUpEvents()
        {
            if (CelestialBodyCore != null)
            {
                ((CelestialBodyCore<T>)CelestialBodyCore).GlobalHolonData.OnHolonLoaded += CelestialBody_OnHolonLoaded;
                ((CelestialBodyCore<T>)CelestialBodyCore).GlobalHolonData.OnHolonSaved += CelestialBodyCore_OnHolonSaved;
                //((CelestialBodyCore<T>)CelestialBodyCore).OnHolonError += CelestialBody_OnHolonError;
                ((CelestialBodyCore<T>)CelestialBodyCore).GlobalHolonData.OnHolonsLoaded += CelestialBodyCore_OnHolonsLoaded;
                ((CelestialBodyCore<T>)CelestialBodyCore).GlobalHolonData.OnHolonsSaved += CelestialBody_OnHolonsSaved;
                //((CelestialBodyCore<T>)CelestialBodyCore).OnHolonsError += CelestialBody_OnHolonsError;
                //((CelestialBodyCore<T>)CelestialBodyCore).OnZomeLoaded += CelestialBody_OnZomeLoaded;
                //((CelestialBodyCore<T>)CelestialBodyCore).OnZomeSaved += CelestialBody_OnZomeSaved;
                ((CelestialBodyCore<T>)CelestialBodyCore).OnZomeAdded += CelestialBody_OnZomeAdded;
                ((CelestialBodyCore<T>)CelestialBodyCore).OnZomeRemoved += CelestialBody_OnZomeRemoved;
                ((CelestialBodyCore<T>)CelestialBodyCore).OnZomeError += CelestialBodyCore_OnZomeError;
                ((CelestialBodyCore<T>)CelestialBodyCore).OnZomesLoaded += CelestialBodyCore_OnZomesLoaded;
                ((CelestialBodyCore<T>)CelestialBodyCore).OnZomesSaved += CelestialBody_OnZomesSaved;
                ((CelestialBodyCore<T>)CelestialBodyCore).OnZomesError += CelestialBody_OnZomesError;
            }
        }

        private void SetProperties(IHolon holon)
        {
            this.Id = holon.Id;
            this.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
            this.CelestialBodyCore.Id = holon.Id;
            this.CelestialBodyCore.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
            this.Name = holon.Name;
            this.Description = holon.Description;
            this.HolonType = holon.HolonType;
            this.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            this.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            this.ParentGrandSuperStar = holon.ParentGrandSuperStar;
            this.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            this.ParentSuperStar = holon.ParentSuperStar;
            this.ParentSuperStarId = holon.ParentSuperStarId;
            this.ParentStar = holon.ParentStar;
            this.ParentStarId = holon.ParentStarId;
            this.ParentPlanet = holon.ParentPlanet;
            this.ParentPlanetId = holon.ParentPlanetId;
            this.ParentMoon = holon.ParentMoon;
            this.ParentMoonId = holon.ParentMoonId;
            this.ParentCelestialSpace = holon.ParentCelestialSpace;
            this.ParentCelestialSpaceId = holon.ParentCelestialSpaceId;
            this.ParentCelestialBody = holon.ParentCelestialBody;
            this.ParentCelestialBodyId = holon.ParentCelestialBodyId;
            this.ParentZome = holon.ParentZome;
            this.ParentZomeId = holon.ParentZomeId;
            this.ParentHolon = holon.ParentHolon;
            this.ParentHolonId = holon.ParentHolonId;
            this.ParentOmniverse = holon.ParentOmniverse;
            this.ParentOmniverseId = holon.ParentOmniverseId;
            this.ParentMultiverse = holon.ParentMultiverse;
            this.ParentMultiverseId = holon.ParentMultiverseId;
            this.ParentUniverse = holon.ParentUniverse;
            this.ParentUniverseId = holon.ParentUniverseId;
            this.ParentGalaxyCluster = holon.ParentGalaxyCluster;
            this.ParentGalaxyClusterId = holon.ParentGalaxyClusterId;
            this.ParentGalaxy = holon.ParentGalaxy;
            this.ParentGalaxyId = holon.ParentGalaxyId;
            this.ParentSolarSystem = holon.ParentSolarSystem;
            this.ParentSolarSystemId = holon.ParentSolarSystemId;
            this.Children = holon.Children;
            this.Nodes = holon.Nodes;
            this.CreatedByAvatar = holon.CreatedByAvatar;
            this.CreatedByAvatarId = holon.CreatedByAvatarId;
            this.CreatedDate = holon.CreatedDate;
            this.ModifiedByAvatar = holon.ModifiedByAvatar;
            this.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            this.ModifiedDate = holon.ModifiedDate;
            this.DeletedByAvatar = holon.DeletedByAvatar;
            this.DeletedByAvatarId = holon.DeletedByAvatarId;
            this.DeletedDate = holon.DeletedDate;
            this.Version = holon.Version;
            this.IsActive = holon.IsActive;
            this.IsChanged = holon.IsChanged;
            this.IsNewHolon = holon.IsNewHolon;
            this.MetaData = holon.MetaData;
            this.ProviderMetaData = holon.ProviderMetaData;
            this.Original = holon.Original;
        }


        // TODO: COME BACK TO SETTING THESE PARENTID'S, NEED TO NOT BE TIRED TO WORK IT ALL OUT! ;-) LOL
        // PLUS NOT EVEN SURE WE NEED TO DO THIS BECAUSE ALL THE ADD METHODS ALREADY SET THE PARENTID'S?!
        // MAYBE THEY CAN ADD ITEMS MANUALLY TO THE COLLECTIONS WITHOUT USING THE CORRECT ADDMETHODS? THIS IS WHERE THIS COULD BE NEEDED?
        private void SetParentIdsForMoon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon)
        {
            if (moon.CelestialBodyCore.Zomes != null)
            {
                foreach (Zome zome in moon.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome);
            }
        }

        private void SetParentIdsForPlanet(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet)
        {
            if (greatGrandSuperStar != null)
            {
                planet.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                planet.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                planet.ParentGreatGrandSuperStar = greatGrandSuperStar;
                planet.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                planet.ParentMultiverse = grandSuperStar.ParentMultiverse;
                planet.ParentMultiverseId = grandSuperStar.ParentMultiverseId;
                planet.ParentGrandSuperStar = grandSuperStar;
                planet.ParentGrandSuperStarId = grandSuperStar.Id;
            }

            if (superStar != null)
            {
                planet.ParentDimension = superStar.ParentDimension;
                planet.ParentDimensionId = superStar.ParentDimensionId;
                planet.ParentUniverse = superStar.ParentUniverse;
                planet.ParentUniverseId = superStar.ParentUniverseId;
                planet.ParentGalaxyCluster = superStar.ParentGalaxyCluster;
                planet.ParentGalaxyClusterId = superStar.ParentGalaxyCluster.Id;
                planet.ParentGalaxy = superStar.ParentGalaxy;
                planet.ParentGalaxyId = superStar.ParentGalaxy.Id;
                planet.ParentSuperStar = superStar;
                planet.ParentSuperStarId = superStar.Id;
            }

            if (star != null)
            {
                planet.ParentSolarSystem = star.ParentSolarSystem;
                planet.ParentSolarSystemId = star.ParentSolarSystem.Id;
                planet.ParentStar = star;
                planet.ParentStarId = star.Id;
            }

            if (planet.CelestialBodyCore.Zomes != null)
            {
                foreach (IZome zome in planet.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(greatGrandSuperStar, grandSuperStar, superStar, star, planet, null, zome);
            }

            if (planet.Moons != null)
            {
                foreach (IMoon moon in planet.Moons)
                    SetParentIdsForMoon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon);
            }
        }

        private void SetParentIdsForStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star)
        {
            star.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
            star.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
            star.ParentGreatGrandSuperStar = greatGrandSuperStar;
            star.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            star.ParentUniverse = grandSuperStar.ParentUniverse;
            star.ParentUniverseId = grandSuperStar.ParentUniverseId;
            star.ParentGrandSuperStar = grandSuperStar;
            star.ParentGrandSuperStarId = grandSuperStar.Id;

            star.ParentGalaxy = grandSuperStar.ParentGalaxy;
            star.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
            star.ParentSuperStar = superStar;
            star.ParentSuperStarId = superStar.Id;

            if (star.ParentSolarSystem.Planets != null)
            {
                foreach (IPlanet planet in star.ParentSolarSystem.Planets)
                {
                    SetParentIdsForPlanet(greatGrandSuperStar, grandSuperStar, superStar, star, planet);
                }
            }

            //TODO: Do we want to add Zomes to a Star? Maybe?
        }

        private void SetParentIdsForSuperStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar)
        {
            foreach (IStar star in superStar.ParentGalaxy.Stars)
            {
                // Stars outside of a Solar System (does not have any planets)
                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, superStar, star);
            }

            foreach (ISolarSystem solarSystem in superStar.ParentGalaxy.SolarSystems)
            {
                solarSystem.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                solarSystem.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                solarSystem.ParentGreatGrandSuperStar = greatGrandSuperStar;
                solarSystem.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                solarSystem.ParentUniverse = grandSuperStar.ParentUniverse;
                solarSystem.ParentUniverseId = grandSuperStar.ParentUniverseId;
                solarSystem.ParentGrandSuperStar = grandSuperStar;
                solarSystem.ParentGrandSuperStarId = grandSuperStar.Id;

                solarSystem.ParentGalaxy = grandSuperStar.ParentGalaxy;
                solarSystem.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
                solarSystem.ParentSuperStar = superStar;
                solarSystem.ParentSuperStarId = superStar.Id;

                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, superStar, solarSystem.Star);
            }
        }

        private void SetParentIdsForGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar)
        {
            Mapper.MapParentCelestialBodyProperties(greatGrandSuperStar, grandSuperStar);
            grandSuperStar.ParentGreatGrandSuperStar = greatGrandSuperStar;
            grandSuperStar.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FirstDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SecondDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.ThirdDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FourthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FifthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SixthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SeventhDimension, grandSuperStar);
        }

        private void SetParentIdsForMultiverseDimension(IMultiverseDimension dimension, IGrandSuperStar grandSuperStar)
        {
            Mapper.MapParentCelestialBodyProperties(grandSuperStar, dimension);
            Mapper.MapParentCelestialBodyProperties(dimension, dimension.Universe);
            dimension.Universe.ParentDimension = dimension;
            dimension.Universe.ParentDimensionId = dimension.Id;

            foreach (IStar star in dimension.Universe.Stars)
            {
                // Stars that are outside of a Galaxy (do not have a superstar).
                star.ParentUniverse = dimension.Universe;
                star.ParentUniverseId = dimension.Universe.Id;

                SetParentIdsForStar(null, grandSuperStar, null, star);
            }

            foreach (IPlanet planet in dimension.Universe.Planets)
            {
                // Planets that are outside of a Galaxy (do not have a superstar).
                planet.ParentUniverse = dimension.Universe;
                planet.ParentUniverseId = dimension.Universe.Id;

                SetParentIdsForPlanet(null, grandSuperStar, null, null, planet);
            }

            foreach (ISolarSystem solarSystem in dimension.Universe.SolarSystems)
            {
                // SolarSystems that are outside of a Galaxy (do not have a superstar).
                solarSystem.ParentUniverse = dimension.Universe;
                solarSystem.ParentUniverseId = dimension.Universe.Id;

                //TODO: Implement method below:
                //SetParentIdsForSolarSystems(greatGrandSuperStar, grandSuperStar, null, null, solarSystem);
            }

            foreach (INebula nebula in dimension.Universe.Nebulas)
            {
                // SolarSystems that are outside of a Galaxy (do not have a superstar).
                nebula.ParentUniverse = dimension.Universe;
                nebula.ParentUniverseId = dimension.Universe.Id;

                //TODO: Implement method below:
                //SetParentIdsForNebulas(greatGrandSuperStar, grandSuperStar, null, null, nebula);
            }

            //TODO: Add rest of CelestialBodies/Spaces in Universe here...

            foreach (IGalaxyCluster galaxyCluster in dimension.Universe.GalaxyClusters)
            {
                Mapper.MapParentCelestialBodyProperties(dimension.Universe, galaxyCluster);
                galaxyCluster.ParentUniverse = dimension.Universe;
                galaxyCluster.ParentUniverseId = dimension.Universe.Id;

                foreach (IGalaxy galaxy in galaxyCluster.Galaxies)
                {
                    Mapper.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                    galaxy.ParentGalaxyCluster = galaxyCluster;
                    galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                    //SetParentIdsForSuperStar(greatGrandSuperStar, grandSuperStar, galaxy.SuperStar);
                    SetParentIdsForSuperStar(null, grandSuperStar, galaxy.SuperStar);
                }
            }

            ThirdDimension thirdDimension = dimension as ThirdDimension;

            if (thirdDimension != null)
            {
                Mapper.MapParentCelestialBodyProperties(grandSuperStar, thirdDimension.MagicVerse);
                //Mapper.MapParentCelestialBodyProperties(grandSuperStar, thirdDimension.UniversePrime);

                foreach (IUniverse universe in thirdDimension.ParallelUniverses)
                    Mapper.MapParentCelestialBodyProperties(grandSuperStar, universe);
            }
        }

        private void SetParentIdsForGreatGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar)
        {
            foreach (IMultiverse multiverse in greatGrandSuperStar.ParentOmniverse.Multiverses)
            {
                Mapper.MapParentCelestialBodyProperties(greatGrandSuperStar, multiverse);
                multiverse.ParentGreatGrandSuperStar = greatGrandSuperStar;
                multiverse.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                foreach (IUniverse universe in multiverse.Dimensions.ThirdDimension.ParallelUniverses)
                {
                    universe.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                    universe.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                    universe.ParentGreatGrandSuperStar = greatGrandSuperStar;
                    universe.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                    //SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.GrandSuperStar);
                    SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.ParentGrandSuperStar);
                }
            }
        }

        //TODO: HOPE TO OBSOLETE THIS METHOD ASAP!
        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
        private void SetParentIds()
        {
            switch (this.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForGreatGrandSuperStar((IGreatGrandSuperStar)this);

                        //If the parent Omniverse is not already saving (and it's children) then begin saving them now...

                        //OBSOLETE: ALL CHILDREN ARE NOW SAVED IN HOLONMANAGER VIA THE CELESTIALBODY.SAVE METHOD (SAVES ALL CHILDREN RECURSIVELY VIA THE ALLCHILDEN PROPERTY).
                        //TODO: NEED TO CHECK WHY WE ARE CALLING SAVE ON THE PARENT OMNIVERSE? DO THE CHILDREN BELONG TO THE OMNIVERSE OR THE GREAT GRAND SUPER STAR?


                        //if (saveChildren && !((IGreatGrandSuperStar)this).ParentOmniverse.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((IGreatGrandSuperStar)this).ParentOmniverse.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (GreatGrandSuperStar) ParentOmniverse. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.GrandSuperStar:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForGrandSuperStar(this.ParentGreatGrandSuperStar, (IGrandSuperStar)this);

                        //if (saveChildren && !((IGrandSuperStar)this).ParentMultiverse.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((IGrandSuperStar)this).ParentMultiverse.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (GrandSuperStar) ParentMultiverse. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.SuperStar:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForSuperStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, (ISuperStar)this);

                        //if (saveChildren && !((ISuperStar)this).ParentGalaxy.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((ISuperStar)this).ParentGalaxy.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (SuperStar) ParentGalaxy. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.Star:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, (IStar)this);

                        //if (saveChildren && !((IStar)this).ParentSolarSystem.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((IStar)this).ParentSolarSystem.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (Star) ParentSolarSystem. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.Planet:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForPlanet(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, (IPlanet)this);
                    }
                    break;
            }
        }

        private void SetMetaData()
        {
            CelestialBodyCore = (ICelestialBodyCore)Mapper.MapBaseHolonProperties(this, CelestialBodyCore);

            //CelestialHolon Properties
            CelestialBodyCore.MetaData["Age"] = this.Age;
            CelestialBodyCore.MetaData["Colour"] = ColorTranslator.ToHtml(this.Colour);
            CelestialBodyCore.MetaData["EclipticLatitute"] = this.EclipticLatitute;
            CelestialBodyCore.MetaData["EclipticLongitute"] = this.EclipticLongitute;
            CelestialBodyCore.MetaData["EquatorialLatitute"] = this.EquatorialLatitute;
            CelestialBodyCore.MetaData["EquatorialLongitute"] = this.EquatorialLongitute;
            CelestialBodyCore.MetaData["GalacticLatitute"] = this.GalacticLatitute;
            CelestialBodyCore.MetaData["GalacticLongitute"] = this.GalacticLongitute;
            CelestialBodyCore.MetaData["HorizontalLatitute"] = this.HorizontalLatitute;
            CelestialBodyCore.MetaData["HorizontalLongitute"] = this.HorizontalLongitute;
            CelestialBodyCore.MetaData["Radius"] = this.Radius;
            CelestialBodyCore.MetaData["Size"] = this.Size;
            CelestialBodyCore.MetaData["SpaceQuadrant"] = this.SpaceQuadrant;
            CelestialBodyCore.MetaData["SpaceSector"] = this.SpaceSector;
            CelestialBodyCore.MetaData["SuperGalacticLatitute"] = this.SuperGalacticLatitute;
            CelestialBodyCore.MetaData["SuperGalacticLongitute"] = this.SuperGalacticLongitute;
            CelestialBodyCore.MetaData["Temperature"] = this.Temperature;

            //CelestialBody Properties
            CelestialBodyCore.MetaData["CurrentOrbitAngleOfParentStar"] = this.CurrentOrbitAngleOfParentStar;
            CelestialBodyCore.MetaData["Density"] = this.Density;
            CelestialBodyCore.MetaData["DistanceFromParentStarInMetres"] = this.DistanceFromParentStarInMetres;
            CelestialBodyCore.MetaData["GravitaionalPull"] = this.GravitaionalPull;
            CelestialBodyCore.MetaData["Mass"] = this.Mass;
            CelestialBodyCore.MetaData["NumberActiveAvatars"] = this.NumberActiveAvatars;
            CelestialBodyCore.MetaData["NumberRegisteredAvatars"] = this.NumberRegisteredAvatars;
            CelestialBodyCore.MetaData["OrbitPositionFromParentStar"] = this.OrbitPositionFromParentStar;
            CelestialBodyCore.MetaData["RotationSpeed"] = this.RotationSpeed;
            CelestialBodyCore.MetaData["RotationPeriod"] = this.RotationPeriod;
            CelestialBodyCore.MetaData["TiltAngle"] = this.TiltAngle;
            CelestialBodyCore.MetaData["Weight"] = this.Weight;
        }

        private OASISResult<ICelestialBody> ProcessSaveResult(OASISResult<ICelestialBody> result, OASISResult<IZome> celestialBodyHolonResult)
        {
            OASISResultHelper.CopyResult(celestialBodyHolonResult, result);

            if (celestialBodyHolonResult != null && !celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
            {
                result.SavedCount++;
                //SetProperties(celestialBodyHolonResult.Result); //Redundant. TODO: Double check! ;-)
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} holon. Reason: {celestialBodyHolonResult.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });
            }

            if (result.WarningCount > 0)
            {
                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. All operations failed, please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. {result.SavedCount} operations did save correctly however. Please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                    result.IsSaved = true;
                }

                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Error, $"Error Creating CelestialBody {this.Name}. Reason: {result.Message}");
            }
            else
            {
                result.IsSaved = true;

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Success, $"CelestialBody {this.Name} Saved Successfully.");
            }

            IsSaving = false;
            OnCelestialBodySaved?.Invoke(this, new CelestialBodySavedEventArgs() { Result = result });
            return result;
        }

        private OASISResult<T> ProcessSaveResult<T>(OASISResult<T> result) where T: IHolon
        {
            //OASISResultHelper.CopyResult<T1, T2>(celestialBodyHolonResult, result);

            if (result != null && !result.IsError && result.Result != null)
            {
                result.SavedCount++;
                //SetProperties(celestialBodyHolonResult.Result); //Redundant. TODO: Double check! ;-)
            }
            else
            {
                OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} holon. Reason: {result.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }

            if (result.WarningCount > 0)
            {
                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. All operations failed, please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. {result.SavedCount} operations did save correctly however. Please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                    result.IsSaved = true;
                }

                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Error, $"Error Creating CelestialBody {this.Name}. Reason: {result.Message}");
            }
            else
            {
                result.IsSaved = true;

                if (STAR.IsStarIgnited && STAR.IsDetailedStatusUpdatesEnabled)
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Success, $"CelestialBody {this.Name} Created.");
            }

            IsSaving = false;
            OnCelestialBodySaved?.Invoke(this, new CelestialBodySavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            return result;
        }

        /*
        private async Task<bool> SaveZomesAndHolons()
        {
            foreach (ZomeBase zome in Zomes)
            {
                //TODO: Need to check if any state has changed and only save if it has...
                //await zome.SaveHolonAsync(zome);
                await zome.SaveHolonAsync(this.RustHolonType, zome); //TODO: FIX ASAP!

                foreach (Holon holon in zome.Holons)
                {
                    //TODO: Need to check if any state has changed and only save if it has...
                    await zome.SaveHolonAsync(this.RustHolonType, holon);
                }
            }

            return true;
        }
        */

        public IZome GetZomeThatHolonBelongsTo(IHolon holon)
        {
            return (IZome)CelestialBodyCore.AllChildren.FirstOrDefault(x => x.Id == holon.Id).ParentHolon;
        }

        public List<IHolon> GetHolonsThatBelongToZome(IZome zome)
        {
            return CelestialBodyCore.AllChildren.Where(x => x.ParentHolon.Id == zome.Id).ToList();
        }

        public IZome GetZomeByName(string name)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Name == name);
        }

        public IZome GetZomeById(Guid id)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Id == id);
        }
        private void CelestialBody_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeAdded(object sender, ZomeAddedEventArgs e)
        {
            OnZomeAdded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeRemoved(object sender, ZomeRemovedEventArgs e)
        {
            OnZomeRemoved?.Invoke(sender, e);
        }

        private void CelestialBodyCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private async void CelestialBodyCore_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);

            // TODO: Dont think this is needed now?
            // This was going to load each of the zomes holons once the zomes were loaded for this Planet. 
            // But maybe it is better to allow them be lazy loaded as and when they are needed rather than pulling them all back in one go?
            // Trade offs between the 2 approaches... for now we leave as lazy loading so will only load when they are needed...

            /*
            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                await zome.Initialize(zome.Name, this.HoloNETClient);
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }*/

            //TODO: Not sure whether to delegate holons being loaded by zomes if can just load direct from PlanetCore?
            //Nice for Zomes to manage their own collections of holons (good practice) so will see... :)
        }

        private void CelestialBody_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private async void CelestialBodyCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);

            // 10/12/21: OBSOLETE: NO LONGER NEEDED, ZOMES/HOLONS ARE AUTOMATICALLY SAVED WHEN CELESTIALBODY IS (IF SAVECHILDREN PARAM IS SET TO TRUE, OTHERWISE CAN CALL SAVEZOMES LATER TO SAVE ALL ZOMES.
            // 10/12/21: TODO:     MAY ADD ABILITY TO SAVE INDIVIDUAL ZOMES/HOLONS BY EITHER NAME/ID/PROVIDERKEY

            /*
            //TODO: Come back to this...
            return;

            //TODO: Handle error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Planet)
                {
                    // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                    this.ProviderUniqueStorageKey = e.Result.Result.ProviderUniqueStorageKey;

                    //Just in case the zomes/holons have been added since the planet was last saved.
                    foreach (Zome zome in CelestialBodyCore.Zomes)
                    {
                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }

                        zome.ParentHolonId = this.Id;
                        zome.ParentHolon = this;

                        // TODO: Need to sort this.Holons collection too (this is a list of ALL holons that belong to ALL zomes for this planet.
                        // So the same holon will be in both collections, just that this.Holons has been flatterned. Why it's Fractal Holonic! ;-)
                        foreach (Holon holon in zome.Holons)
                        {
                            holon.ParentHolon = zome;
                            holon.ParentHolonId = zome.Id;

                            switch (HolonType)
                            {
                                case HolonType.Star:
                                    zome.ParentStar = (IStar)this;
                                    zome.ParentStarId = this.Id;
                                    break;

                                case HolonType.Planet:
                                    zome.ParentPlanet = (IPlanet)this;
                                    zome.ParentPlanetId = this.Id;
                                    break;

                                case HolonType.Moon:
                                    zome.ParentMoon = (IMoon)this;
                                    zome.ParentMoonId = this.Id;
                                    break;
                            }
                        }

                        await zome.SaveHolonsAsync(zome.Holons);
                    }
                }
            }*/
        }

        private void CelestialBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialBodyCore_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }


        //TODO: Come back to this, this is what is fired when each zome is loaded once the celestialbody is loaded but I think for now we will lazy load them later...
        private void Zome_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);

            // 10/12/21: OBSOLETE: NO LONGER NEEDED, ZOMES ARE AUTOMATICALLY LOADED WHEN CELESTIALBODY IS (IF LOADZOMES PARAM IS SET TO TRUE0, OTHERWISE CAN CALL LOADZOMES LATER TO LOAD ALL ZOMES.
            // 10/12/21: TODO:     MAY ADD ABILITY TO LOAD INDIVIDUAL ZOMES BY EITHER NAME/ID/PROVIDERKEY

            /*
            bool holonFound = false;

            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Id == e.Holon.Id)
                    {
                        holonFound = true;
                        break;
                    }
                }
            }

            // If the zome or holon is not stored in the cache yet then add it now...
            // Currently the collection will fill up as the individual zome loads each holon.
            // They can call the LoadAll function to load all Holons and Zomes linked to this Planet (OApp).

            //TODO: Now all zomes and holons belonging to a planet (OApp) are loaded in init method using hc anchor pattern.
            //Maybe it can be a setting to choose between lazy loading (loading only as needed) or to prefetch and load everything up front.
            //Pros and Cons to both methods, Lazy loading = quicker init load time and less memory but then if you start loading lots of zomes/holons after, that's a lot more network traffic, etc.
            //Loading up front- Longer init load time and uses more memory but then all data cached so no more loading or network traffic needed.

            if (!holonFound)
            {
                //IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);
                IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Id == e.Holon.Parent.Id);

                if (zome == null)
                {
                    zome = new Zome(e.Holon.Parent.Id);
                    zome.Holons.Add(e.Holon);
                    CelestialBodyCore.Zomes.Add(zome);
                    //CelestialBodyCore.Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));
                }

                ((ZomeBase)zome).Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
            */
        }

        private void Zome_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }
    }
}