using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class CelestialBodyMetaDataDNA : STARNETHolon//, ISTARNETDNA//, ISTARCelestialBody
    {
        public CelestialBodyMetaDataDNA() : base("CelestialBodyMetaDataDNAJSON")
        {
            this.HolonType = HolonType.CelestialBodyMetaDataDNA;
        }
    }
}