using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IInstalledOAPPTemplate : IOAPPTemplate //IHolon
    {
        //public IDownloadedOAPPTemplate DownloadedOAPPTemplate { get; set; }
        //public IOAPPTemplateDNA OAPPTemplateDNA { get; set; }

        public Guid DownloadedOAPPTemplateId { get; set; }
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
        //public string OAPPTemplateVersion { get; set; }
    }
}