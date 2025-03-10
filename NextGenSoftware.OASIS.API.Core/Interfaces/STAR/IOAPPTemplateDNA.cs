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
        string OAPPTemplatePath { get; set; }
        string OAPPTemplatePublishedPath { get; set; }

        Guid PublishedByAvatarId { get; set; }
        string PublishedByAvatarUsername { get; set; }
        DateTime PublishedOn { get; set; }
        bool OAPPTemplatePublishedOnSTARNET { get; set; }
        bool OAPPTemplatePublishedToCloud { get; set; }
        ProviderType OAPPTemplatePublishedProviderType { get; set; }
        long OAPPTemplateFileSize { get; set; }

        bool IsActive { get; set; }
        string Version { get; set; }
        string STARODKVersion { get; set; }
        string OASISVersion { get; set; }
        string COSMICVersion { get; set; }
        string DotNetVersion { get; set; }

        int Versions { get; set; }
        int Downloads { get; set; }
    }
}