using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// The governed boundary through which information passes between two holons in the Holonic BRAID hierarchy.
    /// The default is private - nothing propagates without an explicit rule. Per-field granularity, matching the
    /// OASIS WEB4 field-level data control system.
    /// </summary>
    public class MembraneRule
    {
        /// <summary>The field/topic/knowledge-category names from the child holon that are allowed to flow upward to the parent.</summary>
        public List<string> FieldsAllowedToPropagate { get; set; } = new List<string>();

        /// <summary>How long the propagated memory is retained at the parent holon.</summary>
        public Enums.RetentionPolicy Retention { get; set; } = Enums.RetentionPolicy.SessionScoped;

        /// <summary>Holon ids (parents, siblings or external agents) explicitly permitted to query/read this holon's propagated memory. Read access is separate from propagation.</summary>
        public List<System.Guid> WhoCanRead { get; set; } = new List<System.Guid>();

        /// <summary>
        /// Optional conditional trigger, e.g. "topic=professional" or "anonymised-aggregate-only". Evaluated against
        /// the memory item's own tags before it is allowed to propagate - if empty, the rule applies unconditionally.
        /// </summary>
        public string TriggerCondition { get; set; }

        /// <summary>True if only anonymised/aggregate patterns (not raw content) may propagate under this rule.</summary>
        public bool AnonymisedAggregateOnly { get; set; }
    }
}
