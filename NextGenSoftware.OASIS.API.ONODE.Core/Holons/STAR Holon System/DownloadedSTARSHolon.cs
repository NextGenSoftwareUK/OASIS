using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedSTARHolon : STARHolon, IDownloadedSTARHolon
    {
        public DownloadedSTARHolon()
        {
            this.HolonType = HolonType.DownloadedSTARHolon;
        }

        public DownloadedSTARHolon(string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(STARHolonDNAJSONName)
        {
            this.HolonType = HolonType.DownloadedSTARHolon;
        }

        [CustomOASISProperty]
        public Guid ParentSTARHolonId { get; set; } //ParentSTARHolonId is used to link the downloaded STAR Holon to its parent STAR Holon in the OASIS and is the same across ALL versions whereas the ParentHolonId points to the specefic holon for that version.

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