using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class FirstDimension : MultiverseDimension, IFirstDimension
    {
        public IUniverse Universe { get; set; }

        public FirstDimension(IMultiverse multiverse = null) : base(multiverse)
        {
            Init(multiverse);
        }

        public FirstDimension(Guid id, IMultiverse multiverse = null) : base(id, multiverse)
        {
            Init(multiverse);
        }

        //public FirstDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey, multiverse)
        //{
        //    Init(multiverse);
        //}

        public FirstDimension(string providerKey, ProviderType providerType, IMultiverse multiverse = null) : base(providerKey, providerType, multiverse)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            this.Name = "The First Dimension";
            this.Description = "The Core Crystal Of Gaia (the planet) - ancient friendly Galactic Societies exist in Hollow Earth waiting to make contact when we are finally ready... :)";
            this.DimensionLevel = DimensionLevel.First;
            Universe = new Universe(this) { Name = $"{this.Name} Universe" };
            base.AddCelestialSpace(Universe, false);
        }
    }
}