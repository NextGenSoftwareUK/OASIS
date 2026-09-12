using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NextGenSoftware.OASIS.Web7.Core.Enums;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.Core.Managers
{
    /// <summary>
    /// Real digital signal processing for every WEB7 bio-signal modality - a radix-2 Cooley-Tukey FFT for EEG
    /// band-power extraction, time-domain HRV metrics (RMSSD, SDNN) from RR-interval series, and GSR tonic/phasic
    /// decomposition. This is pure, deterministic computation - it takes raw sample arrays (captured by whatever
    /// external sensor/SDK the caller's device uses) and returns the processed feature set; it does not talk to
    /// hardware directly, the same way AIProviderManager doesn't implement its own HTTP stack from scratch.
    /// </summary>
    public class BioSignalProcessingManager
    {
        private static readonly (string Name, double LowHz, double HighHz)[] EegBands =
        {
            ("delta", 0.5, 4),
            ("theta", 4, 8),
            ("alpha", 8, 13),
            ("beta", 13, 30),
            ("gamma", 30, 45)
        };

        /// <summary>Computes normalised EEG band power (delta/theta/alpha/beta/gamma) for one channel's waveform via FFT.</summary>
        public Dictionary<string, double> ComputeEegBandPower(BioSignalSample sample)
        {
            if (sample.SignalType != BioSignalType.EEG)
                throw new ArgumentException("ComputeEegBandPower requires a sample of SignalType.EEG.");

            double[] windowed = ApplyHannWindow(sample.Values.ToArray());
            Complex[] spectrum = FFT(PadToPowerOfTwo(windowed));

            double binWidthHz = sample.SampleRateHz / spectrum.Length;
            int usableBins = spectrum.Length / 2; // Nyquist - only the first half carries real frequency content.

            Dictionary<string, double> bandPower = new Dictionary<string, double>();
            double totalPower = 0;

            double[] magnitudeSquared = new double[usableBins];
            for (int i = 0; i < usableBins; i++)
            {
                magnitudeSquared[i] = spectrum[i].Magnitude * spectrum[i].Magnitude;
                totalPower += magnitudeSquared[i];
            }

            foreach ((string name, double lowHz, double highHz) in EegBands)
            {
                int lowBin = (int)Math.Floor(lowHz / binWidthHz);
                int highBin = Math.Min(usableBins - 1, (int)Math.Ceiling(highHz / binWidthHz));
                double power = 0;

                for (int bin = Math.Max(0, lowBin); bin <= highBin; bin++)
                    power += magnitudeSquared[bin];

                // Normalise as a fraction of total spectral power so band powers are comparable across sessions/devices.
                bandPower[name] = totalPower > 0 ? power / totalPower : 0;
            }

            return bandPower;
        }

        /// <summary>Computes time-domain HRV metrics (RMSSD, SDNN, mean heart rate) from a series of RR intervals (ms).</summary>
        public Dictionary<string, double> ComputeHrvMetrics(BioSignalSample sample)
        {
            if (sample.SignalType != BioSignalType.HRV)
                throw new ArgumentException("ComputeHrvMetrics requires a sample of SignalType.HRV.");

            double[] rr = sample.Values.ToArray();

            if (rr.Length < 2)
                return new Dictionary<string, double> { ["rmssd"] = 0, ["sdnn"] = 0, ["meanHrBpm"] = 0 };

            double meanRr = rr.Average();
            double sdnn = Math.Sqrt(rr.Select(x => Math.Pow(x - meanRr, 2)).Sum() / (rr.Length - 1));

            double sumSquaredDiffs = 0;
            for (int i = 1; i < rr.Length; i++)
                sumSquaredDiffs += Math.Pow(rr[i] - rr[i - 1], 2);

            double rmssd = Math.Sqrt(sumSquaredDiffs / (rr.Length - 1));
            double meanHrBpm = 60000.0 / meanRr;

            return new Dictionary<string, double> { ["rmssd"] = rmssd, ["sdnn"] = sdnn, ["meanHrBpm"] = meanHrBpm };
        }

        /// <summary>Decomposes a raw GSR waveform into its slow-moving tonic (skin conductance level) and fast phasic (response) components.</summary>
        public Dictionary<string, double> ComputeGsrMetrics(BioSignalSample sample)
        {
            if (sample.SignalType != BioSignalType.GSR)
                throw new ArgumentException("ComputeGsrMetrics requires a sample of SignalType.GSR.");

            double[] values = sample.Values.ToArray();

            if (values.Length == 0)
                return new Dictionary<string, double> { ["tonicLevel"] = 0, ["phasicEnergy"] = 0 };

            // Tonic level: a simple moving average acts as a low-pass filter approximating the slow-changing skin conductance level.
            int windowSize = Math.Max(1, values.Length / 10);
            double[] tonic = MovingAverage(values, windowSize);
            double tonicLevel = tonic.Average();

            // Phasic component: raw signal minus its tonic baseline - the fast skin-conductance-response energy.
            double phasicEnergy = 0;
            for (int i = 0; i < values.Length; i++)
                phasicEnergy += Math.Pow(values[i] - tonic[i], 2);

            phasicEnergy = Math.Sqrt(phasicEnergy / values.Length);

            return new Dictionary<string, double> { ["tonicLevel"] = tonicLevel, ["phasicEnergy"] = phasicEnergy };
        }

        private static double[] MovingAverage(double[] values, int windowSize)
        {
            double[] result = new double[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(values.Length - 1, i + windowSize / 2);
                double sum = 0;

                for (int j = start; j <= end; j++)
                    sum += values[j];

                result[i] = sum / (end - start + 1);
            }

            return result;
        }

        private static double[] ApplyHannWindow(double[] values)
        {
            double[] windowed = new double[values.Length];

            for (int i = 0; i < values.Length; i++)
                windowed[i] = values[i] * 0.5 * (1 - Math.Cos(2 * Math.PI * i / (values.Length - 1)));

            return windowed;
        }

        private static Complex[] PadToPowerOfTwo(double[] values)
        {
            int n = 1;
            while (n < values.Length)
                n *= 2;

            Complex[] padded = new Complex[n];

            for (int i = 0; i < values.Length; i++)
                padded[i] = new Complex(values[i], 0);

            return padded;
        }

        /// <summary>Iterative, in-place radix-2 Cooley-Tukey FFT. Input length must be a power of two (callers go through PadToPowerOfTwo).</summary>
        private static Complex[] FFT(Complex[] input)
        {
            int n = input.Length;

            if (n <= 1)
                return input;

            // Bit-reversal permutation.
            Complex[] a = (Complex[])input.Clone();
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1)
                    j ^= bit;
                j ^= bit;

                if (i < j)
                    (a[i], a[j]) = (a[j], a[i]);
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2 * Math.PI / len;
                Complex wLen = new Complex(Math.Cos(angle), Math.Sin(angle));

                for (int i = 0; i < n; i += len)
                {
                    Complex w = Complex.One;

                    for (int k = 0; k < len / 2; k++)
                    {
                        Complex u = a[i + k];
                        Complex v = a[i + k + len / 2] * w;
                        a[i + k] = u + v;
                        a[i + k + len / 2] = u - v;
                        w *= wLen;
                    }
                }
            }

            return a;
        }
    }
}
