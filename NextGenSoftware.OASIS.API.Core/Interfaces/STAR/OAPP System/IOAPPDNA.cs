using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOAPPDNA : ISTARNETDNA
    {
        public Guid OAPPTemplateId { get; set; }
        public string OAPPTemplateName { get; set; }
        public string OAPPTemplateDescription { get; set; }
        public OAPPTemplateType OAPPTemplateType { get; set; }
        public int OAPPTemplateVersionSequence { get; set; }
        public string OAPPTemplateVersion { get; set; }

        public Guid CelestialBodyMetaDataId { get; set; }
        public string CelestialBodyMetaDataName { get; set; }
        public string CelestialBodyMetaDataDescription { get; set; }
        public CelestialBodyType CelestialBodyMetaDataType { get; set; }
        public int CelestialBodyMetaDataVersionSequence { get; set; }
        public string CelestialBodyMetaDataVersion { get; set; }
        public string CelestialBodyMetaDataGeneratedPath { get; set; }

        public long OurWorldLat { get; set; }
        public long OurWorldLong { get; set; }
        public long OneWorldLat { get; set; }
        public long OneWorldLong { get; set; }
        //public string OurWorld3dObjectPath { get; set; } //TODO: Not sure we need this?
        public byte[] OurWorld3dObject { get; set; }
        public Uri OurWorld3dObjectURI { get; set; }
        //public string OurWorld2dSpritePath { get; set; } //TODO: Not sure we need this?
        public byte[] OurWorld2dSprite { get; set; }
        public Uri OurWorld2dSpriteURI { get; set; }
        //public string OneWorld3dObjectPath { get; set; } //TODO: Not sure we need this?
        public byte[] OneWorld3dObject { get; set; }
        public Uri OneWorld3dObjectURI { get; set; }
        //public string OneWorld2dSpritePath { get; set; } //TODO: Not sure we need this?
        public byte[] OneWorld2dSprite { get; set; }
        public Uri OneWorld2dSpriteURI { get; set; }

        public string SelfContainedPublishedPath { get; set; } //Contains the STAR & OASIS runtimes.
        public string SelfContainedFullPublishedPath { get; set; } //Contains the STAR, OASIS & .NET Runtimes.
        public bool SelfContainedPublishedToCloud { get; set; }
        public bool SelfContainedFullPublishedToCloud { get; set; }
        public ProviderType SelfContainedPublishedProviderType { get; set; }
        public ProviderType SelfContainedFullPublishedProviderType { get; set; }
        public long SelfContainedFileSize { get; set; }
        public long SelfContainedFullFileSize { get; set; }
        public string SourcePublishedPath { get; set; }
        public bool SourcePublishedOnSTARNET { get; set; }
        public bool SourcePublicOnSTARNET { get; set; }
        public long SourceFileSize { get; set; }
        //public bool IsActive { get; set; }
        public OAPPType OAPPType { get; set; }

        public GenesisType GenesisType { get; set; }
        public Guid CelestialBodyId { get; set; }
        public string CelestialBodyName { get; set; }
        public HolonType CelestialBodyType { get; set; }
        //public IEnumerable<IZome> Zomes { get; set; }

        public int SelfContainedDownloads { get; set; } //Is IOAPP better place for these?
        public int SelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?
        public int SourceDownloads { get; set; } //Is IOAPP better place for these?
        public int TotalSelfContainedDownloads { get; set; } //Is IOAPP better place for these?
        public int TotalSelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?
        public int TotalSourceDownloads { get; set; } //Is IOAPP better place for these?

        public int DownloadsAll
        {
            get
            {
                return (Downloads + SelfContainedDownloads + SelfContainedFullDownloads + SourceDownloads);
            }
        }

        public int TotalDownloadsAll
        {
            get
            {
                return (TotalDownloads + TotalSelfContainedDownloads + TotalSelfContainedFullDownloads + TotalSourceDownloads);
            }
        }


        public int SelfContainedInstalls { get; set; } //Is IOAPP better place for these?
        public int SelfContainedFullInstalls { get; set; } //Is IOAPP better place for these?
        public int SourceInstalls { get; set; } //Is IOAPP better place for these?
        public int TotalSelfContainedInstalls { get; set; } //Is IOAPP better place for these?
        public int TotalSelfContainedFullInstalls { get; set; } //Is IOAPP better place for these?
        public int TotalSourceInstalls { get; set; } //Is IOAPP better place for these?

        public int InstallsAll
        {
            get
            {
                return (Downloads + SelfContainedDownloads + SelfContainedFullDownloads + SourceDownloads);
            }
        }

        public int TotalInstallsAll
        {
            get
            {
                return (TotalInstalls + TotalSelfContainedInstalls + TotalSelfContainedFullInstalls + TotalSourceInstalls);
            }
        }






        //public Guid OAPPTemplateId { get; set; }
        //public string SelfContainedPublishedPath { get; set; } //Contains the STAR & OASIS runtimes.
        //public string SelfContainedFullPublishedPath { get; set; } //Contains the STAR, OASIS & .NET Runtimes.
        //public bool SelfContainedPublishedToCloud { get; set; }
        //public bool SelfContainedFullPublishedToCloud { get; set; }
        //public ProviderType SelfContainedPublishedProviderType { get; set; }
        //public ProviderType SelfContainedFullPublishedProviderType { get; set; }
        //public long SelfContainedFileSize { get; set; }
        //public long SelfContainedFullFileSize { get; set; }
        //public string SourcePublishedPath { get; set; }
        //public bool SourcePublishedOnSTARNET { get; set; }
        //public bool SourcePublicOnSTARNET { get; set; }
        //public long SourceFileSize { get; set; }
        //public OAPPType OAPPType { get; set; }
        //public OAPPTemplateType OAPPTemplateType { get; set; }
        //public GenesisType GenesisType { get; set; }
        //public Guid CelestialBodyId { get; set; }
        //public string CelestialBodyName { get; set; }
        //public HolonType CelestialBodyType { get; set; }
        //public IEnumerable<IZome> Zomes { get; set; }

        //public int SelfContainedDownloads { get; set; } //Is IOAPP better place for these?
        //public int SelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?
        //public int SourceDownloads { get; set; } //Is IOAPP better place for these?
        //public int TotalSelfContainedDownloads { get; set; } //Is IOAPP better place for these?
        //public int TotalSelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?
        //public int TotalSourceDownloads { get; set; } //Is IOAPP better place for these?

        //public int DownloadsAll
        //{
        //    get
        //    {
        //        return (Downloads + SelfContainedDownloads + SelfContainedFullDownloads + SourceDownloads);
        //    }
        //}

        //public int TotalDownloadsAll
        //{
        //    get
        //    {
        //        return (TotalDownloads + TotalSelfContainedDownloads + TotalSelfContainedFullDownloads + TotalSourceDownloads);
        //    }
        //}

        //public int SelfContainedInstalls { get; set; } //Is IOAPP better place for these?
        //public int SelfContainedFullInstalls { get; set; } //Is IOAPP better place for these?
        //public int SourceInstalls { get; set; } //Is IOAPP better place for these?
        //public int TotalSelfContainedInstalls { get; set; } //Is IOAPP better place for these?
        //public int TotalSelfContainedFullInstalls { get; set; } //Is IOAPP better place for these?
        //public int TotalSourceInstalls { get; set; } //Is IOAPP better place for these?

        //public int InstallsAll
        //{
        //    get
        //    {
        //        return (Downloads + SelfContainedDownloads + SelfContainedFullDownloads + SourceDownloads);
        //    }
        //}

        //public int TotalInstallsAll
        //{
        //    get
        //    {
        //        return (TotalInstalls + TotalSelfContainedInstalls + TotalSelfContainedFullInstalls + TotalSourceInstalls);
        //    }
        //}
    }
}