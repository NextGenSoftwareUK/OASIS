
using MessagePack;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class ZomeCallCapGrant
    {
        [Key("tag")]
        public string tag { get; set; }

        //[Key("cap_grant")]
        //public dynamic cap_grant { get; set; }

        [Key("access")]
        //public CapGrantAccess access { get; set; }
        public dynamic access { get; set; }

        [Key("functions")]
        //public GrantedFunctions functions { get; set; }
        public Dictionary<GrantedFunctionsType, List<(string, string)>> functions { get; set; }
    }
}