using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Dimension : CelestialSpace, IDimension
    {
        public Dimension() : base(HolonType.Dimension) 
        {

        }

        public Dimension(Guid id) : base(id, HolonType.Dimension) 
        {

        }

        //public Dimension(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Dimension) 
        //{

        //}

        public Dimension(string providerKey, ProviderType providerType) : base(providerKey, providerType, HolonType.Dimension)
        {

        }
    }
}