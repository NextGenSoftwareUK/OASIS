using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISolarSystem
    {
        IStar Star { get; set; }
        List<IPlanet> Planets { get; set; }
    }
}