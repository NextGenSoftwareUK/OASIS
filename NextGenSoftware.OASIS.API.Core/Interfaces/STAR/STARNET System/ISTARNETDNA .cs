using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISTARNETDNA
    {
        Guid Id { get; set; } 
        //public bool IsPublic { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        Guid CreatedByAvatarId { get; set; }
        string CreatedByAvatarUsername { get; set; }
        DateTime CreatedOn { get; set; }
        string STARNETHolonType { get; set; }
        object STARNETCategory { get; set; }
        public STARNETDependencies Dependencies { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public Dictionary<string, string> MetaTagMappings { get; set; }
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
        string PublishedProviderType { get; set; }
        string LaunchTarget { get; set; }
        long FileSize { get; set; }
        string Version { get; set; }
        public string OASISRuntimeVersion { get; set; }
        public string OASISAPIVersion { get; set; }
        public string COSMICVersion { get; set; }
        public string STARRuntimeVersion { get; set; }
        public string STARODKVersion { get; set; }
        public string STARAPIVersion { get; set; }
        public string STARNETVersion { get; set; }
        public string DotNetVersion { get; set; }
        public int VersionSequence { get; set; }
        public int Downloads { get; set; }
        public int Installs { get; set; }
        public int TotalDownloads { get; set; }
        public int TotalInstalls { get; set; }
        public int NumberOfVersions { get; set; }
    }
}