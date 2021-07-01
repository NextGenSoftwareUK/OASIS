using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // OmniverseDimensions spamn all multiverses/universes.
    public class OmniverseDimensions : IOmniverseDimensions
    {
        public IEighthDimension EighthDimension { get; set; }
        public INinthDimension NinthDimension { get; set; }
        public ITenthDimension TenthDimension { get; set; }
        public IEleventhDimension EleventhDimension { get; set; }
        public ITwelfthDimension TwelfthDimension { get; set; }
    }
}