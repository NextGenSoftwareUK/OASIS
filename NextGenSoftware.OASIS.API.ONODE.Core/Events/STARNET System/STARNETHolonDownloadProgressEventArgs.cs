using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon
{
    public class STARNETHolonDownloadProgressEventArgs : EventArgs
    {
        public ISTARNETHolonDNA STARHolonDNA { get; set; }
        public int Progress { get; set; }
        public STARNETHolonDownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}