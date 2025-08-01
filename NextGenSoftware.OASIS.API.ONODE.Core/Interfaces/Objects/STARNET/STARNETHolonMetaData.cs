using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects.STARNET
{
    public class STARNETHolonMetaData : ISTARNETHolonMetaData
    {
        public Guid HolonId { get; set; }
        public Guid STARNETHolonId { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public int VersionSequence { get; set; }
    }
}
