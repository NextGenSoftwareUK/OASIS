using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class EleventhDimension : OmniverseDimension, IEleventhDimension
    {
        public EleventhDimension(IOmiverse omniverse = null) : base(omniverse)
        {
            Init(omniverse);
        }

        public EleventhDimension(Guid id, IOmiverse omniverse = null) : base(id, omniverse)
        {
            Init(omniverse);
        }

        //public EleventhDimension(Dictionary<ProviderType, string> providerKey, IOmiverse omniverse = null) : base(providerKey, omniverse)
        //{
        //    Init(omniverse);
        //}

        public EleventhDimension(string providerKey, ProviderType providerType, IOmiverse omniverse = null) : base(providerKey, providerType, omniverse)
        {
            Init(omniverse);
        }

        private void Init(IOmiverse omniverse = null)
        {
            if (this.Id == Guid.Empty)
                this.Id = Guid.NewGuid();

            this.Name = "The Eleventh Dimension";
            this.Description = "Coming Soon...";
            this.DimensionLevel = DimensionLevel.Eleventh;
            this.SuperVerse.Name = $"{this.Name} SuperVerse";
        }
    }
}