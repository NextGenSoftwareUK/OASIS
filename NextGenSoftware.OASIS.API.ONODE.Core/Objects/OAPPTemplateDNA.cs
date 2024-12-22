using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPPTemplateDNA : IOAPPTemplateDNA
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public string CreatedByAvatarUsername { get; set; }
        public DateTime CreatedOn { get; set; }

        public OAPPTemplateType OAPPTemplateType { get; set; }
        public string OAPPTemplatePath { get; set; }
        public string OAPPTemplatePublishedPath { get; set; }

        public Guid PublishedByAvatarId { get; set; }
        public string PublishedByAvatarUsername { get; set; }
        public DateTime PublishedOn { get; set; }
        public bool OAPPTemplatePublishedOnSTARNET { get; set; }
        public bool OAPPTemplatePublishedToCloud { get; set; }
        public ProviderType OAPPTemplatePublishedProviderType { get; set; }
        public long OAPPTemplateFileSize { get; set; }

        public bool IsActive { get; set; }
        public string Version { get; set; }
        public string STARODKVersion { get; set; }
        public string OASISVersion { get; set; }
        public string COSMICVersion { get; set; }
        public string DotNetVersion { get; set; }

        public int Versions { get; set; }
        public int Downloads { get; set; }
    }
}
