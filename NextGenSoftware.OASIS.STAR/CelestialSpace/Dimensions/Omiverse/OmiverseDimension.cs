using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class OmniverseDimension : Dimension, IOmniverseDimension
    {
        //TODO: Eighth Dimension and above is at the Omiverse level so spans ALL Multiverses/Universes so not sure what we would have here? Needs more thought...
        public ISuperVerse SuperVerse { get; set; } = new SuperVerse();
    }
}