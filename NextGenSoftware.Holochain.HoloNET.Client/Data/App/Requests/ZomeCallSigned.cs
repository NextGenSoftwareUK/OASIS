using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class ZomeCallSigned : ZomeCall
    {
        [Key("signature")]
        //[Key(8)]
        public byte[] signature { get; set; }
    }
}