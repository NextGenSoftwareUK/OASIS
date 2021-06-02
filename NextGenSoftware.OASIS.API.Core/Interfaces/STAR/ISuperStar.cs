
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISuperStar : IStar
    {
        List<IStar> Stars { get; set; }
    }
}