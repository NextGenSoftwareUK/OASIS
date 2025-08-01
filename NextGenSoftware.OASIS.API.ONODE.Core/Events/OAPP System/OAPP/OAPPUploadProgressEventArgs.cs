using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPUploadProgressEventArgs : EventArgs
    {
        public IOAPPDNA OAPPDNA { get; set; }
        public int Progress { get; set; }
        public OAPPUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
