
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class InstallAppRequest
    {
        [Key("agent_key")]
        public byte[] agent_key { get; set; }

        [Key("installed_app_id")]
        public string installed_app_id { get; set; }

        [Key("membrane_proofs")]
        public Dictionary<string, byte[]> membrane_proofs { get; set; }

        [Key("network_seed")]
        public string network_seed { get; set; }

        //[Key("source")]
        //public string source { get; set; }

        [Key("path")]
        public string path { get; set; } //Can ONLY be path OR bundle

        [Key("bundle")]
        public AppBundle bundle { get; set; } //Can ONLY be path OR bundle

        //https://docs.rs/holochain_types/0.2.1/holochain_types/app/enum.AppBundleSource.html
        //pub enum AppBundleSource
        //{
        //    Bundle(AppBundle),
        //    Path(PathBuf),
        //}
    }
}