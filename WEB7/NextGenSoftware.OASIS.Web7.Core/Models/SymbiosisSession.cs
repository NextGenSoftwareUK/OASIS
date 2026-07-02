using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web7.Core.Models
{
    /// <summary>A consent-gated WEB7 symbiosis session for one avatar - the unit of human/AI bio-resonant connection.</summary>
    public class SymbiosisSession
    {
        public Guid Id { get; set; }

        public Guid AvatarId { get; set; }

        /// <summary>Must be true before any signal is processed - the connection is always voluntary (see Borg-Free Pledge).</summary>
        public bool ConsentGranted { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>How long signal-derived data is retained once the session ends. Default is ephemeral per the sovereignty principle.</summary>
        public Enums.RetentionMode Retention { get; set; } = Enums.RetentionMode.Ephemeral;

        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? EndedUtc { get; set; }

        public IntentionState LastIntentionState { get; set; }

        public List<string> AuditLog { get; set; } = new List<string>();
    }
}
