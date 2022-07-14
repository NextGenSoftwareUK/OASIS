using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    //https://solarstory.net/planets/
    public class Planet : CelestialBody<Planet>, IPlanet
    {
        //TODO: When you first create an OAPP, it needs to be a moon of the OurWorld planet, once they have raised their karma to 33 (master) 
        //then they can create a planet. The user needs to log into their avatar Star before they can create a moon/planet with the Genesis command.
        public List<IMoon> Moons { get; set; } = new List<IMoon>();
        public PlanetType PlanetType { get; set; }
        public PlanetSubType PlanetSubType { get; set; }
        public PlanetClassification PlanetClassification { get; set; }

        public Planet() : base(HolonType.Planet) { }
        public Planet(Guid id, bool autoLoad = true) : base(id, HolonType.Planet, autoLoad) {}
        public Planet(Dictionary<ProviderType, string> providerKey, bool autoLoad = true) : base(providerKey, HolonType.Planet, autoLoad) {}
    }
}
