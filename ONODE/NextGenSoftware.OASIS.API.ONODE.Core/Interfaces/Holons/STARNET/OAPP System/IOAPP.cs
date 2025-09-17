//using System;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

//namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
//{
//    public interface IOAPP : ISTARNETHolon
//    {
//        Guid CelestialBodyId { get; set; }
//        string CelestialBodyMetaDataDescription { get; set; }
//        string CelestialBodyMetaDataGeneratedPath { get; set; }
//        Guid CelestialBodyMetaDataId { get; set; }
//        string CelestialBodyMetaDataName { get; set; }
//        CelestialBodyType CelestialBodyMetaDataType { get; set; }
//        string CelestialBodyMetaDataVersion { get; set; }
//        int CelestialBodyMetaDataVersionSequence { get; set; }
//        string CelestialBodyName { get; set; }
//        HolonType CelestialBodyType { get; set; }
//        GenesisType GenesisType { get; set; }
//        string OAPPTemplateDescription { get; set; }
//        Guid OAPPTemplateId { get; set; }
//        string OAPPTemplateName { get; set; }
//        OAPPTemplateType OAPPTemplateType { get; set; }
//        string OAPPTemplateVersion { get; set; }
//        int OAPPTemplateVersionSequence { get; set; }
//        OAPPType OAPPType { get; set; }
//        byte[] OneWorld2dSprite { get; set; }
//        Uri OneWorld2dSpriteURI { get; set; }
//        byte[] OneWorld3dObject { get; set; }
//        Uri OneWorld3dObjectURI { get; set; }
//        long OneWorldLat { get; set; }
//        long OneWorldLong { get; set; }
//        byte[] OurWorld2dSprite { get; set; }
//        Uri OurWorld2dSpriteURI { get; set; }
//        byte[] OurWorld3dObject { get; set; }
//        Uri OurWorld3dObjectURI { get; set; }
//        long OurWorldLat { get; set; }
//        long OurWorldLong { get; set; }
//        int SelfContainedDownloads { get; set; }
//        long SelfContainedFileSize { get; set; }
//        int SelfContainedFullDownloads { get; set; }
//        long SelfContainedFullFileSize { get; set; }
//        int SelfContainedFullInstalls { get; set; }
//        string SelfContainedFullPublishedPath { get; set; }
//        ProviderType SelfContainedFullPublishedProviderType { get; set; }
//        bool SelfContainedFullPublishedToCloud { get; set; }
//        int SelfContainedInstalls { get; set; }
//        string SelfContainedPublishedPath { get; set; }
//        ProviderType SelfContainedPublishedProviderType { get; set; }
//        bool SelfContainedPublishedToCloud { get; set; }
//        int SourceDownloads { get; set; }
//        long SourceFileSize { get; set; }
//        int SourceInstalls { get; set; }
//        bool SourcePublicOnSTARNET { get; set; }
//        bool SourcePublishedOnSTARNET { get; set; }
//        string SourcePublishedPath { get; set; }
//        int TotalSelfContainedDownloads { get; set; }
//        int TotalSelfContainedFullDownloads { get; set; }
//        int TotalSelfContainedFullInstalls { get; set; }
//        int TotalSelfContainedInstalls { get; set; }
//        int TotalSourceDownloads { get; set; }
//        int TotalSourceInstalls { get; set; }
//        IEnumerable<IZome> Zomes { get; set; }
//    }
//}