using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISolarSystem : IHolon
    {
        IStar Star { get; set; }
        List<IPlanet> Planets { get; set; }
    }
}