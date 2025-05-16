using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class InstalledOAPPSystemHolon : OAPPTemplate, IInstalledOAPPSystemHolon
    {
        public InstalledOAPPSystemHolon()
        {
            this.HolonType = HolonType.InstalledOAPPSystemHolon;
        }

        [CustomOASISProperty]
        public Guid DownloadedOAPPSystemHolonId { get; set; }

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

        //[CustomOASISProperty]
        //public string OAPPTemplateVersion { get; set; }
    }
}