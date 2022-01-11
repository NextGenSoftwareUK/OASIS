using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SixthDimension : MultiverseDimension, ISixthDimension
    {
        public IUniverse Universe { get; set; }

        public SixthDimension(IMultiverse multiverse = null) : base(multiverse)
        {
            Init(multiverse);
        }

        public SixthDimension(Guid id, IMultiverse multiverse = null) : base(id, multiverse)
        {
            Init(multiverse);
        }

        public SixthDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey, multiverse)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            this.Name = "The Sixth Dimension";
            this.Description = "Sacred Geometry is found here - the building blocks of all that is such as the Flower of Life, etc.";
            this.DimensionLevel = DimensionLevel.Sixth;
            Universe = new Universe(this) { Name = $"{this.Name} Universe" };
            base.RegisterCelestialSpaces(new List<ICelestialSpace>() { Universe });
        }
    }
}