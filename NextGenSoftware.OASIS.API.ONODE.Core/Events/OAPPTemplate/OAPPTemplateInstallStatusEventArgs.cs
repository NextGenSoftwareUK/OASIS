using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPTemplateInstallStatusEventArgs : EventArgs
    {
        public IOAPPTemplateDNA OAPPTemmplateDNA { get; set; }
        public OAPPInstallStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
