using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class OAPPTemplate : STARHolon, IOAPPTemplate
    {
        public OAPPTemplate() : base("OAPPTemplateDNAJSON")
        {
            this.HolonType = HolonType.OAPPTemplate; 
        }
     }
}