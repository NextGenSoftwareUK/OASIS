using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects
{
    public interface IOAPPDNA
    {
        Guid CelestialBodyId { get; set; }
        Guid CreatedByAvtarId { get; set; }
        DateTime CreatedDate { get; set; }
        string Description { get; set; }
        GenesisType GenesisType { get; set; }
        Guid OAPPId { get; set; }
        string OAPPName { get; set; }
        OAPPType OAPPType { get; set; }
        Guid PublishedByAvatarId { get; set; }
        DateTime PublishedDate { get; set; }
        string Version { get; set; }
    }
}