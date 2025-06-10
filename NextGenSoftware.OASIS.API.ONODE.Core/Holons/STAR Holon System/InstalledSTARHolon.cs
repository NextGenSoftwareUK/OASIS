using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledSTARHolon : STARHolon, IInstalledSTARHolon
    {
        public InstalledSTARHolon()
        {
            this.HolonType = HolonType.InstalledSTARHolon;
        }

        public InstalledSTARHolon(string STARHolonDNAJSONName = "STARHolonDNAJSON") : base(STARHolonDNAJSONName)
        {
            this.HolonType = HolonType.InstalledSTARHolon;
        }

        [CustomOASISProperty]
        public Guid DownloadedSTARHolonId { get; set; }

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