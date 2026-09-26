using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web7.Core.Enums;

namespace NextGenSoftware.OASIS.Web7.Core.Models
{
    /// <summary>A single channel's raw waveform/series from an external bio-signal sensor over a time window.</summary>
    public class BioSignalSample
    {
        public BioSignalType SignalType { get; set; }

        /// <summary>Channel name, e.g. "Fp1", "Fp2" for EEG, or "RR" for HRV inter-beat intervals.</summary>
        public string Channel { get; set; }

        /// <summary>Sample rate in Hz (EEG/GSR/EyeTracking). Not required for HRV, which is supplied as RR intervals in ms.</summary>
        public double SampleRateHz { get; set; } = 256;

        /// <summary>Raw values - EEG/GSR/EyeTracking are evenly-sampled waveforms; HRV values are RR intervals in milliseconds.</summary>
        public List<double> Values { get; set; } = new List<double>();

        public DateTime CapturedUtc { get; set; } = DateTime.UtcNow;
    }
}
