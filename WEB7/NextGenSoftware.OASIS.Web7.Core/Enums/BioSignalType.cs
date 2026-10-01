namespace NextGenSoftware.OASIS.Web7.Core.Enums
{
    /// <summary>
    /// The non-invasive, externally-read bio-signal modalities WEB7 accepts. There is deliberately no enum value
    /// for any implanted/sub-dermal/cranial signal source - the Borg-Free pledge is enforced in code, not just policy
    /// (see SymbiosisSessionManager.ValidateBorgFreeCompliance).
    /// </summary>
    public enum BioSignalType
    {
        /// <summary>External EEG electrodes (e.g. Muse, OpenBCI headset) - brainwave band power.</summary>
        EEG,

        /// <summary>Heart-rate variability from a chest strap or optical PPG sensor.</summary>
        HRV,

        /// <summary>Galvanic skin response / electrodermal activity.</summary>
        GSR,

        /// <summary>Eye-tracking gaze and pupil dilation.</summary>
        EyeTracking,

        /// <summary>Vocal harmonics / prosody analysis from voice input.</summary>
        VocalHarmonics
    }
}
