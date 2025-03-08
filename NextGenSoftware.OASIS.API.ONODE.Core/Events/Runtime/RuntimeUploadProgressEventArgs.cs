using System;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.Runtime;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class RuntimeUploadProgressEventArgs : EventArgs
    {
        public RuntimeType RunTimeType { get; set; }
        public string Version { get; set; }
        public int Progress { get; set; }
        public RuntimeUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
