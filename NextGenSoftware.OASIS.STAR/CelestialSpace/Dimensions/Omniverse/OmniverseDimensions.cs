using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    // OmniverseDimensions spamn all multiverses/universes.
    public class OmniverseDimensions : IOmniverseDimensions
    {
        public OmniverseDimensions(IOmiverse omiverse = null)
        {
            EighthDimension = new EighthDimension(omiverse);
            NinthDimension = new NinthDimension(omiverse);
            TenthDimension = new TenthDimension(omiverse);
            EleventhDimension = new EleventhDimension(omiverse);
            TwelfthDimension = new TwelthDimension(omiverse);
        }

        public IEighthDimension EighthDimension { get; set; }
        public INinthDimension NinthDimension { get; set; }
        public ITenthDimension TenthDimension { get; set; }
        public IEleventhDimension EleventhDimension { get; set; }
        public ITwelfthDimension TwelfthDimension { get; set; }
        public List<IDimension> CustomDimensions { get; set; } = new List<IDimension>(); //TODO: This allows any custom dimensions to be added, but not sure if we should allow this? On one hand we want the engine/simulation to be as accurate as possible but on the other hand we want it to be as open and flexible as possible for expansion?
    }
}