using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class DownloadedOAPPTemplate : DownloadedOAPPSystemHolon, IDownloadedOAPPTemplate //TODO: Do we want to use Holon? What was the reason again?! ;-) Think so can be used with Data API and HolonManager?
    {
        //private IOAPPTemplateDNA _OAPPTemplateDNA;

        public DownloadedOAPPTemplate()
        {
            this.HolonType = HolonType.DownloadedOAPPTemplate;
        }

        public IOAPPTemplateDNA OAPPTemplateDNA
        {
            get
            {
                return (IOAPPTemplateDNA)base.OAPPSystemHolonDNA;
            }
            set
            {
                base.OAPPSystemHolonDNA = value;
            }
        }

        //public IOAPPTemplateDNA OAPPTemplateDNA
        //{
        //    get
        //    {
        //        if (_OAPPTemplateDNA == null && MetaData["OAPPTemplateDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["OAPPTemplateDNAJSON"].ToString()))
        //            _OAPPTemplateDNA = JsonSerializer.Deserialize<OAPPTemplateDNA>(MetaData["OAPPTemplateDNAJSON"].ToString());

        //        return _OAPPTemplateDNA;
        //    }
        //    set
        //    {
        //        _OAPPTemplateDNA = value;
        //        MetaData["OAPPTemplateDNAJSON"] = JsonSerializer.Serialize(OAPPTemplateDNA);
        //    }
        //}

        //[CustomOASISProperty]
        //public string DownloadedPath { get; set; }

        //[CustomOASISProperty]
        //public DateTime DownloadedOn { get; set; }

        //[CustomOASISProperty]
        //public Guid DownloadedBy { get; set; }

        //[CustomOASISProperty]
        //public string DownloadedByAvatarUsername { get; set; }
    }
}