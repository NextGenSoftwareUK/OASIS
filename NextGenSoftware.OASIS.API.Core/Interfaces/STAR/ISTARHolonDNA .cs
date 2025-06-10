using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISTARHolonDNA
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        Guid CreatedByAvatarId { get; set; }
        string CreatedByAvatarUsername { get; set; }
        DateTime CreatedOn { get; set; }
        object STARHolonType { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        string SourcePath { get; set; }
        string PublishedPath { get; set; }
        Guid PublishedByAvatarId { get; set; }
        string PublishedByAvatarUsername { get; set; }
        DateTime PublishedOn { get; set; }
        Guid ModifiedByAvatarId { get; set; }
        string ModifiedByAvatarUsername { get; set; }
        DateTime ModifiedOn { get; set; }
        bool PublishedOnSTARNET { get; set; }
        bool PublishedToCloud { get; set; }
        bool PublishedToPinata { get; set; }
        string PinataIPFSHash { get; set; }
        ProviderType PublishedProviderType { get; set; }
        string LaunchTarget { get; set; }
        long FileSize { get; set; }
        string Version { get; set; }
        string STARODKVersion { get; set; }
        string OASISVersion { get; set; }
        string COSMICVersion { get; set; }
        string DotNetVersion { get; set; }
        public int VersionSequence { get; set; }
        public int Downloads { get; set; }
        public int Installs { get; set; }
        public int TotalDownloads { get; set; }
        public int TotalInstalls { get; set; }
        public int NumberOfVersions { get; set; }
    }
}