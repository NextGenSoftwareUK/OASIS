using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOAPPTemplateDNA : IOAPPSystemHolonDNA
    {
        OAPPTemplateType OAPPTemplateType { get; set; }
    }
}