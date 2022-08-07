using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class CelestialBodyDNATemplate : CELESTIALBODY, ICELESTIALBODY
    {
        public CelestialBodyDNATemplate() : base(new Guid("ID")) { }
    }
}