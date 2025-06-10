using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IDownloadedSTARHolon : IHolon
    {
        ISTARHolonDNA STARHolonDNA { get; set; }
        public string DownloadedPath { get; set; }
        public DateTime DownloadedOn { get; set; }
        public Guid DownloadedBy { get; set; }
        public string DownloadedByAvatarUsername { get; set; }
    }
}