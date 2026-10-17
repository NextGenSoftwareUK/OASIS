using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.Web7.Core.Enums;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.Core.Managers
{
    /// <summary>
    /// Maps processed bio-signal features (from BioSignalProcessingManager) into the human-meaningful IntentionState
    /// (focus, arousal, emotional valence, cognitive load) that the rest of WEB7 reasons about. The mapping uses
    /// well-established psychophysiology heuristics: EEG alpha/beta ratio for relaxed-focus, EEG theta/beta ratio
    /// for cognitive load, HRV (RMSSD) and GSR phasic energy for arousal/valence.
    /// </summary>
    public class IntentionEngine
    {
        private readonly BioSignalProcessingManager _signalProcessor = new BioSignalProcessingManager();

        public IntentionState ComputeIntentionState(IEnumerable<BioSignalSample> samples)
        {
            IntentionState state = new IntentionState();
            List<BioSignalSample> sampleList = samples?.ToList() ?? new List<BioSignalSample>();

            BioSignalSample eeg = sampleList.FirstOrDefault(s => s.SignalType == BioSignalType.EEG);
            BioSignalSample hrv = sampleList.FirstOrDefault(s => s.SignalType == BioSignalType.HRV);
            BioSignalSample gsr = sampleList.FirstOrDefault(s => s.SignalType == BioSignalType.GSR);

            if (eeg != null && eeg.Values.Count > 1)
            {
                Dictionary<string, double> bands = _signalProcessor.ComputeEegBandPower(eeg);

                foreach (KeyValuePair<string, double> band in bands)
                    state.Features[$"eeg_{band.Key}_power"] = band.Value;

                double alpha = bands.GetValueOrDefault("alpha");
                double beta = bands.GetValueOrDefault("beta");
                double theta = bands.GetValueOrDefault("theta");

                // Relaxed-focus index: high alpha relative to beta indicates calm, sustained attention.
                state.Focus = Clamp01(alpha / (alpha + beta + 1e-9) * 2);

                // Cognitive load index: high theta relative to beta indicates mental effort/working-memory load.
                state.CognitiveLoad = Clamp01(theta / (theta + beta + 1e-9) * 2);
            }

            if (hrv != null && hrv.Values.Count > 1)
            {
                Dictionary<string, double> hrvMetrics = _signalProcessor.ComputeHrvMetrics(hrv);

                foreach (KeyValuePair<string, double> metric in hrvMetrics)
                    state.Features[$"hrv_{metric.Key}"] = metric.Value;

                // Higher RMSSD (parasympathetic/vagal tone) correlates with calmer, more positive affect.
                double rmssd = hrvMetrics.GetValueOrDefault("rmssd");
                state.EmotionalValence += Clamp(rmssd / 50.0 - 1.0, -1, 1) * 0.5;
            }

            if (gsr != null && gsr.Values.Count > 0)
            {
                Dictionary<string, double> gsrMetrics = _signalProcessor.ComputeGsrMetrics(gsr);

                foreach (KeyValuePair<string, double> metric in gsrMetrics)
                    state.Features[$"gsr_{metric.Key}"] = metric.Value;

                // Higher phasic energy (skin-conductance responses) indicates heightened arousal/activation.
                state.Arousal = Clamp01(gsrMetrics.GetValueOrDefault("phasicEnergy") / 2.0);

                // Strong, frequent phasic responses without corresponding HRV calm pull valence toward negative (stress).
                state.EmotionalValence -= Clamp01(gsrMetrics.GetValueOrDefault("phasicEnergy") / 4.0) * 0.5;
            }

            state.EmotionalValence = Clamp(state.EmotionalValence, -1, 1);
            return state;
        }

        private static double Clamp01(double value) => Clamp(value, 0, 1);

        private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    }
}
