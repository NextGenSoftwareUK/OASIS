using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    /// <summary>
    /// A single entry in an avatar's activity feed.
    /// <para>
    /// This is a flattened projection of <c>KarmaAkashicRecord</c>. The Akashic Records
    /// already are the OASIS activity log, so the feed reads from them rather than from a
    /// separate event store. Flattening matters because the underlying record wraps its
    /// enums in <c>EnumValue&lt;T&gt;</c>, which serialises as a nested object and is
    /// awkward for clients to unpack.
    /// </para>
    /// </summary>
    public class ActivityFeedEntry
    {
        /// <summary>Avatar the activity belongs to.</summary>
        public Guid AvatarId { get; set; }

        /// <summary>When the activity occurred.</summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Activity type, taken from the positive or negative karma type as appropriate
        /// (for example "SelfHelpImprovement" or "BeingUnhelpful").
        /// </summary>
        public string ActivityType { get; set; }

        /// <summary>Name of the app, website or game the karma came from.</summary>
        public string SourceTitle { get; set; }

        /// <summary>Description of what happened.</summary>
        public string SourceDescription { get; set; }

        /// <summary>Kind of source: App, dApp, hApp, Website or Game.</summary>
        public string SourceType { get; set; }

        /// <summary>Karma delta for this activity. Negative when karma was lost.</summary>
        public int Karma { get; set; }

        /// <summary>The avatar's running karma total after this activity.</summary>
        public long TotalKarma { get; set; }

        /// <summary>True when karma was earnt, false when it was lost.</summary>
        public bool IsPositive { get; set; }

        /// <summary>OASIS provider the record was stored against.</summary>
        public string Provider { get; set; }

        /// <summary>Optional link to the source of the activity.</summary>
        public string WebLink { get; set; }
    }
}
