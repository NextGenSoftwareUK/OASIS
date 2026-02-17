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
        public string gameSource;
        public string priority;
        public float progress;
        public int objectivesCompleted;
        public int objectivesTotal;
    }

    [Serializable]
    public class NftAssetItem
    {
        public string id;
        public string name;
        public string description;
        public string type;
        public string source;
    }

    [Serializable]
    public class AvatarProfileItem
    {
        public string id;
        public string username;
        public string email;
        public string firstName;
        public string lastName;
        public string title;
    }

    [Serializable]
    public class KarmaEntry
    {
        public string id;
        public string source;
        public string reason;
        public float amount;
        public string karmaType;
        public string createdDate;
    }

    [Serializable]
    public class KarmaOverview
    {
        public float totalKarma;
        public List<KarmaEntry> history = new List<KarmaEntry>();
    }

    [Serializable]
    public class OmniverseGlobalSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float soundVolume = 0.8f;
        public float voiceVolume = 0.8f;
        public string graphicsPreset = "High";
        public bool fullscreen = false;
        public string resolution = "1920x1080";
        public string keyOpenControlCenter = "I";
        public string keyHideHostedGame = "F1";
        public int toastMaxVisible = 3;
        public float toastDurationSeconds = 1.7f;
        public float uiFontScale = 1f;
        public bool uiHighContrast = false;
        public bool showStatusStrip = true;
        public List<OmniverseViewPreset> viewPresets = new List<OmniverseViewPreset>();
        public List<OmniverseActiveViewPreset> activeViewPresets = new List<OmniverseActiveViewPreset>();
        public List<OmniversePanelLayout> panelLayouts = new List<OmniversePanelLayout>();
    }

    [Serializable]
    public class OmniverseViewPreset
    {
        public string name;
        public string tab;
        public string searchQuery;
        public string sortField;
        public bool sortAscending = true;
    }

    [Serializable]
    public class OmniverseActiveViewPreset
    {
        public string tab;
        public string presetName;
    }

    [Serializable]
    public class OmniversePanelLayout
    {
        public string panelId;
        public float anchoredX;
        public float anchoredY;
        public float width;
        public float height;
    }
}

