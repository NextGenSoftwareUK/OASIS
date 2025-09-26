
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    // OmniverseDimensions spamn all multiverses/universes.
    public interface IOmniverseDimensions
    {
        IEighthDimension EighthDimension { get; set; }
        INinthDimension NinthDimension { get; set; }
        ITenthDimension TenthDimension { get; set; }
        IEleventhDimension EleventhDimension { get; set; }
        ITwelfthDimension TwelfthDimension { get; set; }
        List<IDimension> CustomDimensions { get; set; } //TODO: This allows any custom dimensions to be added, but not sure if we should allow this? On one hand we want the engine/simulation to be as accurate as possible but on the other hand we want it to be as open and flexible as possible for expansion?
    }
}