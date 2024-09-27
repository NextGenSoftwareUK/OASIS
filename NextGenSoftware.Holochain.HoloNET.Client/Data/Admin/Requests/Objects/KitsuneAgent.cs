namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    public class KitsuneAgent
    {
        public byte[] Data { get; set; }

        public KitsuneAgent(byte[] data)
        {
            Data = data;
        }

        public byte[] GetData()
        {
            return Data;
        }
    }
}

//https://docs.rs/kitsune_p2p/0.2.1/kitsune_p2p/struct.KitsuneAgent.html
//pub struct KitsuneAgent(pub Vec<u8, Global>);