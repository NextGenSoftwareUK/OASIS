
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest
{
    [MessagePackObject]
    public struct Roles
    {
        [Key("name")]
        public string name { get; set; }

        [Key("provisioning")]
        public Provisioning provisioning { get; set; }

        [Key("dna")]
        public Dna dna { get; set; }
    }
}