using System;
using System.Collections.Generic;

namespace OASIS.Omniverse.UnityHost.Config
{
    [Serializable]
    public class OmniverseHostConfig
    {
        public int staleGameMinutes = 10;
        public int maintenancePollSeconds = 20;
        public int lowMemoryAvailableMbThreshold = 2048;
        public string web4OasisApiBaseUrl = "https://oasisweb4.one/api";
        public string web5StarApiBaseUrl = "https://oasisweb4.one/star/api";
        public string avatarId = string.Empty;
        public string apiKey = string.Empty;
        public List<HostedGameDefinition> games = new List<HostedGameDefinition>();
    }

    [Serializable]
    public class HostedGameDefinition
    {
        public string gameId = string.Empty;
        public string displayName = string.Empty;
        public string executableRelativePath = string.Empty;
        public string workingDirectoryRelativePath = string.Empty;
        public string defaultLevelArgument = string.Empty;
        public string baseArguments = string.Empty;
        public float portalX;
        public float portalZ = 12f;
        public float portalColorR = 1f;
        public float portalColorG = 1f;
        public float portalColorB = 1f;
    }

    [Serializable]
    public class InventoryItem
    {
        public string id;
        public string name;
        public string description;
        public string source;
        public string itemType;
    }

    [Serializable]
    public class QuestItem
    {
        public string id;
        public string name;
        public string description;
        public string status;
    }
}

