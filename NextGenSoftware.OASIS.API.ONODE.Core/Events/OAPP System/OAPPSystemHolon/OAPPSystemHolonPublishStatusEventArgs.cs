using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPSystemHolonPublishStatusEventArgs : EventArgs
    {
        public IOAPPSystemHolonDNA OAPPSystemHolonDNA { get; set; }
        public OAPPSystemHolonPublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}