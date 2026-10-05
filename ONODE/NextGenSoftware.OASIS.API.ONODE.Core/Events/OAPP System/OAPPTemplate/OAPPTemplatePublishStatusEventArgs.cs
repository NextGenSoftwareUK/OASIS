using System;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPTemplatePublishStatusEventArgs : EventArgs
    {
        public IOAPPTemplateDNA OAPPTemplateDNA { get; set; }
        public OAPPTemplatePublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
