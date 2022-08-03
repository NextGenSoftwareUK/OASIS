using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class FifthDimension : MultiverseDimension, IFifthDimension
    {
        public IUniverse Universe { get; set; }

        public FifthDimension(IMultiverse multiverse = null) : base(multiverse)
        {
            Init(multiverse);
        }

        public FifthDimension(Guid id, IMultiverse multiverse = null) : base(id, multiverse)
        {
            Init(multiverse);
        }

        //public FifthDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey, multiverse)
        //{
        //    Init(multiverse);
        //}

        public FifthDimension(string providerKey, ProviderType providerType, IMultiverse multiverse = null) : base(providerKey, providerType, multiverse)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            this.Name = "The Fifth Dimension";
            this.Description = "Love/Wisdom/Unity Conciusouness dimension. We will be asscending to this dimension soon... ;-)";
            this.DimensionLevel = DimensionLevel.Fifth;
            Universe = new Universe(this) { Name = $"{this.Name} Universe"};
            base.RegisterCelestialSpaces(new List<ICelestialSpace>() { Universe });
        }
    }
}