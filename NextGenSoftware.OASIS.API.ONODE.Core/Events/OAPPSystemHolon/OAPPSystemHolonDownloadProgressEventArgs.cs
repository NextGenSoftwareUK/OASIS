using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPSystemHolonDownloadProgressEventArgs : EventArgs
    {
        public IOAPPSystemHolonDNA OAPPSystemHolonDNA { get; set; }
        public int Progress { get; set; }
        public OAPPSystemHolonDownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}