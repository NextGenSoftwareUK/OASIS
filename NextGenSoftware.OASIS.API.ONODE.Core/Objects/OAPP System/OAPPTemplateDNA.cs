using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class OAPPTemplateDNA : OAPPSystemHolonDNA, IOAPPTemplateDNA
    {
        public OAPPTemplateType OAPPTemplateType { get; set; }
    }
}