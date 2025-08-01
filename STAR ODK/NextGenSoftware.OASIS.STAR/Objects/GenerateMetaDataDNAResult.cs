
using NextGenSoftware.OASIS.STAR.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class GenerateMetaDataDNAResult : IGenerateMetaDataDNAResult
    {
        public string CelestialBodyMetaDataDNAPath { get; set; }
        public string HolonMetaDataDNAPath { get; set; }
        public string ZomeMetaDataDNAPath { get; set; }

        //public ISTARNETHolon CelestialBodyMetaDataDNA { get; set; }
        //public ISTARNETHolon ZomeMetaDataDNA { get; set; }
        //public ISTARNETHolon HolonMetaDataDNA { get; set; }
    }
}
