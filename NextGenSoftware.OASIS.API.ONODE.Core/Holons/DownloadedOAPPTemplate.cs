using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class DownloadedOAPPTemplate : Holon, IDownloadedOAPPTemplate //TODO: Do we want to use Holon? What was the reason again?! ;-) Think so can be used with Data API and HolonManager?
    {
        private IOAPPTemplateDNA _OAPPTemplateDNA;

        public DownloadedOAPPTemplate()
        {
            this.HolonType = HolonType.DownloadedOAPPTemplate;
        }

        //[CustomOASISProperty]
        //public Guid OAPPId { get; set; }

        // [CustomOASISProperty(StoreAsJsonString = true)] //TODO: Get this working later on so we dont need to do the manual code below.
        //public IOAPPDNA OAPPDNA { get; set; }

        public IOAPPTemplateDNA OAPPTemplateDNA
        {
            get
            {
                if (_OAPPTemplateDNA == null && MetaData["OAPPTEMPLATEDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["OAPPTEMPLATEDNAJSON"].ToString()))
                    _OAPPTemplateDNA = JsonSerializer.Deserialize<OAPPTemplateDNA>(MetaData["OAPPTEMPLATEDNAJSON"].ToString());

                return _OAPPTemplateDNA;
            }
            set
            {
                _OAPPTemplateDNA = value;
                MetaData["OAPPTEMPLATEDNAJSON"] = JsonSerializer.Serialize(OAPPTemplateDNA);
            }
        }

        [CustomOASISProperty]
        public string DownloadedPath { get; set; }

        [CustomOASISProperty]
        public DateTime DownloadedOn { get; set; }

        [CustomOASISProperty]
        public Guid DownloadedBy { get; set; }

        [CustomOASISProperty]
        public string DownloadedByAvatarUsername { get; set; }
    }
}