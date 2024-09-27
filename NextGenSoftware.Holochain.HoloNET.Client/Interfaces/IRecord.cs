using System;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IRecord
    {
        int ActionSequence { get; set; }
        string Author { get; set; }
        byte[] Bytes { get; set; }
        string BytesString { get; set; }
        DateTime DateTime { get; set; }
        dynamic EntryDataObject { get; set; }
        string EntryHash { get; set; }
        Dictionary<string, object> EntryKeyValuePairs { get; set; }
        string EntryType { get; set; }
        string ActionHash { get; set; }
        string OriginalActionAddress { get; set; }
        string OriginalEntryAddress { get; set; }
        string PreviousActionHash { get; set; }
        string Signature { get; set; }
        long Timestamp { get; set; }
        string Type { get; set; }
    }
}