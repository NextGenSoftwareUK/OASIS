using System;
using System.Collections.Generic;

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
