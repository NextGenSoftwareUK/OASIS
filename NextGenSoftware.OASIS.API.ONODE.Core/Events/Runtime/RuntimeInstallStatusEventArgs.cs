using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.Runtime;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class RuntimeInstallStatusEventArgs : EventArgs
    {
        public RuntimeDNA RuntimeDNA { get; set; }
        public RuntimeType RunTimeType { get; set; }
        public string Version { get; set; }
        public RuntimeInstallStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
