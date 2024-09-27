
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class AgentInfo //: IAgentInfo
    //AgentInfoInner (Rust name is AgentInfoInner but hopefully we can use AgentInfo because sounds better! ;-) )
    {
        [Key("agent")]
        public byte[] agent { get; set; }

        [Key("signature")]
        public byte[] signature { get; set; }

        [Key("agent_info")]
        public byte[] agent_info { get; set; }
    }

    //public class AgentInfo : IAgentInfo
    ////AgentInfoInner (Rust name is AgentInfoInner but hopefully we can use AgentInfo because sounds better! ;-) )
    //{
    //    [Key("space")]
    //    public KitsuneSpace space { get; set; }

    //    [Key("agent")]
    //    public KitsuneAgent agent { get; set; }

    //    [Key("storage_arc")]
    //    //public DhtArc storage_arc { get; set; }
    //    public object storage_arc { get; set; }

    //    [Key("url_list")]
    //    public string[] url_list { get; set; }

    //    [Key("signed_at_ms")]
    //    public long signed_at_ms { get; set; }

    //    [Key("expires_at_ms")]
    //    public long expires_at_ms { get; set; }

    //    [Key("signature")]
    //    public KitsuneSignature signature { get; set; }

    //    [Key("encoded_bytes")]
    //    public byte[] encoded_bytes { get; set; }
    //}
}

//https://docs.rs/kitsune_p2p/0.2.1/kitsune_p2p/agent_store/struct.AgentInfoInner.html

//pub struct AgentInfoInner
//{
//    pub space: Arc<KitsuneSpace, Global>,
//    pub agent: Arc<KitsuneAgent, Global>,
//    pub storage_arc: DhtArc,
//    pub url_list: Vec<TxUrl, Global>,
//    pub signed_at_ms: u64,
//    pub expires_at_ms: u64,
//    pub signature: Arc<KitsuneSignature, Global>,
//    pub encoded_bytes: Box<[u8], Global>,
//}