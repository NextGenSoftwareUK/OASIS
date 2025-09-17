using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedSTARNETHolon : STARNETHolon, IDownloadedSTARNETHolon
    {
        public DownloadedSTARNETHolon()
        {
            this.HolonType = HolonType.DownloadedSTARNETHolon;
        }

        public DownloadedSTARNETHolon(string STARHolonDNAJSONName = "STARNETHolonDNAJSON") : base(STARHolonDNAJSONName)
        {
            this.HolonType = HolonType.DownloadedSTARNETHolon;
        }

        [CustomOASISProperty]
        public Guid ParentSTARNETHolonId { get; set; } //ParentSTARHolonId is used to link the downloaded STAR Holon to its parent STAR Holon in the OASIS and is the same across ALL versions whereas the ParentHolonId points to the specefic holon for that version.

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