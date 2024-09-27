
using MessagePack;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class ZomeDefinition
    {
        [Key("ZomeName")]
        public string ZomeName { get; set; }
        
        [Key("wasm_hash")]
        public byte[] wasm_hash { get; set; }

        [Key("dependencies")]
        //public string[] dependencies { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
    }
}