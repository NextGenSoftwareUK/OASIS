using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    /// <summary>
    /// Portable, versioned avatar preferences shared by Unity, Edge ONODE and hosted ONODE.
    /// Keep this contract free of Unity types so it remains NativeAOT/IL2CPP safe.
    /// </summary>
    public sealed class HyperDriveAvatarPreferences
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 0.8f;
        public float SoundVolume { get; set; } = 0.8f;
        public float VoiceVolume { get; set; } = 0.8f;
        public string GraphicsPreset { get; set; } = "High";
        public bool Fullscreen { get; set; }
        public string Resolution { get; set; } = "1920x1080";
        public string KeyOpenControlCenter { get; set; } = "I";
        public string KeyHideHostedGame { get; set; } = "F1";
        public string KeyReturnToHub { get; set; } = "CTRL+H";
        public int ToastMaxVisible { get; set; } = 3;
        public float ToastDurationSeconds { get; set; } = 1.7f;
        public float UiFontScale { get; set; } = 1f;
        public bool UiHighContrast { get; set; }
        public bool ShowStatusStrip { get; set; } = true;
        public HyperDriveViewPreset[] ViewPresets { get; set; } = Array.Empty<HyperDriveViewPreset>();
        public HyperDriveActiveViewPreset[] ActiveViewPresets { get; set; } = Array.Empty<HyperDriveActiveViewPreset>();
        public HyperDrivePanelLayout[] PanelLayouts { get; set; } = Array.Empty<HyperDrivePanelLayout>();
    }

    public sealed class HyperDriveViewPreset
    {
        public string Name { get; set; }
        public string Tab { get; set; }
        public string SearchQuery { get; set; }
        public string SortField { get; set; }
        public bool SortAscending { get; set; } = true;
    }

    public sealed class HyperDriveActiveViewPreset
    {
        public string Tab { get; set; }
        public string PresetName { get; set; }
    }

    public sealed class HyperDrivePanelLayout
    {
        public string PanelId { get; set; }
        public float AnchoredX { get; set; }
        public float AnchoredY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public static class HyperDriveAvatarPreferencesSettings
    {
        public static bool TryValidate(HyperDriveAvatarPreferences value, out string code, out string message)
        {
            if (value == null)
                return Invalid("COMMAND_PAYLOAD_INVALID", "Preferences are required.", out code, out message);
            if (!IsRange(value.MasterVolume, 0f, 1f) || !IsRange(value.MusicVolume, 0f, 1f) ||
                !IsRange(value.SoundVolume, 0f, 1f) || !IsRange(value.VoiceVolume, 0f, 1f))
                return Invalid("PREFERENCES_INVALID_VOLUME",
                    "Preference volumes must be finite values between zero and one.", out code, out message);
            if (value.ToastMaxVisible < 1 || value.ToastMaxVisible > 20 ||
                !IsRange(value.ToastDurationSeconds, 0.25f, 30f) || !IsRange(value.UiFontScale, 0.5f, 3f))
                return Invalid("PREFERENCES_INVALID_UI",
                    "Toast and font-scale preferences are outside their supported ranges.", out code, out message);
            if (string.IsNullOrWhiteSpace(value.GraphicsPreset) || string.IsNullOrWhiteSpace(value.Resolution))
                return Invalid("PREFERENCES_INVALID_DISPLAY",
                    "Graphics preset and resolution are required.", out code, out message);
            code = null;
            message = null;
            return true;
        }

        public static Dictionary<string, object> ToDictionary(HyperDriveAvatarPreferences value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["masterVolume"] = value.MasterVolume,
                ["musicVolume"] = value.MusicVolume,
                ["soundVolume"] = value.SoundVolume,
                ["voiceVolume"] = value.VoiceVolume,
                ["graphicsPreset"] = value.GraphicsPreset,
                ["fullscreen"] = value.Fullscreen,
                ["resolution"] = value.Resolution,
                ["keyOpenControlCenter"] = value.KeyOpenControlCenter,
                ["keyHideHostedGame"] = value.KeyHideHostedGame,
                ["keyReturnToHub"] = value.KeyReturnToHub,
                ["toastMaxVisible"] = value.ToastMaxVisible,
                ["toastDurationSeconds"] = value.ToastDurationSeconds,
                ["uiFontScale"] = value.UiFontScale,
                ["uiHighContrast"] = value.UiHighContrast,
                ["showStatusStrip"] = value.ShowStatusStrip,
                ["viewPresets"] = value.ViewPresets ?? Array.Empty<HyperDriveViewPreset>(),
                ["activeViewPresets"] = value.ActiveViewPresets ?? Array.Empty<HyperDriveActiveViewPreset>(),
                ["panelLayouts"] = value.PanelLayouts ?? Array.Empty<HyperDrivePanelLayout>()
            };
        }

        private static bool IsRange(float value, float minimum, float maximum) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;
        private static bool Invalid(string errorCode, string errorMessage, out string code, out string message)
        { code = errorCode; message = errorMessage; return false; }
    }
}
