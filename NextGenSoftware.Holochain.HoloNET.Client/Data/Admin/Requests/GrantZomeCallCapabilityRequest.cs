
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class GrantZomeCallCapabilityRequest
    {
        [Key("cell_id")]
        public byte[][] cell_id { get; set; }

        [Key("cap_grant")]
        public ZomeCallCapGrant cap_grant { get; set; }
    }
}