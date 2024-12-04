using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOAPPDNA
    {
        //ICelestialBody CelestialBody { get; set; } //optional
        Guid CelestialBodyId { get; set; }
        string CelestialBodyName { get; set; }
        HolonType CelestialBodyType { get; set; }
        //IEnumerable<IZome> Zomes { get; set; }
        Guid CreatedByAvatarId { get; set; }
        //public string CreatedByAvatarName { get; set; }
        public string CreatedByAvatarUsername { get; set; }
        DateTime CreatedOn { get; set; }
        string Description { get; set; }
        GenesisType GenesisType { get; set; }
        Guid OAPPId { get; set; }
        string OAPPName { get; set; }
        OAPPType OAPPType { get; set; }
        Guid PublishedByAvatarId { get; set; }
        //public string PublishedByAvatarName { get; set; }
        public string PublishedByAvatarUsername { get; set; }
        DateTime PublishedOn { get; set; }
        public string OAPPPublishedPath { get; set; }
        public string OAPPSourcePublishedPath { get; set; }
        public bool OAPPSourcePublishedOnSTARNET { get; set; }
        public bool OAPPPublishedOnSTARNET { get; set; }
        public bool OAPPPublishedToCloud { get; set; }
        public ProviderType OAPPPublishedProviderType { get; set; }
        public long OAPPSourceFileSize { get; set; }
        public long OAPPFileSize { get; set; }
        bool IsActive { get; set; }
        public string LaunchTarget { get; set; }
        string Version { get; set; }
        public string STARODKVersion { get; set; }
        public string OASISVersion { get; set; }
        public string COSMICVersion { get; set; }
    }
}