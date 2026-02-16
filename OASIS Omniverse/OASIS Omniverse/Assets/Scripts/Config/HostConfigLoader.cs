using System.IO;
using Newtonsoft.Json;
using OASIS.Omniverse.UnityHost.Core;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Config
{
    public static class HostConfigLoader
    {
        private const string ConfigName = "omniverse_host_config.json";

        public static OASISResult<OmniverseHostConfig> Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, ConfigName);
            if (!File.Exists(path))
            {
                return OASISResult<OmniverseHostConfig>.Error($"Config file not found: {path}");
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<OmniverseHostConfig>(json);
                if (config == null)
                {
                    return OASISResult<OmniverseHostConfig>.Error("Config deserialized to null.");
                }

                if (config.games == null || config.games.Count == 0)
                {
                    return OASISResult<OmniverseHostConfig>.Error("Config has no games configured.");
                }

                return OASISResult<OmniverseHostConfig>.Success(config);
            }
            catch (System.Exception ex)
            {
                return OASISResult<OmniverseHostConfig>.Error($"Failed loading config: {ex.Message}");
            }
        }
    }
}

