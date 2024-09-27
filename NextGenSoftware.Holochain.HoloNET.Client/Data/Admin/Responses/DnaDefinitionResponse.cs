
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class DnaDefinitionResponse : IDnaDefinitionResponse
    {
        [Key("type")]
        public string type { get; set; }

        [Key("data")]
        public DnaDefinitionResponseDetail data { get; set; }
    }
}