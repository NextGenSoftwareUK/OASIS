using System;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPUploadProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
        public OAPPUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
