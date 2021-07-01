using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IThirdDimension : IDimension
    {
        //Parallel Universes (everything that can happen does happen (Quantum Mechanics)).
        List<IUniverse> Universes { get; set; }
    }
}