using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // Physical Plane
    public interface IThirdDimension : IMultiverseDimension
    {
        // Primary Universe that we are in now.
      //  public IUniverse UniversePrime { get; set; } Universe inherited from MultiverseDimension is the UniversePrime.

        // The MagicVerse we will all co-create together. This is the Universe that OAPPs, SOAPPs, etc will appear when being created through the STAR ODK. The other universes (UniversePrime and ParallelUniverses) are part of the original OASIS Simulation and so are Read-Only but can be explored and reached through the SuperStars/GrandSuperStars...
        public IUniverse MagicVerse { get; set; }

        //Parallel Universes (everything that can happen does happen (Quantum Mechanics)).
        List<IUniverse> ParallelUniverses { get; set; }
    }
}