using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.Collections.Generic;

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
        public List<IDimension> CustomDimensions { get; set; } //TODO: This allows any custom dimensions to be added, but not sure if we should allow this? On one hand we want the engine/simulation to be as accurate as possible but on the other hand we want it to be as open and flexible as possible for expansion?
    }
}