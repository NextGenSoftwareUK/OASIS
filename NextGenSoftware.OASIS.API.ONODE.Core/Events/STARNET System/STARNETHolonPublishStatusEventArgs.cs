using System;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon
{
    public class STARNETHolonPublishStatusEventArgs : EventArgs
    {
        public ISTARNETHolonDNA STARNETHolonDNA { get; set; }
        public STARNETHolonPublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}