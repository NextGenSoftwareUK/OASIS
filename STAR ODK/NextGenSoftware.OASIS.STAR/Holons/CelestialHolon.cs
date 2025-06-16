using System;
using System.Drawing;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.Holons
{
    public abstract class CelestialHolon : Holon, ICelestialHolon
    //public abstract class CelestialHolon : STARHolon, ICelestialHolon
    {
        public GenesisType GenesisType { get; set; }
        //public OASISAPIManager OASISAPI = new OASISAPIManager(new List<IOASISProvider>() { new SEEDSOASIS() });

        public bool IsInitialized { get; set; }

        public event Initialized OnInitialized;
        //public event CelestialHolonLoaded OnCelestialHolonLoaded;
        //public event CelestialHolonSaved OnCelestialHolonSaved;
        //public event CelestialHolonError OnCelestialHolonError;
        //public event HolonsLoaded OnHolonsLoaded;
        //public event HolonSaved OnHolonSaved;
        //public event HolonLoaded OnHolonLoaded;

        public SpaceQuadrantType SpaceQuadrant { get; set; }
        public int SpaceSector { get; set; }
        public float SuperGalacticLatitute { get; set; }
        public float SuperGalacticLongitute { get; set; }
        public float GalacticLatitute { get; set; }
        public float GalacticLongitute { get; set; }
        public float HorizontalLatitute { get; set; }
        public float HorizontalLongitute { get; set; }
        public float EquatorialLatitute { get; set; }
        public float EquatorialLongitute { get; set; }
        public float EclipticLatitute { get; set; }
        public float EclipticLongitute { get; set; }
        public Color Colour { get; set; }
        public long Size { get; set; }
        public int Radius { get; set; }
        public long Age { get; set; }
        public int Temperature { get; set; }

        public CelestialHolon(Guid id, HolonType holonType) : base(id)
        {
            this.HolonType = holonType;
            //Initialize();
        }

        public CelestialHolon(Guid id, HolonType holonType, IStar parentStar) : base(id)
        {
            this.HolonType = holonType;
            this.ParentStar = parentStar;

            //Initialize();
        }

        public CelestialHolon(Guid id, HolonType holonType, Guid parentStarId) : base(id)
        {
            this.HolonType = holonType;
            this.ParentStarId = parentStarId;

            //Initialize();
        }

        public CelestialHolon(string providerKey, ProviderType providerType, HolonType holonType) : base(providerKey, providerType)
        {
            this.HolonType = holonType;
            //Initialize(); 
        }

        public CelestialHolon(string providerKey, ProviderType providerType, HolonType holonType, IStar parentStar) : base(providerKey, providerType)
        {
            this.HolonType = holonType;
            this.ParentStar = parentStar;
            //Initialize(); 
        }

        public CelestialHolon(string providerKey, ProviderType providerType, HolonType holonType, Guid parentStarId) : base(providerKey, providerType)
        {
            this.HolonType = holonType;
            this.ParentStarId = parentStarId;
            //Initialize(); 
        }

        //public CelestialHolon(Dictionary<ProviderType, string> providerKeys, HolonType holonType) : base(providerKeys)
        //{
        //    this.HolonType = holonType;
        //    //Initialize(); 
        //}

        public CelestialHolon(HolonType holonType) : base(holonType)
        {
            //Initialize();
        }

        protected virtual async Task InitializeAsync()
        {
            IsInitialized = true;
            OnInitialized?.Invoke(this, new System.EventArgs());
        }

        protected virtual void Initialize()
        {
            IsInitialized = true;
            OnInitialized?.Invoke(this, new System.EventArgs());
        }

        //TODO: Be nice to make Save Generic also, but then CelesitalBody and CelestialSpace would need to return CelesitalHolons...

        //public abstract Task<OASISResult<ICelestialHolon>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialHolon, new();
        //public abstract OASISResult<ICelestialHolon> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialHolon, new();
        //public abstract Task<OASISResult<IHolon>> LoadAsync(bool loadChildren, bool continueOnError); //TODO: May move more generic load logic here later...
        //public abstract OASISResult<IHolon> Load(bool loadChildren, bool continueOnError); //TODO: May move more generic load logic here later...
    }
}