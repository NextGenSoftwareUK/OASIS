using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedOAPPTemplate : DownloadedSTARNETHolon, IDownloadedOAPPTemplate
    {
        public DownloadedOAPPTemplate() : base("OAPPTemplateDNAJSON")
        {
            this.HolonType = HolonType.DownloadedOAPPTemplate;
        }
    }
}