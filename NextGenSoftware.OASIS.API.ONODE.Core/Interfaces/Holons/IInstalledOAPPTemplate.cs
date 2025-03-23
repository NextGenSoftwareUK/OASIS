using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IInstalledOAPPTemplate : IHolon //: IOAPPTemplate
    {
        public IDownloadedOAPPTemplate DownloadedOAPPTemplate { get; set; }
        public IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        public string InstalledPath { get; set; }
        public DateTime InstalledOn { get; set; }
        public Guid InstalledBy { get; set; }
        public string InstalledByAvatarUsername { get; set; }
    }
}