using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOAPPTemplateDNA
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        Guid CreatedByAvatarId { get; set; }
        string CreatedByAvatarUsername { get; set; }
        DateTime CreatedOn { get; set; }

        OAPPTemplateType OAPPTemplateType { get; set; }
        string OAPPTemplatePath { get; set; } //Source  Path
        string OAPPTemplatePublishedPath { get; set; }

        Guid PublishedByAvatarId { get; set; }
        string PublishedByAvatarUsername { get; set; }
        DateTime PublishedOn { get; set; }

        Guid ModifiedByAvatarId { get; set; }
        string ModifiedByAvatarUsername { get; set; }
        DateTime ModifiedOn { get; set; }

        bool OAPPTemplatePublishedOnSTARNET { get; set; }
        bool OAPPTemplatePublishedToCloud { get; set; }
        bool OAPPTemplatePublishedToPinata { get; set; }
        string PinataIPFSHash { get; set; }
        ProviderType OAPPTemplatePublishedProviderType { get; set; }
        string LaunchTarget { get; set; }
        long OAPPTemplateFileSize { get; set; }

        //bool IsActive { get; set; }
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