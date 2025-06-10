using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IOAPPTemplateDNA : ISTARHolonDNA
    {
        OAPPTemplateType OAPPTemplateType { get; set; }
    }
}