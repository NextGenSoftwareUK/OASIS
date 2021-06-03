using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IGalaxy
    {
        ISuperStar SuperStar { get; set; }
        List<IStar> Stars { get; set; }
    }
}