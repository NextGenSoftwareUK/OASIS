using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IDownloadedOAPPTemplate : IDownloadedOAPPSystemHolon //: IOAPPTemplate
    {
        IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        //public string DownloadedPath { get; set; }
        //public DateTime DownloadedOn { get; set; }
        //public Guid DownloadedBy { get; set; }
        //public string DownloadedByAvatarUsername { get; set; }
    }
}