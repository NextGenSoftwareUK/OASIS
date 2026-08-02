using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using OASIS.Omniverse.UnityHost.UI;
using System.IO;
using UnityEngine;
using OASIS.Omniverse.UnityHost.API;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    [System.Serializable]
    public class OmniverseRuntimeHealthSnapshot
    {
        public GatewayHealthSnapshot api = new GatewayHealthSnapshot();
        public HostRuntimeHealthSnapshot host = new HostRuntimeHealthSnapshot();
    }

    public class OmniverseKernel : MonoBehaviour
    {
        public static OmniverseKernel Instance { get; private set; }

        private OmniverseHostConfig _config;
        private GameProcessHostService _hostService;
        private SharedHudOverlay _hudOverlay;
        private QuestTrackerWidget _questTrackerWidget;
        private Web4Web5GatewayClient _apiClient;
        private GlobalSettingsService _globalSettingsService;
        private LoginScreen _loginScreen;
        private bool _isInitialized;
        private float _lastTeleportPollTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            var root = new GameObject("OASIS_Omniverse_Kernel");
            root.AddComponent<OmniverseKernel>();
            DontDestroyOnLoad(root);
        }

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            var configResult = HostConfigLoader.Load();
            if (configResult.IsError)
            {
                Debug.LogError(configResult.Message);
                enabled = false;
                return;
            }

            _config = configResult.Result;

            // Check if avatarId is set, if not show login screen
            if (string.IsNullOrWhiteSpace(_config.avatarId))
            {
                ShowLoginScreen();
                return;
            }

            // AvatarId is set, proceed with initialization
            await InitializeAsync();
        }

        private void ShowLoginScreen()
        {
            _loginScreen = gameObject.AddComponent<LoginScreen>();
            _loginScreen.Initialize(_config.web4OasisApiBaseUrl);
            _loginScreen.OnBeamInSuccess += OnBeamInSuccess;
            _loginScreen.Show();
        }

        private async void OnBeamInSuccess(string avatarId, string jwtToken)
        {
            if (_loginScreen != null)
            {
                _loginScreen.Hide();
                Destroy(_loginScreen);
                _loginScreen = null;
            }

            // Update config with login credentials
            _config.avatarId = avatarId;
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                _config.apiKey = jwtToken;
            }

            // Save updated config
            var saveResult = HostConfigLoader.Save(_config);
            if (saveResult.IsError)
            {
                Debug.LogError($"Failed to save config: {saveResult.Message}");
            }

            // Now initialize everything
            await InitializeAsync();
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            
            _apiClient = new Web4Web5GatewayClient(_config.web4OasisApiBaseUrl, _config.web5StarApiBaseUrl, _config.apiKey, _config.avatarId);
            _globalSettingsService = new GlobalSettingsService();
            await _globalSettingsService.InitializeAsync(_apiClient);
            
            // Build hub after settings are loaded so quality can be applied
            SpaceHubBuilder.BuildHub(_config, _globalSettingsService);

            _hostService = new GameProcessHostService(_config, _globalSettingsService);
            _hudOverlay = gameObject.AddComponent<SharedHudOverlay>();
            _hudOverlay.Initialize(_config, _apiClient, _globalSettingsService, this);
            _questTrackerWidget = gameObject.AddComponent<QuestTrackerWidget>();
            _questTrackerWidget.Initialize(_apiClient, _globalSettingsService, this);

            var preloadResult = await _hostService.PreloadAllAsync();
            if (preloadResult.IsError)
            {
                Debug.LogError(preloadResult.Message);
            }
            else
            {
                Debug.Log(preloadResult.Message);
            }
        }

        private void Update()
        {
            if (_hostService != null)
            {
                _hostService.TickMaintenance();
            }

            if (Input.GetKeyDown(KeyCode.F1) && _hostService != null)
            {
                _hostService.HideAllGames();
            }

            if (_hudOverlay != null && _hostService != null)
            {
                _hudOverlay.UpdateReturnToHubButtonVisibility(_hostService.HasActiveGame());
            }

            TickTeleportIpc();
        }

        [System.Serializable]
        private class TeleportRequest
        {
            public string targetGame;
            public string targetMap;
            public float x;
            public float y;
            public float z;
        }

        private async void TickTeleportIpc()
        {
            if (!_isInitialized || _hostService == null || _config == null || string.IsNullOrWhiteSpace(_config.avatarId))
            {
                return;
            }

            if (Time.unscaledTime - _lastTeleportPollTime < 0.5f)
            {
                return;
            }

            _lastTeleportPollTime = Time.unscaledTime;

            var path = Path.Combine(Path.GetTempPath(), $"oasis_teleport_{_config.avatarId}.json");
            if (!File.Exists(path))
            {
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
                File.Delete(path);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OASIS Teleport] Failed to read teleport IPC file: {ex.Message}");
                return;
            }

            TeleportRequest request;
            try
            {
                request = JsonUtility.FromJson<TeleportRequest>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OASIS Teleport] Failed to parse teleport request: {ex.Message}");
                return;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.targetGame))
            {
                Debug.LogWarning("[OASIS Teleport] Received empty or invalid teleport request.");
                return;
            }

            Debug.Log($"[OASIS Teleport] {_config.avatarId} → {request.targetGame}:{request.targetMap}");

            if (_hudOverlay != null)
            {
                _hudOverlay.ShowToast($"Teleporting to {request.targetGame}...");
            }
            else
            {
                Debug.Log($"[OASIS Teleport] Teleporting to {request.targetGame}...");
            }

            var result = await _hostService.ActivateGameAsync(request.targetGame);
            if (result.IsError)
            {
                Debug.LogError($"[OASIS Teleport] ActivateGameAsync failed for '{request.targetGame}': {result.Message}");
                return;
            }

            var arrivePath = Path.Combine(Path.GetTempPath(), $"oasis_teleport_arrive_{_config.avatarId}.json");
            var arriveJson = $"{{\"map\":\"{request.targetMap}\",\"x\":{request.x},\"y\":{request.y},\"z\":{request.z}}}";
            try
            {
                File.WriteAllText(arrivePath, arriveJson);
                Debug.Log($"[OASIS Teleport] Arrive file written for {request.targetGame} → map '{request.targetMap}'.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OASIS Teleport] Failed to write arrive file: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task<OASISResult<bool>> EnterPortalAsync(string gameId, int? targetLevel)
        {
            if (_hostService == null)
            {
                return OASISResult<bool>.Error("Kernel host service is not initialized.");
            }

            if (string.IsNullOrWhiteSpace(gameId))
            {
                return OASISResult<bool>.Error("Game id is required.");
            }

            return await _hostService.ActivateGameAsync(gameId);
        }

        public OASISResult<bool> HideHostedGames()
        {
            if (_hostService == null)
            {
                return OASISResult<bool>.Error("Kernel host service is not initialized.");
            }

            return _hostService.HideAllGames();
        }

        public async System.Threading.Tasks.Task<OASISResult<bool>> ApplyGlobalSettingsAndRebuildSessionsAsync(OmniverseGlobalSettings settings)
        {
            if (_hostService == null || _globalSettingsService == null || _apiClient == null)
            {
                return OASISResult<bool>.Error("Kernel services are not initialized.");
            }

            var save = await _globalSettingsService.SaveAndApplyAsync(settings, _apiClient);
            if (save.IsError)
            {
                return save;
            }

            // Update portals with new graphics quality
            UpdatePortalsQuality(settings.graphicsPreset);

            var rebuild = await _hostService.RebuildSessionsForUpdatedSettingsAsync();
            if (rebuild.IsError)
            {
                return rebuild;
            }

            return OASISResult<bool>.Success(true, "Global settings applied and hosted game sessions rebuilt.");
        }

        private void UpdatePortalsQuality(string graphicsPreset)
        {
            if (_config == null) return;

            var qualityLevel = PortalQualityManager.ParseQualityLevel(graphicsPreset);

            foreach (var game in _config.games)
            {
                var portalName = $"Portal_{game.displayName}";
                var portalRoot = GameObject.Find(portalName);
                if (portalRoot != null)
                {
                    var portalColor = new Color(
                        Mathf.Clamp01(game.portalColorR),
                        Mathf.Clamp01(game.portalColorG),
                        Mathf.Clamp01(game.portalColorB)
                    );
                    PortalQualityManager.ApplyQualityToPortal(portalRoot, portalColor, qualityLevel);
                }
            }
        }

        public async System.Threading.Tasks.Task<OASISResult<bool>> SaveUiPreferencesAsync(OmniverseGlobalSettings settings)
        {
            if (_globalSettingsService == null || _apiClient == null)
            {
                return OASISResult<bool>.Error("Kernel services are not initialized.");
            }

            return await _globalSettingsService.SavePreferencesOnlyAsync(settings, _apiClient);
        }

        public OmniverseRuntimeHealthSnapshot GetRuntimeHealthSnapshot()
        {
            return new OmniverseRuntimeHealthSnapshot
            {
                api = _apiClient?.GetHealthSnapshot() ?? new GatewayHealthSnapshot(),
                host = _hostService?.GetHealthSnapshot() ?? new HostRuntimeHealthSnapshot()
            };
        }

        public OASISResult<OmniversePanelLayout> GetQuestTrackerLayout()
        {
            if (_questTrackerWidget == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker is not initialized.");
            }

            return _questTrackerWidget.GetCurrentLayout();
        }

        public OASISResult<OmniversePanelLayout> ApplyQuestTrackerLayoutPreset(string presetName)
        {
            if (_questTrackerWidget == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker is not initialized.");
            }

            return _questTrackerWidget.ApplyLayoutPreset(presetName);
        }

        public async System.Threading.Tasks.Task<OASISResult<OmniversePanelLayout>> ApplyQuestTrackerLayoutPresetAnimatedAsync(string presetName, float durationSeconds = 0.22f)
        {
            if (_questTrackerWidget == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker is not initialized.");
            }

            return await _questTrackerWidget.ApplyLayoutPresetAnimatedAsync(presetName, durationSeconds);
        }

        public OASISResult<OmniversePanelLayout> ResetQuestTrackerLayoutToDefault()
        {
            if (_questTrackerWidget == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker is not initialized.");
            }

            return _questTrackerWidget.ResetLayoutToDefault();
        }

        public async System.Threading.Tasks.Task<OASISResult<OmniversePanelLayout>> ResetQuestTrackerLayoutToDefaultAnimatedAsync(float durationSeconds = 0.22f)
        {
            if (_questTrackerWidget == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker is not initialized.");
            }

            return await _questTrackerWidget.ResetLayoutToDefaultAnimatedAsync(durationSeconds);
        }

        private void OnDestroy()
        {
            _apiClient?.Dispose();
        }
    }
}

