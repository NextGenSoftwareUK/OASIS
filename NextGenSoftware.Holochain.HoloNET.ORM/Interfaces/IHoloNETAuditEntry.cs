using NextGenSoftware.Holochain.HoloNET.Client;

namespace NextGenSoftware.Holochain.HoloNET.ORM
{
    public interface IHoloNETAuditEntry
    {
        DateTime DateTime { get; set; }
        string EntryHash { get; set; }
        HoloNETAuditEntryType Type { get; set; }
    }
}