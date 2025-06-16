using System;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon
{
    public class STARNETHolonUploadProgressEventArgs : EventArgs
    {
        public ISTARNETHolonDNA STARHolonDNA { get; set; }
        public int Progress { get; set; }
        public STARNETHolonUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
