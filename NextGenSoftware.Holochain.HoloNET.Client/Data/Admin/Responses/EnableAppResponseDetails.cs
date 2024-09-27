
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class EnableAppResponseDetails
    {
        [Key("app")]
        public AppInfo app { get; set; }

        [Key("errors")]
        //public [byte[][], string] Errors { get; set; } //errors: Array<[CellId, string]>;
        public object errors { get; set; } //errors: Array<[CellId, string]>; //TODO: Need to find out what this contains and the correct data structure.
    }
}