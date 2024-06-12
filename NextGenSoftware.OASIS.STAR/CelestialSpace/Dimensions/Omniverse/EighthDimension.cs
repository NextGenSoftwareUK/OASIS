using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class EighthDimension : OmniverseDimension, IEighthDimension
    {
        public EighthDimension(IOmiverse omniverse = null) : base(omniverse)   
        {
            Init(omniverse);
        }

        public EighthDimension(Guid id, IOmiverse omniverse = null) : base(id, omniverse)
        {
            Init(omniverse);
        }

        //public EighthDimension(Dictionary<ProviderType, string> providerKey, IOmiverse omniverse = null) : base(providerKey, omniverse)
        //{
        //    Init(omniverse);
        //}

        public EighthDimension(string providerKey, ProviderType providerType, IOmiverse omniverse = null) : base(providerKey, providerType, omniverse)
        {
            Init(omniverse);
        }

        private void Init(IOmiverse omniverse = null)
        {
            if (this.Id == Guid.Empty)
                this.Id = Guid.NewGuid();

            this.Name = "The Eighth Dimension";
            this.Description = "Coming Soon...";
            this.DimensionLevel = DimensionLevel.Eighth;
            this.SuperVerse.Name = $"{this.Name} SuperVerse";
        }
    }
}