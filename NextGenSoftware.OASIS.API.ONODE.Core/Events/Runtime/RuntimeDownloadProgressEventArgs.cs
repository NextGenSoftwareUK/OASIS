using System;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class RuntimeDownloadProgressEventArgs : EventArgs
    {
        public IRuntimeDNA RuntimeDNA { get; set; }
        public int Progress { get; set; }
        public RuntimeDownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}