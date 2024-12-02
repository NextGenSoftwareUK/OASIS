using System;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPDownloadProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
        public OAPPDownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}