using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class CelestialBodyDNATemplate : {CELESTIALBODY}, {ICELESTIALBODY}
    {
        public CelestialBodyDNATemplate() : base(new Guid("ID")) { }
    }
}