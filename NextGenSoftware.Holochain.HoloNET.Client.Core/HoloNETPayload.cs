
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    [MessagePackObject]
    public class HoloNETPayload
    {
        [Key(0)]
        public string id { get; set; }

        [Key(1)]
        public string type { get; set; }

        [Key(2)]
        public HoloNETData data { get; set; }

        /*
        // Key attributes take a serialization index (or string name)
        // The values must be unique and versioning has to be considered as well.
        // Keys are described in later sections in more detail.
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string FirstName { get; set; }

        [Key(2)]
        public string LastName { get; set; }

        // All fields or properties that should not be serialized must be annotated with [IgnoreMember].
        [IgnoreMember]
        public string FullName { get { return FirstName + LastName; } }
        */
    }

    public class HoloNETData
    {
        public string cap { get; set; } //CapSecret | null = string
        public string[,] cell_id { get; set; } = new string[1, 2] //CellId = [HoloHash, AgentPubKey] = [string, string] = 2 dimensional array.
        public string zome_name { get; set; }
        public string fn_name { get; set; }
        public string payload { get; set; } //Payload - What is Payload object?
        public string provenance { get; set; } //AgentPubKey = string

    }
}
