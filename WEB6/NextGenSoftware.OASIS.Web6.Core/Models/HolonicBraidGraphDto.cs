using System;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A reasoning graph in the Holonic BRAID shared library - a Mermaid execution graph generated once per
    /// task type by a high-tier "generator" agent, then re-used (and executed cheaply) by every future agent/session
    /// doing the same task type. Stored as a HolonicBraidGraph holon so it persists/replicates across every OASIS provider.
    /// </summary>
    public class HolonicBraidGraphDto
    {
        public Guid Id { get; set; }

        public string TaskType { get; set; }

        public string MermaidDiagram { get; set; }

        public string GeneratedByModel { get; set; }

        public int TimesReused { get; set; }

        public DateTime CreatedUtc { get; set; }

        /// <summary>The Library holon this graph holon is a child of (per whitepaper §3.1 - the library holon is itself a holon whose children are individual graph holons, one per task type).</summary>
        public Guid ParentHolonId { get; set; }

        /// <summary>Average solver accuracy reported back from real outcomes (0-1). Drives accuracy-threshold regeneration.</summary>
        public double AvgSolverAccuracy { get; set; } = 1.0;

        /// <summary>Bumped every time a new version of this graph replaces a previous one (accuracy regeneration or conflict resolution).</summary>
        public int Version { get; set; } = 1;
    }
}
