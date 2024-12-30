using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPTemplateUploadProgressEventArgs : EventArgs
    {
        public IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        public int Progress { get; set; }
        public OAPPTemplateUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
