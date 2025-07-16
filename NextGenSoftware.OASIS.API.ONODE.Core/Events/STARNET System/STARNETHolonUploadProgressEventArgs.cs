using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon
{
    public class STARNETHolonUploadProgressEventArgs : EventArgs
    {
        public ISTARNETDNA STARNETDNA { get; set; }
        public int Progress { get; set; }
        public STARNETHolonUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
