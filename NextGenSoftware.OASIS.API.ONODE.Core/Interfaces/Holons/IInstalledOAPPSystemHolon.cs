using System;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IInstalledOAPPSystemHolon : IOAPPSystemHolon
    {
        public Guid DownloadedOAPPSystemHolonId { get; set; }
        public string DownloadedPath { get; set; }
        public DateTime DownloadedOn { get; set; }
        public Guid DownloadedBy { get; set; }
        public string DownloadedByAvatarUsername { get; set; }
        public string InstalledPath { get; set; }
        public DateTime InstalledOn { get; set; }
        public Guid InstalledBy { get; set; }
        public string InstalledByAvatarUsername { get; set; }
        public DateTime UninstalledOn { get; set; }
        public Guid UninstalledBy { get; set; }
        public string UninstalledByAvatarUsername { get; set; }
        public string Active { get; set; }
    }
}