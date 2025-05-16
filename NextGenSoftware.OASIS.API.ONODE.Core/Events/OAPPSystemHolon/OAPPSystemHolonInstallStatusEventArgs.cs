using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPSystemHolonInstallStatusEventArgs : EventArgs
    {
        public IOAPPSystemHolonDNA OAPPSystemHolonDNA { get; set; }
        public OAPPSystemHolonInstallStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
