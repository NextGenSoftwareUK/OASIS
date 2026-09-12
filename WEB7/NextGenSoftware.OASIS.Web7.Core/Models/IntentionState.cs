using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web7.Core.Models
{
    /// <summary>The processed, human-meaningful intention/cognitive state derived from raw bio-signals for one moment in time.</summary>
    public class IntentionState
    {
        /// <summary>0-1, derived from EEG alpha/beta ratio - higher means more focused/attentive.</summary>
        public double Focus { get; set; }

        /// <summary>-1 (negative) to +1 (positive) emotional valence, derived from HRV and GSR.</summary>
        public double EmotionalValence { get; set; }

        /// <summary>0-1 arousal/activation level, derived from GSR and HRV.</summary>
        public double Arousal { get; set; }

        /// <summary>0-1 cognitive load, derived from EEG theta/beta ratio.</summary>
        public double CognitiveLoad { get; set; }

        /// <summary>Raw computed signal features this state was derived from, keyed by feature name (e.g. "eeg_alpha_power").</summary>
        public Dictionary<string, double> Features { get; set; } = new Dictionary<string, double>();

        public DateTime ComputedUtc { get; set; } = DateTime.UtcNow;
    }
}
