using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public abstract class DownloadedOAPPSystemHolon : OAPPSystemHolon, IDownloadedOAPPSystemHolon
    {
        public DownloadedOAPPSystemHolon(string OAPPSystemHolonDNAJSONName = "OAPPSystemHolonDNAJSON") : base(OAPPSystemHolonDNAJSONName)
        {
            this.HolonType = HolonType.DownloadedOAPPSystemHolon;
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