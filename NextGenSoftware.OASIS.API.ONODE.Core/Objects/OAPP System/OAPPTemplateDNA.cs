using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class OAPPTemplateDNA : STARHolonDNA, IOAPPTemplateDNA
    {
        public OAPPTemplateType OAPPTemplateType { get; set; }
    }
}