using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPP : Holon, IOAPP
    {
        public OAPP()
        {
            this.HolonType = HolonType.OAPP;
        }

        public OAPPType OAPPType { get; set; }
        public GenesisType GenesisType { get; set; }
        //public ICelestialHolon CelestialHolon { get; set; } //The base CelestialHolon that represents this OAPP (planet, moon, star, solar system, galaxy etc).
        public ICelestialBody CelestialBody { get; set; } //The base CelestialBody that represents this OAPP (planet, moon, star, super star, grand super star, etc).
        public bool IsPublished { get; set; }

        //TODO:More to come! ;-)
    }
}
