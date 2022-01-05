using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class OmniverseDimension : Dimension, IOmniverseDimension
    {
        //TODO: Eighth Dimension and above is at the Omniverse level so spans ALL Multiverses/Universes so not sure what we would have here? Needs more thought...
        public OmniverseDimension(IOmiverse omniverse = null)
        {
            SuperVerse = new SuperVerse(omniverse);
        }

        public ISuperVerse SuperVerse { get; set; }
    }
}