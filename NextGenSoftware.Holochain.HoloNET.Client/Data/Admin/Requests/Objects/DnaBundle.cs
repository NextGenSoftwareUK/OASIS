
using MessagePack;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class DnaBundle
    {
        [Key("manifest")]
        public DnaManifest manifest { get; set; }

        [Key("resources")]
        //public ResourceMap resources { get; set; }
        public Dictionary<string, byte[]> resources { get; set; }
    }
}


//export type ResourceMap = { [key: string]: ResourceBytes };
//export type ResourceBytes = Uint8Array;
