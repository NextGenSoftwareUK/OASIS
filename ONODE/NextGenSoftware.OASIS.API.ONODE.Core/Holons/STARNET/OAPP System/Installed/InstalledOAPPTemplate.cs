using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledOAPPTemplate : InstalledSTARNETHolon, IInstalledOAPPTemplate
    {
        public InstalledOAPPTemplate() : base("OAPPTemplateDNAJSON")
        {
            this.HolonType = HolonType.InstalledOAPPTemplate;
        }
    }
}