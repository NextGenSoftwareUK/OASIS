using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class MultiverseDimension : Dimension, IMultiverseDimension
    {
        public IUniverse Universe { get; set; }

        public MultiverseDimension() : base()
        {
            Init();
        }

        public MultiverseDimension(IMultiverse multiverse = null) : base() 
        {
            Init(multiverse);
        }

        public MultiverseDimension(Guid id, IMultiverse multiverse = null) : base(id) 
        {
            Init(multiverse);
        }

        //public MultiverseDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey) 
        //{
        //    Init(multiverse);
        //}

        public MultiverseDimension(string providerKey, ProviderType providerType, IMultiverse multiverse = null) : base(providerKey, providerType)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            if (this.Id == Guid.Empty)
                this.Id = Guid.NewGuid();

            if (multiverse != null)
            {
                Mapper<IMultiverse, MultiverseDimension>.MapParentCelestialBodyProperties(multiverse, this);
                this.ParentMultiverse = multiverse;
                this.ParentMultiverseId = multiverse.Id;
            }
        }
    }
}