using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class TwelthDimension : OmniverseDimension, ITenthDimension
    {
        public TwelthDimension(IOmiverse omniverse = null) : base(omniverse)
        {
            Init(omniverse);
        }

        public TwelthDimension(Guid id, IOmiverse omniverse = null) : base(id, omniverse)
        {
            Init(omniverse);
        }

        public TwelthDimension(Dictionary<ProviderType, string> providerKey, IOmiverse omniverse = null) : base(providerKey, omniverse)
        {
            Init(omniverse);
        }

        private void Init(IOmiverse omniverse = null)
        {
            this.Name = "The Twelth Dimension";
            this.Description = "Coming Soon...";
            this.DimensionLevel = DimensionLevel.Twelfth;
        }
    }
}