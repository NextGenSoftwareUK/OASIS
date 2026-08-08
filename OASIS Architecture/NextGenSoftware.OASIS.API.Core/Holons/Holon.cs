using System;
using System.Collections.Generic;
using System.ComponentModel;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public partial class Holon : SemanticHolon, IHolon, INotifyPropertyChanged
    {
        // ── Constructors ─────────────────────────────────────────────────────

        public Holon(Guid id)
        {
            Id = id;
        }

        public Holon(string providerKey, ProviderType providerType)
        {
            if (providerType == ProviderType.Default)
                providerType = ProviderManager.Instance.CurrentStorageProviderType.Value;

            this.ProviderUniqueStorageKey[providerType] = providerKey;
        }

        public Holon(HolonType holonType)
        {
            IsNewHolon = true;
            HolonType = holonType;
        }

        public Holon()
        {
            IsNewHolon = true;
        }

        // ── Properties ───────────────────────────────────────────────────────

        public GlobalHolonData GlobalHolonData { get; set; } = new GlobalHolonData();

        /// <summary>
        /// Optional per-call override for holon MetaData encryption. Not persisted.
        /// Set this before calling SaveHolon to override the global HolonDataEncryption setting in OASISDNA.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public EncryptionSettings DataEncryptionOverride { get; set; }

        // ── Holonic location in the Omniverse hierarchy ───────────────────────
        // These apply to any holon — an avatar lives on a planet, a clan in a galaxy, etc.
        // Purely spatial/celestial properties (coordinates, size, temperature) live on
        // CelestialHolon (STAR ODK) via ICelestialHolon in Interfaces/STAR/.

        public IList<INode> Nodes { get; set; } = new List<INode>();
        public Guid ParentOmniverseId { get; set; }
        public IOmiverse ParentOmniverse { get; set; }
        public Guid ParentMultiverseId { get; set; }
        public IMultiverse ParentMultiverse { get; set; }
        public Guid ParentUniverseId { get; set; }
        public IUniverse ParentUniverse { get; set; }
        public Guid ParentDimensionId { get; set; }
        public IDimension ParentDimension { get; set; }
        public DimensionLevel DimensionLevel { get; set; }
        public SubDimensionLevel SubDimensionLevel { get; set; }
        public Guid ParentGalaxyClusterId { get; set; }
        public IGalaxyCluster ParentGalaxyCluster { get; set; }
        public Guid ParentGalaxyId { get; set; }
        public IGalaxy ParentGalaxy { get; set; }
        public Guid ParentSolarSystemId { get; set; }
        public ISolarSystem ParentSolarSystem { get; set; }
        public Guid ParentGreatGrandSuperStarId { get; set; }
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; }
        public Guid ParentGrandSuperStarId { get; set; }
        public IGrandSuperStar ParentGrandSuperStar { get; set; }
        public Guid ParentSuperStarId { get; set; }
        public ISuperStar ParentSuperStar { get; set; }
        public Guid ParentStarId { get; set; }
        public IStar ParentStar { get; set; }
        public Guid ParentPlanetId { get; set; }
        public IPlanet ParentPlanet { get; set; }
        public Guid ParentMoonId { get; set; }
        public IMoon ParentMoon { get; set; }
        public Guid ParentCelestialSpaceId { get; set; }
        public ICelestialSpace ParentCelestialSpace { get; set; }
        public Guid ParentCelestialBodyId { get; set; }
        public ICelestialBody ParentCelestialBody { get; set; }
        public Guid ParentZomeId { get; set; }
        public IZome ParentZome { get; set; }

        // ── Events ───────────────────────────────────────────────────────────

        public event EventDelegates.Initialized OnInitialized;
        public event EventDelegates.HolonError OnError;

        public event EventDelegates.HolonLoaded OnLoaded;
        public event EventDelegates.HolonSaved OnSaved;
        public event EventDelegates.HolonDeleted OnDeleted;
        public event EventDelegates.HolonAdded OnHolonAdded;
        public event EventDelegates.HolonRemoved OnHolonRemoved;
        public event EventDelegates.HolonsLoaded OnChildrenLoaded;
        public event EventDelegates.HolonsError OnChildrenLoadError;
    }
}
