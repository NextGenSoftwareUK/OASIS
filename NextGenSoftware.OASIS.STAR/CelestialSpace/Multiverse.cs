using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class Multiverse : CelestialSpace, IMultiverse
    {
        public IGrandSuperStar GrandSuperStar { get; set; } = new GrandSuperStar() { CreatedOASISType = new API.Core.Helpers.EnumValue<API.Core.Enums.OASISType>(API.Core.Enums.OASISType.STARCLI)}; //Lets you jump between universes/dimensions within this multiverse.
        public IMultiverseDimensions Dimensions { get; set; } = new MultiverseDimensions();

        public Multiverse() : base(HolonType.Multiverse) { }

        public Multiverse(Guid id) : base(id, HolonType.Multiverse) { }

        public Multiverse(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.Multiverse) { }
    }
}