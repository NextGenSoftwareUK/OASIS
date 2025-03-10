using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPTemplatePublishStatusEventArgs : EventArgs
    {
        public IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        public OAPPTemplatePublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
