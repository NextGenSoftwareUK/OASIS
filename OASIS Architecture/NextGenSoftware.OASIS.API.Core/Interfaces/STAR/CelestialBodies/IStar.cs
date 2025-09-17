
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IStar : ICelestialBody
    {
        int Luminosity { get; set; }
        StarType StarType { get; set; }
        StarClassification StarClassification { get; set; }
        StarBinaryType StarBinaryType { get; set; }
    }
}