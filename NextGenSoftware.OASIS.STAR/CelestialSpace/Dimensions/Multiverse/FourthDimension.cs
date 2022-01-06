using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class FourthDimension : MultiverseDimension, IFourthDimension
    {
        public IUniverse Universe { get; set; }

        public FourthDimension(IMultiverse multiverse = null) : base(multiverse)
        {
            Init(multiverse);
        }

        public FourthDimension(Guid id, IMultiverse multiverse = null) : base(id, multiverse)
        {
            Init(multiverse);
        }

        public FourthDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey, multiverse)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            this.Name = "The Fourth Dimension";
            this.Description = "The Astral Plane.";
            this.DimensionLevel = DimensionLevel.Fourth;
            Universe = new Universe(multiverse);
        }
    }
}