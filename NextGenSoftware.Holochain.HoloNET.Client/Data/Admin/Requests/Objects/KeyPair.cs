
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    public class KeyPair
    {
        public byte[] PrivateKey { get; set; }
        public byte[] PublicKey { get; set; }
    }
}