
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class DnaStorageBlob
    {
        [Key("dna")]
        public DnaStorageInfo dna { get; set; } 
    }
}