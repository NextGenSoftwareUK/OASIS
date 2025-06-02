using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface IOAPPDNA
    {
        Guid CelestialBodyId { get; set; }
        string CelestialBodyName { get; set; }
        HolonType CelestialBodyType { get; set; }
        int DownloadsAll { get; }
        GenesisType GenesisType { get; set; }
        int InstallsAll { get; }
        bool IsActive { get; set; }
        OAPPTemplateType OAPPTemplateType { get; set; }
        OAPPType OAPPType { get; set; }
        int SelfContainedDownloads { get; set; }
        long SelfContainedFileSize { get; set; }
        int SelfContainedFullDownloads { get; set; }
        long SelfContainedFullFileSize { get; set; }
        int SelfContainedFullInstalls { get; set; }
        string SelfContainedFullPublishedPath { get; set; }
        ProviderType SelfContainedFullPublishedProviderType { get; set; }
        bool SelfContainedFullPublishedToCloud { get; set; }
        int SelfContainedInstalls { get; set; }
        string SelfContainedPublishedPath { get; set; }
        ProviderType SelfContainedPublishedProviderType { get; set; }
        bool SelfContainedPublishedToCloud { get; set; }
        int SourceDownloads { get; set; }
        long SourceFileSize { get; set; }
        int SourceInstalls { get; set; }
        bool SourcePublicOnSTARNET { get; set; }
        bool SourcePublishedOnSTARNET { get; set; }
        string SourcePublishedPath { get; set; }
        int TotalDownloadsAll { get; }
        int TotalInstallsAll { get; }
        int TotalSelfContainedDownloads { get; set; }
        int TotalSelfContainedFullDownloads { get; set; }
        int TotalSelfContainedFullInstalls { get; set; }
        int TotalSelfContainedInstalls { get; set; }
        int TotalSourceDownloads { get; set; }
        int TotalSourceInstalls { get; set; }
        IEnumerable<IZome> Zomes { get; set; }
    }
}