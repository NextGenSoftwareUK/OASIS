using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OASIS.Omniverse.UnityHost.API;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public class GlobalSettingsService
    {
        private const string LocalSettingsKey = "OASIS_OMNIVERSE_GLOBAL_SETTINGS";

        public OmniverseGlobalSettings CurrentSettings { get; private set; } = new OmniverseGlobalSettings();

        public async Task<OASISResult<OmniverseGlobalSettings>> InitializeAsync(Web4Web5GatewayClient apiClient)
        {
            var localResult = LoadLocal();
            if (!localResult.IsError)
            {
                CurrentSettings = localResult.Result;
            }

            var remoteResult = await apiClient.GetGlobalPreferencesAsync();
            if (!remoteResult.IsError && remoteResult.Result != null)
            {
                CurrentSettings = MergeSettings(CurrentSettings, remoteResult.Result);
                SaveLocal(CurrentSettings);
            }

            ApplyToUnity(CurrentSettings);
            return OASISResult<OmniverseGlobalSettings>.Success(CurrentSettings);
        }

        public async Task<OASISResult<bool>> SaveAndApplyAsync(OmniverseGlobalSettings settings, Web4Web5GatewayClient apiClient)
        {
            CurrentSettings = settings ?? new OmniverseGlobalSettings();
            SaveLocal(CurrentSettings);
            ApplyToUnity(CurrentSettings);

            var remoteSave = await apiClient.SaveGlobalPreferencesAsync(CurrentSettings);
            if (remoteSave.IsError)
            {
                return OASISResult<bool>.Error($"Saved locally, but failed to save to WEB4 settings API: {remoteSave.Message}");
            }

            return OASISResult<bool>.Success(true, "Global settings saved locally and to WEB4 settings API.");
        }

        public async Task<OASISResult<bool>> SavePreferencesOnlyAsync(OmniverseGlobalSettings settings, Web4Web5GatewayClient apiClient)
        {
            CurrentSettings = settings ?? new OmniverseGlobalSettings();
            SaveLocal(CurrentSettings);

            var remoteSave = await apiClient.SaveGlobalPreferencesAsync(CurrentSettings);
            if (remoteSave.IsError)
            {
                return OASISResult<bool>.Error($"Saved locally, but failed to save to WEB4 settings API: {remoteSave.Message}");
            }

            return OASISResult<bool>.Success(true, "UI preferences saved locally and to WEB4 settings API.");
        }

        public OASISResult<OmniverseGlobalSettings> CloneCurrentSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(CurrentSettings ?? new OmniverseGlobalSettings());
                var clone = JsonConvert.DeserializeObject<OmniverseGlobalSettings>(json);
                if (clone == null)
                {
                    return OASISResult<OmniverseGlobalSettings>.Error("Could not clone current settings.");
                }

                return OASISResult<OmniverseGlobalSettings>.Success(clone);
            }
            catch (Exception ex)
            {
                return OASISResult<OmniverseGlobalSettings>.Error($"Failed to clone settings: {ex.Message}");
            }
        }

        public OASISResult<string> BuildLaunchArgumentsForGame(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                return OASISResult<string>.Error("Game id is required.");
            }

            var s = CurrentSettings ?? new OmniverseGlobalSettings();
            var sb = new StringBuilder();

            if (gameId.Equals("odoom", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendFormat(" +snd_mastervolume {0:0.00}", Mathf.Clamp01(s.masterVolume));
                sb.AppendFormat(" +snd_musicvolume {0:0.00}", Mathf.Clamp01(s.musicVolume));
                sb.AppendFormat(" +snd_midivolume {0:0.00}", Mathf.Clamp01(s.voiceVolume));
            }
            else if (gameId.Equals("oquake", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendFormat(" +volume {0:0.00}", Mathf.Clamp01(s.masterVolume));
                sb.AppendFormat(" +bgmvolume {0:0.00}", Mathf.Clamp01(s.musicVolume));
            }

            return OASISResult<string>.Success(sb.ToString().Trim());
        }

        public OASISResult<KeyCode> ResolveKeyBinding(string keyText, KeyCode fallback)
        {
            if (string.IsNullOrWhiteSpace(keyText))
            {
                return OASISResult<KeyCode>.Success(fallback);
            }

            if (Enum.TryParse(keyText, true, out KeyCode parsed))
            {
                return OASISResult<KeyCode>.Success(parsed);
            }

            return OASISResult<KeyCode>.Error($"Unable to parse key binding '{keyText}'.");
        }

        private static OmniverseGlobalSettings MergeSettings(OmniverseGlobalSettings local, OmniverseGlobalSettings remote)
        {
            var merged = local ?? new OmniverseGlobalSettings();
            if (remote == null)
            {
                return merged;
            }

            merged.masterVolume = remote.masterVolume;
            merged.musicVolume = remote.musicVolume;
            merged.soundVolume = remote.soundVolume;
            merged.voiceVolume = remote.voiceVolume;
            merged.graphicsPreset = string.IsNullOrWhiteSpace(remote.graphicsPreset) ? merged.graphicsPreset : remote.graphicsPreset;
            merged.fullscreen = remote.fullscreen;
            merged.resolution = string.IsNullOrWhiteSpace(remote.resolution) ? merged.resolution : remote.resolution;
            merged.keyOpenControlCenter = string.IsNullOrWhiteSpace(remote.keyOpenControlCenter) ? merged.keyOpenControlCenter : remote.keyOpenControlCenter;
            merged.keyHideHostedGame = string.IsNullOrWhiteSpace(remote.keyHideHostedGame) ? merged.keyHideHostedGame : remote.keyHideHostedGame;
            merged.viewPresets = remote.viewPresets ?? merged.viewPresets ?? new System.Collections.Generic.List<OmniverseViewPreset>();
            merged.activeViewPresets = remote.activeViewPresets ?? merged.activeViewPresets ?? new System.Collections.Generic.List<OmniverseActiveViewPreset>();
            merged.panelLayouts = remote.panelLayouts ?? merged.panelLayouts ?? new System.Collections.Generic.List<OmniversePanelLayout>();
            return merged;
        }

        private static OASISResult<OmniverseGlobalSettings> LoadLocal()
        {
            if (!PlayerPrefs.HasKey(LocalSettingsKey))
            {
                return OASISResult<OmniverseGlobalSettings>.Error("No local settings found.");
            }

            try
            {
                var json = PlayerPrefs.GetString(LocalSettingsKey);
                var parsed = JsonConvert.DeserializeObject<OmniverseGlobalSettings>(json);
                if (parsed == null)
                {
                    return OASISResult<OmniverseGlobalSettings>.Error("Local settings are invalid.");
                }

                return OASISResult<OmniverseGlobalSettings>.Success(parsed);
            }
            catch (Exception ex)
            {
                return OASISResult<OmniverseGlobalSettings>.Error($"Failed reading local settings: {ex.Message}");
            }
        }

        private static void SaveLocal(OmniverseGlobalSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            PlayerPrefs.SetString(LocalSettingsKey, json);
            PlayerPrefs.Save();
        }

        private static void ApplyToUnity(OmniverseGlobalSettings settings)
        {
            AudioListener.volume = Mathf.Clamp01(settings.masterVolume);
            Screen.fullScreen = settings.fullscreen;

            if (settings.graphicsPreset.Equals("Low", StringComparison.OrdinalIgnoreCase))
            {
                QualitySettings.SetQualityLevel(0, true);
            }
            else if (settings.graphicsPreset.Equals("Medium", StringComparison.OrdinalIgnoreCase))
            {
                QualitySettings.SetQualityLevel(Mathf.Min(2, QualitySettings.names.Length - 1), true);
            }
            else if (settings.graphicsPreset.Equals("High", StringComparison.OrdinalIgnoreCase))
            {
                QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.names.Length - 2), true);
            }
            else if (settings.graphicsPreset.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
            {
                QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.names.Length - 1), true);
            }
        }
    }
}




