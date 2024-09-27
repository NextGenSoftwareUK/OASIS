
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class UpdateCoordinatorsRequest
    {
        [Key("dnaHash")]
        public byte[] dnaHash { get; set; }

        [Key("path")]
        public string path { get; set; } //Can be either path OR bundle but not both.

        [Key("bundle")]
        public CoordinatorBundle bundle { get; set; } //Can be either path OR bundle but not both.
    }
}

//flattened CoordinatorSource (path and bundle) into HoloNETAdminUpdateCoordinatorsRequest