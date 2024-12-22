using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPTemplateDownloadProgressEventArgs : EventArgs
    {
        public IOAPPTemplateDNA OAPPTemmplateDNA { get; set; }
        public int Progress { get; set; }
        public OAPPDownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}