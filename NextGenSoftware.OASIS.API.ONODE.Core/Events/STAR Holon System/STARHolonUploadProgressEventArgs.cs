using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARHolon;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events.STARHolon
{
    public class STARHolonUploadProgressEventArgs : EventArgs
    {
        public ISTARHolonDNA STARHolonDNA { get; set; }
        public int Progress { get; set; }
        public STARHolonUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
