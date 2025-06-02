//using System;
//using NextGenSoftware.OASIS.API.Core.Enums;

//namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
//{
//    public interface IOAPPDNA
//    {
//        Guid CelestialBodyId { get; set; }
//        string CelestialBodyName { get; set; }
//        HolonType CelestialBodyType { get; set; }
//        GenesisType GenesisType { get; set; }
//        OAPPType OAPPType { get; set; }
//        OAPPTemplateType OAPPTemplateType { get; set; }
//        Guid OAPPTemplateId { get; set; }
//        public string OAPPSelfContainedPublishedPath { get; set; } //Contains the STAR & OASIS runtimes.
//        public string OAPPSelfContainedFullPublishedPath { get; set; } //Contains the STAR, OASIS & .NET Runtimes.
//        public bool OAPPSelfContainedPublishedToCloud { get; set; }
//        public bool OAPPSelfContainedFullPublishedToCloud { get; set; }
//        public ProviderType OAPPSelfContainedPublishedProviderType { get; set; }
//        public ProviderType OAPPSelfContainedFullPublishedProviderType { get; set; }
//        public long OAPPSelfContainedFileSize { get; set; }
//        public long OAPPSelfContainedFullFileSize { get; set; }
//        public string OAPPSourcePublishedPath { get; set; }
//        public bool OAPPSourcePublishedOnSTARNET { get; set; }
//        public bool OAPPSourcePublicOnSTARNET { get; set; }
//        public long OAPPSourceFileSize { get; set; }
//        public int OAPPSelfContainedDownloads { get; set; } //Is IOAPP better place for these?
//        public int OAPPSelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?
//        public int OAPPSourceDownloads { get; set; } //Is IOAPP better place for these?
//    }
//}