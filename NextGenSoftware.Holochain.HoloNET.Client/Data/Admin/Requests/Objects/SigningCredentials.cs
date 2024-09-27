
namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    public class SigningCredentials
    {
        public byte[] CapSecret { get; set; }
        public KeyPair KeyPair { get; set; }
        public byte[] SigningKey { get; set; }
    }
}