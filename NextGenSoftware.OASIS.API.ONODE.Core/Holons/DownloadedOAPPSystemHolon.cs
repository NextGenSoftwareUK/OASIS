using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class DownloadedOAPPSystemHolon : Holon, IDownloadedOAPPSystemHolon //TODO: Do we want to use Holon? What was the reason again?! ;-) Think so can be used with Data API and HolonManager?
    {
        private IOAPPSystemHolonDNA _IOAPPSystemHolonDNA;

        public DownloadedOAPPSystemHolon()
        {
            this.HolonType = HolonType.DownloadedOAPPSystemHolon;
        }

        //[CustomOASISProperty]
        //public Guid OAPPId { get; set; }

        // [CustomOASISProperty(StoreAsJsonString = true)] //TODO: Get this working later on so we dont need to do the manual code below.
        //public IOAPPDNA OAPPDNA { get; set; }

        public IOAPPSystemHolonDNA OAPPSystemHolonDNA
        {
            get
            {
                if (_IOAPPSystemHolonDNA == null && MetaData["OAPPSystemHolonDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["OAPPSystemHolonDNAJSON"].ToString()))
                    _IOAPPSystemHolonDNA = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(MetaData["OAPPSystemHolonDNAJSON"].ToString());

                return _IOAPPSystemHolonDNA;
            }
            set
            {
                _IOAPPSystemHolonDNA = value;
                MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(OAPPSystemHolonDNA);
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