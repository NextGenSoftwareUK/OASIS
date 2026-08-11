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
    }
}
