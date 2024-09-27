
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public struct Action
    {
        [Key("Dna")]
        public Dna Dna { get; set; }

        //[Key("AgentValidationPkg")]
        //public AgentValidationPkg AgentValidationPkg { get; set; }

        //[Key("InitZomesComplete")]
        //public InitZomesComplete InitZomesComplete { get; set; }

        //[Key("CreateLink")]
        //public CreateLink CreateLink { get; set; }

        //[Key("DeleteLink")]
        //public DeleteLink DeleteLink { get; set; }

        //[Key("OpenChain")]
        //public OpenChain OpenChain { get; set; }

        //[Key("CloseChain")]
        //public CloseChain CloseChain { get; set; }

        //[Key("Create")]
        //public Create Create { get; set; }

        //[Key("Update")]
        //public Update Update { get; set; }

        //[Key("Delete")]
        //public Delete Delete { get; set; }
    }
}