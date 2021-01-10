using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR
{
    public interface IPlanet : ICelestialBody, OASIS.API.Core.IPlanet
    {
        public List<IMoon> Moons { get; set; }
    }
}