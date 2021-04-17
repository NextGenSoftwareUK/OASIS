using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.Interfaces
{
    public interface IStar
    {
        List<IPlanet> Planets { get; set; }
    }
}