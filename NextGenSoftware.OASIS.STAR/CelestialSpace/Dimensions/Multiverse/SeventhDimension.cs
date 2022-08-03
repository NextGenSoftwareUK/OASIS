using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SeventhDimension : MultiverseDimension, ISeventhDimension
    {
        public IUniverse Universe { get; set; }

        public SeventhDimension(IMultiverse multiverse = null) : base(multiverse)
        {
            Init(multiverse);
        }

        public SeventhDimension(Guid id, IMultiverse multiverse = null) : base(id, multiverse)
        {
            Init(multiverse);
        }

        //public SeventhDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey, multiverse)
        //{
        //    Init(multiverse);
        //}

        public SeventhDimension(string providerKey, ProviderType providerType, IMultiverse multiverse = null) : base(providerKey, providerType, multiverse)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            this.Name = "The Seventh Dimension";
            this.Description = "The Asscended Masters reside here.";
            this.DimensionLevel = DimensionLevel.Seventh;
            Universe = new Universe(this) { Name = $"{this.Name} Universe" };
            base.RegisterCelestialSpaces(new List<ICelestialSpace>() { Universe });
        }
    }
}