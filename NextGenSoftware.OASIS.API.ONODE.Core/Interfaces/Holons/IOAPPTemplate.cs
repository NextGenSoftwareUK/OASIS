using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IOAPPTemplate : IPublishableHolon
    {
       public OAPPTemplateType OAPPTemplateType { get; set; }
       public string OAPPTemplatePath { get; set; }
       public string OAPPTemplatePublishedPath { get; set; }
       public bool OAPPTemplatePublishedOnSTARNET { get; set; }
       public bool OAPPTemplatePublishedToCloud { get; set; }
       public ProviderType OAPPTemplatePublishedProviderType { get; set; }
       public long OAPPTemplateFileSize { get; set; }
       public int Versions { get; set; } 
       public int Downloads { get; set; }
    }
}