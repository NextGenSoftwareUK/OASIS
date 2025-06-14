using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IInstalledSTARHolon : ISTARHolon
    {
        public Guid ParentSTARHolonId { get; set; } //ParentSTARHolonId is used to link the downloaded STAR Holon to its parent STAR Holon in the OASIS and is the same across ALL versions whereas the ParentHolonId points to the specefic holon for that version.
        public Guid DownloadedSTARHolonId { get; set; }
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