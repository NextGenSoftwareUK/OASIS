using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledSTARNETHolon : STARNETHolon, IInstalledSTARNETHolon
    {
        public InstalledSTARNETHolon()
        {
            this.HolonType = HolonType.InstalledSTARNETHolon;
        }

        public InstalledSTARNETHolon(string STARNETHolonDNAJSONName = "STARNETHolonDNAJSON") : base(STARNETHolonDNAJSONName)
        {
            this.HolonType = HolonType.InstalledSTARNETHolon;
        }

        [CustomOASISProperty]
        public Guid ParentSTARNETHolonId { get; set; } //ParentSTARHolonId is used to link the downloaded STAR Holon to its parent STAR Holon in the OASIS and is the same across ALL versions whereas the ParentHolonId points to the specefic holon for that version.

        [CustomOASISProperty]
        public Guid DownloadedSTARNETHolonId { get; set; }

        [CustomOASISProperty]
        public string DownloadedPath { get; set; }

        [CustomOASISProperty]
        public DateTime DownloadedOn { get; set; }

        [CustomOASISProperty]
        public Guid DownloadedBy { get; set; }

        [CustomOASISProperty]
        public string DownloadedByAvatarUsername { get; set; }

        [CustomOASISProperty]
        public string InstalledPath { get; set; }

        [CustomOASISProperty]
        public DateTime InstalledOn { get; set; }

        [CustomOASISProperty]
        public Guid InstalledBy { get; set; }

        [CustomOASISProperty]
        public string InstalledByAvatarUsername { get; set; }

        [CustomOASISProperty]
        public DateTime UninstalledOn { get; set; }

        [CustomOASISProperty]
        public Guid UninstalledBy { get; set; }

        [CustomOASISProperty]
        public string UninstalledByAvatarUsername { get; set; }

        [CustomOASISProperty]
        public string Active { get; set; }
    }
}