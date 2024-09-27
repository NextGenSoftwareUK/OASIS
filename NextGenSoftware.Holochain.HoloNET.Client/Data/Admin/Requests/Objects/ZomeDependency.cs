
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class ZomeDependency
    {
        [Key("name")]
        public string name { get; set; } //ZomeName
    }
}