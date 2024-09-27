
using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class AttachAppInterfaceResponse
    {
        [Key("port")]
        public UInt16 port { get; set; }
        //public object port { get; set; }

        //[Key("p_id")]
        //public string AppName { get; set; }

        //[Key("p_id")]
        //public string AppName { get; set; }
    }
}