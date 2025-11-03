using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    //public class OAPP : OAPPBase, IOAPP
    public class OAPP : STARNETHolon, IOAPP
    {
        public OAPP() : base("OAPPDNAJSON")
        {
            this.HolonType = HolonType.OAPP;
        }

        [CustomOASISProperty]
        public Guid OAPPTemplateId { get; set; }

        [CustomOASISProperty]
        public string OAPPTemplateName { get; set; }

        [CustomOASISProperty]
        public string OAPPTemplateDescription { get; set; }

        [CustomOASISProperty]
        public OAPPTemplateType OAPPTemplateType { get; set; }

        [CustomOASISProperty]
        public int OAPPTemplateVersionSequence { get; set; }

        [CustomOASISProperty]
        public string OAPPTemplateVersion { get; set; }

        [CustomOASISProperty]
        //public Dictionary<string, string> MetaTagMappings { get; set; }

        //[CustomOASISProperty]
        public Guid CelestialBodyMetaDataId { get; set; }

        [CustomOASISProperty]
        public string CelestialBodyMetaDataName { get; set; }

        [CustomOASISProperty]
        public string CelestialBodyMetaDataDescription { get; set; }

        [CustomOASISProperty]
        public CelestialBodyType CelestialBodyMetaDataType { get; set; }

        [CustomOASISProperty]
        public int CelestialBodyMetaDataVersionSequence { get; set; }

        [CustomOASISProperty]
        public string CelestialBodyMetaDataVersion { get; set; }

        [CustomOASISProperty]
        public string CelestialBodyMetaDataGeneratedPath { get; set; }

        [CustomOASISProperty]
        public long OurWorldLat { get; set; }

        [CustomOASISProperty]
        public long OurWorldLong { get; set; }

        [CustomOASISProperty]
        public long OneWorldLat { get; set; }

        [CustomOASISProperty]
        public long OneWorldLong { get; set; }
        //public string OurWorld3dObjectPath { get; set; } //TODO: Not sure we need this?

        [CustomOASISProperty]
        public byte[] OurWorld3dObject { get; set; }

        [CustomOASISProperty]
        public Uri OurWorld3dObjectURI { get; set; }
        //public string OurWorld2dSpritePath { get; set; } //TODO: Not sure we need this?

        [CustomOASISProperty]
        public byte[] OurWorld2dSprite { get; set; }

        [CustomOASISProperty]
        public Uri OurWorld2dSpriteURI { get; set; }
        //public string OneWorld3dObjectPath { get; set; } //TODO: Not sure we need this?

        [CustomOASISProperty]
        public byte[] OneWorld3dObject { get; set; }

        [CustomOASISProperty]
        public Uri OneWorld3dObjectURI { get; set; }
        //public string OneWorld2dSpritePath { get; set; } //TODO: Not sure we need this?

        [CustomOASISProperty]
        public byte[] OneWorld2dSprite { get; set; }

        [CustomOASISProperty]
        public Uri OneWorld2dSpriteURI { get; set; }

        [CustomOASISProperty]
        public string SelfContainedPublishedPath { get; set; } //Contains the STAR & OASIS runtimes.

        [CustomOASISProperty]
        public string SelfContainedFullPublishedPath { get; set; } //Contains the STAR, OASIS & .NET Runtimes.

        [CustomOASISProperty]
        public bool SelfContainedPublishedToCloud { get; set; }

        [CustomOASISProperty]
        public bool SelfContainedFullPublishedToCloud { get; set; }

        [CustomOASISProperty]
        public ProviderType SelfContainedPublishedProviderType { get; set; }

        [CustomOASISProperty]
        public ProviderType SelfContainedFullPublishedProviderType { get; set; }

        [CustomOASISProperty]
        public long SelfContainedFileSize { get; set; }

        [CustomOASISProperty]
        public long SelfContainedFullFileSize { get; set; }

        [CustomOASISProperty]
        public string SourcePublishedPath { get; set; }

        [CustomOASISProperty]
        public bool SourcePublishedOnSTARNET { get; set; }

        [CustomOASISProperty]
        public bool SourcePublicOnSTARNET { get; set; }

        [CustomOASISProperty]
        public long SourceFileSize { get; set; }
        //public bool IsActive { get; set; }

        [CustomOASISProperty]
        public OAPPType OAPPType { get; set; }

        [CustomOASISProperty]
        public GenesisType GenesisType { get; set; }

        [CustomOASISProperty]
        public Guid CelestialBodyId { get; set; }

        [CustomOASISProperty]
        public string CelestialBodyName { get; set; }

        [CustomOASISProperty]
        public HolonType CelestialBodyType { get; set; }

        [CustomOASISProperty]
        public IEnumerable<IZome> Zomes { get; set; }

        [CustomOASISProperty]
        public int SelfContainedDownloads { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int SelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int SourceDownloads { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int TotalSelfContainedDownloads { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int TotalSelfContainedFullDownloads { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int TotalSourceDownloads { get; set; } //Is IOAPP better place for these?


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

        [CustomOASISProperty]
        public int SelfContainedInstalls { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int SelfContainedFullInstalls { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int SourceInstalls { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int TotalSelfContainedInstalls { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int TotalSelfContainedFullInstalls { get; set; } //Is IOAPP better place for these?

        [CustomOASISProperty]
        public int TotalSourceInstalls { get; set; } //Is IOAPP better place for these?

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

        //public OAPPType OAPPType { get; set; }
    }
}