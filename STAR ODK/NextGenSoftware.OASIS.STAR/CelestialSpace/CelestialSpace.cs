using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public abstract partial class CelestialSpace : CelestialHolon, ICelestialSpace
    {
        private List<IHolon> _allchildren = new List<IHolon>();
        private List<ICelestialSpace> _celestialSpaces = new List<ICelestialSpace>();
        private List<ICelestialBody> _celestialBodies = new List<ICelestialBody>();
        public event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        public event CelestialSpaceSaved OnCelestialSpaceSaved;
        public event CelestialSpaceAdded OnCelestialSpaceAdded;
        public event CelestialSpaceRemoved OnCelestialSpaceRemoved;
        public event CelestialSpaceError OnCelestialSpaceError;
        public event CelestialSpacesLoaded OnCelestialSpacesLoaded;
        public event CelestialSpacesSaved OnCelestialSpacesSaved;
        public event CelestialSpacesError OnCelestialSpacesError;
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyAdded OnCelestialBodyAdded;
        public event CelestialBodyRemoved OnCelestialBodyRemoved;
        public event CelestialBodyError OnCelestialBodyError;
        public event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        public event CelestialBodiesSaved OnCelestialBodiesSaved;
        public event CelestialBodiesError OnCelestialBodiesError;

        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
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

        public IStar NearestStar { get; set; }

        public ReadOnlyCollection<ICelestialSpace> CelestialSpaces
        {
            get
            {
                return _celestialSpaces.AsReadOnly();
            }
        }

        public ReadOnlyCollection<ICelestialBody> CelestialBodies
        {
            get
            {
                return _celestialBodies.AsReadOnly();
            }
        }

        //public override ReadOnlyCollection<IHolon> Children
        //{
        //    get
        //    {
        //        return _allchildren.AsReadOnly();
        //    }
        //}

        public override ReadOnlyCollection<IHolon> AllChildren
        {
            get
            {
                _allchildren.Clear();
                _allchildren.AddRange(Children);
                _allchildren.AddRange(CelestialBodies);
                _allchildren.AddRange(CelestialSpaces);

                return _allchildren.AsReadOnly();
            }
        }

        public CelestialSpace(HolonType holonType) : base(holonType)
        {
            Initialize();
        }

        public CelestialSpace(Guid id, HolonType holonType, bool autoLoad = true) : base(id, holonType)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(Guid id, HolonType holonType, IStar parentStar, bool autoLoad = true) : base(id, holonType, parentStar)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(Guid id, HolonType holonType, Guid parentStarId, bool autoLoad = true) : base(id, holonType, parentStarId)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(string providerKey, ProviderType providerType, HolonType holonType, bool autoLoad = true) : base(providerKey, providerType, holonType)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(string providerKey, ProviderType providerType, HolonType holonType, IStar parentStar, bool autoLoad = true) : base(providerKey, providerType, holonType, parentStar)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(string providerKey, ProviderType providerType, HolonType holonType, Guid parentStarId, bool autoLoad = true) : base(providerKey, providerType, holonType, parentStarId)
        {
            Initialize(autoLoad);
        }

        protected void Initialize(bool autoLoad = true)
        {
            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialSpace> celestialSpaceResult = Load();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    base.Initialize();
            }
        }

        protected async Task InitializeAsync(bool autoLoad = true)
        {
            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialSpace> celestialSpaceResult = await LoadAsync();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    await base.InitializeAsync();
            }
        }
    }
}
