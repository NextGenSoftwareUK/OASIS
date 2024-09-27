using NextGenSoftware.Holochain.HoloNET.Client;
namespace NextGenSoftware.Holochain.HoloNET.ORM.Entries
{
    /// <summary>
    /// Contains audit information for a Holochain Entry.
    /// </summary>
    public class HoloNETAuditEntry : IHoloNETAuditEntry
    {
        /// <summary>
        /// The entry hash.
        /// </summary>
        public string EntryHash { get; set; }

        /// <summary>
        /// The datetime the entry was created/updated/deleted.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// The audit action (created, updated or deleted).
        /// </summary>
        public HoloNETAuditEntryType Type { get; set; }
    }
}
