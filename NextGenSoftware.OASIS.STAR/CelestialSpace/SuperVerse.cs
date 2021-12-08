using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class SuperVerse : CelestialSpace, ISuperVerse
    {
        public List<IUniverse> Universes { get; set; } = new List<IUniverse>();

        public SuperVerse() : base(HolonType.SuperVerse) { }

        public SuperVerse(Guid id) : base(id, HolonType.SuperVerse) { }

        public SuperVerse(Dictionary<ProviderType, string> providerKey) : base(providerKey, HolonType.SuperVerse) { }
    }
}