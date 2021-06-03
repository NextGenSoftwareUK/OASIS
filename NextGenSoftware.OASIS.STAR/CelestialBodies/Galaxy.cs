using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class Galaxy : IGalaxy
    {
        public ISuperStar SuperStar { get; set; }
        public List<IStar> Stars { get; set; }
    }
}
