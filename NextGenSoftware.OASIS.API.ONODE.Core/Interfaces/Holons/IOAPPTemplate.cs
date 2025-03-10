using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IOAPPTemplate : IHolon // : IPublishableHolon
    {
        IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        byte[] PublishedOAPPTemplate { get; set; }

       // OAPPTemplateType OAPPTemplateType { get; set; }
       //public string OAPPTemplatePath { get; set; }
       //public string OAPPTemplatePublishedPath { get; set; }
       //public bool OAPPTemplatePublishedOnSTARNET { get; set; }
       //public bool OAPPTemplatePublishedToCloud { get; set; }
       //public ProviderType OAPPTemplatePublishedProviderType { get; set; }
       //public long OAPPTemplateFileSize { get; set; }
       //public int Versions { get; set; } 
       //public int Downloads { get; set; }
    }
}