using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Omiverse : CelestialSpace, IOmiverse
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; } = new GreatGrandSuperStar() { CreatedOASISType = new API.Core.Helpers.EnumValue<API.Core.Enums.OASISType>(API.Core.Enums.OASISType.STARCLI) }; //let's you jump between Multiverses/Dimensions.
        public IOmniverseDimensions Dimensions { get; set; } = new OmniverseDimensions();
        public List<IMultiverse> Multiverses { get; set; } = new List<IMultiverse>();

        public Omiverse() : base(HolonType.Omiverse) { }

        public Omiverse(Guid id) : base(id, HolonType.Omiverse) { }

        public Omiverse(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Omiverse) { }
    }
}