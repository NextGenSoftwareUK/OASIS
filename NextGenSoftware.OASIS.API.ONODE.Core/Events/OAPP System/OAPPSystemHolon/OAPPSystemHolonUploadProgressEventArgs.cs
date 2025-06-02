using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPSystemHolonUploadProgressEventArgs : EventArgs
    {
        public IOAPPSystemHolonDNA OAPPSystemHolonDNA { get; set; }
        public int Progress { get; set; }
        public OAPPSystemHolonUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
