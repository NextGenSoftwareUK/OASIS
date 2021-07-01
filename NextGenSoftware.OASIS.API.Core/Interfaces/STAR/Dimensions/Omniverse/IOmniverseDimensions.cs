
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
    }
}