using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Events
{
    public class OAPPPublishStatusEventArgs : EventArgs
    {
        //public IOAPPDNA OAPPDNA { get; set; }
        public ISTARNETDNA OAPPDNA { get; set; }
        public OAPPPublishStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
