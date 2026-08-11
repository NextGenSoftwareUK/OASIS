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
    }
}
