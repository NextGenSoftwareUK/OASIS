using System;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.Runtime;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class RuntimeDownloadProgressEventArgs : EventArgs
    {
        public RuntimeType RunTimeType { get; set; }
        public string Version { get; set; }
        public int Progress { get; set; }
        public RuntimeDownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}