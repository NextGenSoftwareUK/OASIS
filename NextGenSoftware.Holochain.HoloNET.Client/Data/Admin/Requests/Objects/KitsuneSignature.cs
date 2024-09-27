
namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    public class KitsuneSignature
    {
        public byte[] Data { get; set; }

        public KitsuneSignature(byte[] data)
        {
            Data = data;
        }

        public byte[] GetData() 
        {
            return Data;
        }
    }
}

//https://docs.rs/kitsune_p2p/0.2.1/kitsune_p2p/struct.KitsuneSignature.html
//pub struct KitsuneSignature(pub Vec<u8, Global>);