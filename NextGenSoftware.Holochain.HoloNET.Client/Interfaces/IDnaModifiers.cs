using System;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IDnaModifiers
    {
        string network_seed { get; set; }
        long origin_time { get; set; }
        DateTime OriginTime { get; set; }
        object properties { get; set; }
        RustDuration quantum_time { get; set; }
    }
}