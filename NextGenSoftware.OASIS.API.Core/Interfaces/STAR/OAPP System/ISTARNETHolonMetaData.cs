using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISTARNETHolonMetaData
    {
        Guid HolonId { get; set; }
        string Name { get; set; }
        Guid STARNETHolonId { get; set; }
        string Version { get; set; }
        int VersionSequence { get; set; }
    }
}