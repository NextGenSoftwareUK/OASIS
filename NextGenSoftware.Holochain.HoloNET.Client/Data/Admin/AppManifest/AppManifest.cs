
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest
{
    //public enum AppManifestType
    //{
    //    V1, //PausedAppReasonType
    //    Disabled,//DisabledAppReasonType
    //    Running
    //}

    [MessagePackObject]
    public class AppManifest : IAppManifest
    {
        [Key("manifest_version")]
        public string manifest_version { get; set; }

        [Key("name")]
        public string name { get; set; }

        [Key("description")]
        public string description { get; set; }

        [Key("roles")]
        public Roles[] roles { get; set; }
    }
}