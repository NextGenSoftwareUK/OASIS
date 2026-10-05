using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPInstallStatusEventArgs : EventArgs
    {
        public IOAPPDNA OAPPDNA { get; set; }
        public OAPPInstallStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}