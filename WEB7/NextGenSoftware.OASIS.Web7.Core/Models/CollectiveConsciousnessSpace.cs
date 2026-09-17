using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web7.Core.Models
{
    /// <summary>A shared intention field where multiple consenting symbiosis sessions co-create in real time.</summary>
    public class CollectiveConsciousnessSpace
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public List<Guid> ParticipantSessionIds { get; set; } = new List<Guid>();

        /// <summary>The aggregate (mean) intention state across every consenting participant - never any individual's raw signal.</summary>
        public IntentionState AggregateState { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
