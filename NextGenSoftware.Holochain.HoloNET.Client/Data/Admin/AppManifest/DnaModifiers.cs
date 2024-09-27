
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest
{
    [MessagePackObject]
    public class DnaModifiers : IDnaModifiers
    {
        [Key("network_seed")]
        public string network_seed { get; set; }

        [Key("properties")]
        public object properties { get; set; } //Could be object or byte[]?

        [Key("origin_time")]
        public long origin_time { get; set; } //RegisterDnaRequest doesn't need this.

        [IgnoreMember]
        public DateTime OriginTime { get; set; } //RegisterDnaRequest doesn't need this.

        [Key("quantum_time")]
        public RustDuration quantum_time { get; set; } //RegisterDnaRequest doesn't need this.
        //public object quantum_time { get; set; } //RegisterDnaRequest doesn't need this.
    }
}