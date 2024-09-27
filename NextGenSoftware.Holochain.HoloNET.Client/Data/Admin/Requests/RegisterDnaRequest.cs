
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class RegisterDnaRequest
    {
        [Key("modifiers")]
        public DnaModifiers modifiers { get; set; }

        //[Key("DnaSource")]
        //public string DnaSource { get; set; }

        [Key("path")]
        public string path { get; set; } //Can ONLY be path, bundle or hash.

        [Key("bundle")]
        public DnaBundle bundle { get; set; } //Can ONLY be path, bundle or hash.

        [Key("hash")]
        public byte[] hash { get; set; } //Can ONLY be path, bundle or hash.
    }
}