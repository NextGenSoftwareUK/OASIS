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
        //public string OAPPWithSTARAndOASISRunTimePublishedPath { get; set; }
        //public string OAPPWithSTARAndOASISRunTimeAndDotNetPublishedPath { get; set; }
        //public bool OAPPPublishedOnSTARNET { get; set; }
        //public bool OAPPPublishedToCloud { get; set; }
        //public bool OAPPWithSTARAndOASISRunTimePublishedToCloud { get; set; }
        //public bool OAPPWithSTARAndOASISRunTimeAndDotNetPublishedToCloud { get; set; }
        //public ProviderType OAPPPublishedProviderType { get; set; }
        //public ProviderType OAPPWithSTARAndOASISRunTimePublishedProviderType { get; set; }
        //public ProviderType OAPPWithSTARAndOASISRunTimeAndDotNetPublishedProviderType { get; set; }
        //public long OAPPFileSize { get; set; }
        //public long OAPPWithSTARAndOASISRunTimeFileSize { get; set; }
        //public long OAPPWithSTARAndOASISRunTimeAndDotNetFileSize { get; set; }
        public string OAPPSelfContainedPublishedPath { get; set; } //Contains the STAR & OASIS runtimes.
        public string OAPPSelfContainedFullPublishedPath { get; set; } //Contains the STAR, OASIS & .NET Runtimes.
        public bool OAPPPublishedOnSTARNET { get; set; }
        public bool OAPPPublishedToCloud { get; set; }
        public bool OAPPSelfContainedPublishedToCloud { get; set; }
        public bool OAPPSelfContainedFullPublishedToCloud { get; set; }
        public ProviderType OAPPPublishedProviderType { get; set; }
        public ProviderType OAPPSelfContainedPublishedProviderType { get; set; }
        public ProviderType OAPPSelfContainedFullPublishedProviderType { get; set; }
        public long OAPPFileSize { get; set; }
        public long OAPPSelfContainedFileSize { get; set; }
        public long OAPPSelfContainedFullFileSize { get; set; }
        public string OAPPSourcePublishedPath { get; set; }
        public bool OAPPSourcePublishedOnSTARNET { get; set; }
        public bool OAPPSourcePublicOnSTARNET { get; set; }
        public long OAPPSourceFileSize { get; set; }
        bool IsActive { get; set; }
        public string LaunchTarget { get; set; }
        string Version { get; set; }
        public string STARODKVersion { get; set; }
        public string OASISVersion { get; set; }
        public string COSMICVersion { get; set; }
        public string DotNetVersion { get; set; }
        public int Versions { get; set; }
        public int OAPPDownloads { get; set; } //Is IOAPP better place for these?
        public int OAPPSelfContainedDownloads { get; set; } //Is IOAPP better place for these?
        public int OAPPSelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?
        public int OAPPSourceDownloads { get; set; } //Is IOAPP better place for these?
    }
}