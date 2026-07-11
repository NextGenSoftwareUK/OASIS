using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>A single piece of memory recorded at a holon (e.g. a session fact, a performance signal, a topic).</summary>
    public class HolonicMemoryItem
    {
        public string FieldName { get; set; }

        public string Value { get; set; }

        /// <summary>Tags used to evaluate membrane TriggerConditions, e.g. "topic:professional", "anonymised".</summary>
        public List<string> Tags { get; set; } = new List<string>();

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Priority 16c — TTL enforcement
        /// <summary>How long this memory persists. Default Persistent (no expiry).</summary>
        public RetentionPolicy RetentionPolicy { get; set; } = RetentionPolicy.Persistent;

        /// <summary>Absolute UTC expiry for TimeLimited retention. Null = no expiry.</summary>
        public DateTime? ExpiresUtc { get; set; }

        /// <summary>Returns true when this item has passed its TTL and should be purged.</summary>
        public bool IsExpired => ExpiresUtc.HasValue && DateTime.UtcNow > ExpiresUtc.Value;
    }

    /// <summary>A holon in the Holonic BRAID fractal memory hierarchy (Session → Agent → User → Group → ... → Earth).</summary>
    public class HolonicMemoryHolonDto
    {
        public Guid Id { get; set; }

        public Enums.HolonicMemoryLevel Level { get; set; }

        public Guid ParentHolonId { get; set; }

        public string Name { get; set; }

        public List<HolonicMemoryItem> MemoryItems { get; set; } = new List<HolonicMemoryItem>();

        public MembraneRule MembraneRule { get; set; } = new MembraneRule();
    }
}
