using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using OASIS.Omniverse.UnityHost.UI;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public class OmniverseKernel : MonoBehaviour
    {
        public static OmniverseKernel Instance { get; private set; }

        private OmniverseHostConfig _config;
        private GameProcessHostService _hostService;
        private SharedHudOverlay _hudOverlay;

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

            _hostService = new GameProcessHostService(_config);
            _hudOverlay = gameObject.AddComponent<SharedHudOverlay>();
            _hudOverlay.Initialize(_config);

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

            return await _hostService.ActivateGameAsync(gameId);
        }
    }
}

