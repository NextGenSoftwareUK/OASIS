using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using OASIS.Omniverse.UnityHost.UI;
using UnityEngine;
using OASIS.Omniverse.UnityHost.API;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public class OmniverseKernel : MonoBehaviour
    {
        public static OmniverseKernel Instance { get; private set; }

        private OmniverseHostConfig _config;
        private GameProcessHostService _hostService;
        private SharedHudOverlay _hudOverlay;
        private QuestTrackerWidget _questTrackerWidget;
        private Web4Web5GatewayClient _apiClient;
        private GlobalSettingsService _globalSettingsService;

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
            SpaceHubBuilder.BuildHub(_config);

            _apiClient = new Web4Web5GatewayClient(_config.web4OasisApiBaseUrl, _config.web5StarApiBaseUrl, _config.apiKey, _config.avatarId);
            _globalSettingsService = new GlobalSettingsService();
            await _globalSettingsService.InitializeAsync(_apiClient);

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

            var rebuild = await _hostService.RebuildSessionsForUpdatedSettingsAsync();
            if (rebuild.IsError)
            {
                return rebuild;
            }

            return OASISResult<bool>.Success(true, "Global settings applied and hosted game sessions rebuilt.");
        }

        public async System.Threading.Tasks.Task<OASISResult<bool>> SaveUiPreferencesAsync(OmniverseGlobalSettings settings)
        {
            if (_globalSettingsService == null || _apiClient == null)
            {
                return OASISResult<bool>.Error("Kernel services are not initialized.");
            }

            return await _globalSettingsService.SavePreferencesOnlyAsync(settings, _apiClient);
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

