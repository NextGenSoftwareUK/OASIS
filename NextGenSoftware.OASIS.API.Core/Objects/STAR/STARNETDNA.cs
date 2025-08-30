using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class STARNETDNA : ISTARNETDNA
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string STARNETHolonType { get; set; }
        public object STARNETCategory { get; set; }
        public STARNETDependencies Dependencies { get; set; } = new STARNETDependencies();
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> MetaTagMappings { get; set; } = new Dictionary<string, string>();
        public Guid CreatedByAvatarId { get; set; }
        public string CreatedByAvatarUsername { get; set; }
        public DateTime CreatedOn { get; set; }
        public string SourcePath { get; set; }
        public string PublishedPath { get; set; }
        public Guid PublishedByAvatarId { get; set; }
        public string PublishedByAvatarUsername { get; set; }
        public DateTime PublishedOn { get; set; }
        public string LaunchTarget { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public string ModifiedByAvatarUsername { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool PublishedOnSTARNET { get; set; }
        public bool PublishedToCloud { get; set; }
        public bool PublishedToPinata { get; set; }
        public string PinataIPFSHash { get; set; }
        public string PublishedProviderType { get; set; }
        public long FileSize { get; set; }
        public string Version { get; set; }
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