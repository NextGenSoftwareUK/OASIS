using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.STAR.Holons;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public abstract class CelestialSpace : CelestialHolon, ICelestialSpace
    {
        public IEnumerable<ICelestialBody> CelestialBodies = new List<ICelestialBody>();

        public CelestialSpace(HolonType holonType) : base(holonType)
        {
            Initialize();  
        }

        public CelestialSpace(Guid id, HolonType holonType) : base(id, holonType)
        {
            Initialize();
        }

        public CelestialSpace(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        {
            Initialize();  
        }

        protected override void Initialize()
        {
            //TODO: Load and Wireup Events like CelestialBody, etc.
            base.Initialize();
        }

        protected override async Task InitializeAsync()
        {
            //TODO: Load and Wireup Events like CelestialBody, etc.
            await base.InitializeAsync();
        }

        public async Task<OASISResult<ICelestialSpace>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();

            //TODO: Need to save all CelestialBodies contained within this space.


        }

        public OASISResult<ICelestialSpace> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IHolon>> LoadAsync()
        {
            return new OASISResult<IHolon>();
            //return await CelestialBodyCore.LoadCelestialBodyAsync();
        }

        public override OASISResult<IHolon> Load()
        {
            return new OASISResult<IHolon>();
            // return CelestialBodyCore.LoadCelestialBody();
        }
    }
}