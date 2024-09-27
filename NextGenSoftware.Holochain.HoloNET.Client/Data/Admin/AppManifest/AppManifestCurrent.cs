using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest
{
    [MessagePackObject]
    public struct AppManifestCurrent //Or maybe AppManifestV1?
    {
        [Key("name")]
        public string name { get; set; }

        [Key("description")]
        public string description { get; set; }

        [Key("roles")]
        public string roles { get; set; }
    }
}