
using MessagePack;
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class DhtOp
    {
        [Key("StoreRecord")]
        public (Signature, Action, RecordEntry) StoreRecord { get; set; } //(Signature, Action, RecordEntry) 

        //[Key("StoreEntry")]
        //public (Signature, NewEntryAction, Entry) StoreEntry { get; set; } 

        //[Key("RegisterAgentActivity")]
        //public (Signature, Action) RegisterAgentActivity { get; set; } 

        //[Key("RegisterUpdatedContent")]
        //public (Signature, Update, RecordEntry) RegisterUpdatedContent { get; set; }

        //[Key("RegisterUpdatedRecord")]
        //public (Signature, Update, RecordEntry) RegisterUpdatedRecord { get; set; }

        //[Key("RegisterDeletedBy")]
        //public (Signature, Delete) RegisterDeletedBy { get; set; }

        //[Key("RegisterDeletedEntryAction")]
        //public (Signature, Delete) RegisterDeletedEntryAction { get; set; }

        //[Key("RegisterAddLink")]
        //public (Signature, CreateLink) RegisterAddLink { get; set; }

        //[Key("RegisterRemoveLink")]
        //public (Signature, DeleteLink) RegisterRemoveLink { get; set; }
    }
}