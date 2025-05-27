using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPPSystemHolonDNA : IOAPPSystemHolonDNA
    {
        public Guid Id { get; set; }
        //public Guid VersionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public object OAPPSystemHolonType { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public string CreatedByAvatarUsername { get; set; }
        public DateTime CreatedOn { get; set; }

        //public OAPPTemplateType OAPPTemplateType { get; set; }
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
        public ProviderType PublishedProviderType { get; set; }
        public long FileSize { get; set; }

        //public bool IsActive { get; set; }
        public string Version { get; set; }
        public string STARODKVersion { get; set; }
        public string OASISVersion { get; set; }
        public string COSMICVersion { get; set; }
        public string DotNetVersion { get; set; }

        public int VersionSequence { get; set; }
        public int Downloads { get; set; }
        public int Installs { get; set; }
        public int TotalDownloads { get; set; }
        public int TotalInstalls { get; set; }
        public int NumberOfVersions { get; set; }
    }
}
