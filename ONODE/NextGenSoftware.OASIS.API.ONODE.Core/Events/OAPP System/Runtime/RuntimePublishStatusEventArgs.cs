using System;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class RuntimePublishStatusEventArgs : EventArgs
    {
        public IRuntimeDNA RuntimeDNA { get; set; }
        public string Version { get; set; }
        public RuntimePublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
