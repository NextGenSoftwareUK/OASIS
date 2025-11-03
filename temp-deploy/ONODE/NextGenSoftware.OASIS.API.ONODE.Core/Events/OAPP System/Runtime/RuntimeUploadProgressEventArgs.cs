using System;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class RuntimeUploadProgressEventArgs : EventArgs
    {
        public IRuntimeDNA RuntimeDNA { get; set; }
        public int Progress { get; set; }
        public RuntimeUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
