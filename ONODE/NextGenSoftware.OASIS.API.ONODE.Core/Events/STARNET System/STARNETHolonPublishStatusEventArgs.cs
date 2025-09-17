using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon
{
    public class STARNETHolonPublishStatusEventArgs : EventArgs
    {
        public ISTARNETDNA STARNETDNA { get; set; }
        public STARNETHolonPublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}