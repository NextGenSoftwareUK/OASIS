
using System;
using System.Collections.Generic;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class Record : IRecord
    {
        /// <summary>
        /// The author of the entry (AgentPubKey on their device).
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The ActionHash
        /// </summary>
        public string ActionHash { get; set; }

        /// <summary>
        /// Hash of the entry
        /// </summary>
        public string EntryHash { get; set; }

        /// <summary>
        /// The previous ActionHash.
        /// </summary>
        public string PreviousActionHash { get; set; }

        /// <summary>
        /// The signature of the entry.
        /// </summary>
        public string Signature { get; set; } //Not sure what this is?

        /// <summary>
        /// The Unix timestamp (returned from the Holochain Conductor)
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Standard .NET DateTime converted from the Unix timestamp (returned from the Holochain Conductor).
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Create/Update/Delete
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// //App/Not sure what other types there are?
        /// </summary>
        public string EntryType { get; set; }

        /// <summary>
        /// The Action Sequence for this entry.
        /// </summary>
        public int ActionSequence { get; set; }

        /// <summary>
        /// Is the original Hash for the entry.
        /// </summary>
        public string OriginalActionAddress { get; set; }

        /// <summary>
        /// Is the original EntryHash for the entry.
        /// </summary>
        public string OriginalEntryAddress { get; set; }

        /// <summary>
        /// The raw bytes returned from the Holochain Conductor for the entry.
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// The raw bytes returned from the Holochain Conductor for the entry formatted as a string making logging etc easier.
        /// </summary>
        public string BytesString { get; set; }

        /// <summary>
        /// A key/value pair/dictionary containing the entry data itself.
        /// </summary>
        public Dictionary<string, object> EntryKeyValuePairs { get; set; }

        /// <summary>
        /// A dynamic object constructed from the entry data.
        /// </summary>
        public dynamic EntryDataObject { get; set; }
    }
}