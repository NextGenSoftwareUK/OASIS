
using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class AttachAppInterfaceRequest
    {
        [Key("port")]
        public UInt16? port { get; set; }
        //public int port { get; set; }
    }
}