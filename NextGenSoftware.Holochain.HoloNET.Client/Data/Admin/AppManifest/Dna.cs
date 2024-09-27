
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest
{

    [MessagePackObject]
    public struct Dna
    {
        [Key("bundeled")]
        public Bundeled bundeled { get; set; }
    }
}