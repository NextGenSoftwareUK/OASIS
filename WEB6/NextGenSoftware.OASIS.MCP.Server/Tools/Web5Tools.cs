using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB5 (STAR API/STARNET) typed in-process MCP tools covering Quest/Mission completion tracking,
    /// OAPP lifecycle (load/search/download/install/activate/deactivate), CelestialBody/Space browsing,
    /// and STARNET holon publish/download/install — all verified against the ISTARNETManagerBase interface
    /// implemented by STARNETManagerBase (the shared generic base for every STARNET manager).
    /// No generic HTTP passthrough: every operation is named and typed so the agent knows exactly what's available.
    /// </summary>
    [McpServerToolType]
    public static class Web5Tools
    {
        private static readonly STARDNA _starDna = new STARDNA();

        // ── QUESTS ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_quest_load"), Description("WEB5: loads a single quest by its GUID id.")]
        public static async Task<string> QuestLoad(string avatarId, string questId)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(questId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_quest_load_all_for_avatar"), Description("WEB5: loads every quest available to/started by an avatar.")]
        public static async Task<string> QuestLoadAllForAvatar(string avatarId)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllQuestsForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_quest_search"), Description("WEB5: searches quests on STARNET by a free-text search term.")]
        public static async Task<string> QuestSearch(string avatarId, string searchTerm)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_quest_start"), Description("WEB5: starts a quest for an avatar.")]
        public static async Task<string> QuestStart(string avatarId, string questId, string? startNotes = null)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.StartQuestAsync(Guid.Parse(avatarId), Guid.Parse(questId), startNotes);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_quest_complete_objective"), Description("WEB5: marks a single objective of a quest complete for an avatar.")]
        public static async Task<string> QuestCompleteObjective(string avatarId, string questId, string objectiveId, string? gameSource = null, string? completionNotes = null)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.CompleteQuestObjectiveAsync(Guid.Parse(avatarId), Guid.Parse(questId), Guid.Parse(objectiveId), gameSource, completionNotes);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_quest_complete"), Description("WEB5: marks an entire quest complete for an avatar.")]
        public static async Task<string> QuestComplete(string avatarId, string questId, string? completionNotes = null)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.CompleteQuestAsync(Guid.Parse(avatarId), Guid.Parse(questId), completionNotes);
            return JsonSerializer.Serialize(result);
        }

        // ── MISSIONS ────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_mission_load"), Description("WEB5: loads a single mission by its GUID id.")]
        public static async Task<string> MissionLoad(string avatarId, string missionId)
        {
            MissionManager manager = new MissionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(missionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_mission_load_all_for_avatar"), Description("WEB5: loads every mission for an avatar.")]
        public static async Task<string> MissionLoadAllForAvatar(string avatarId)
        {
            MissionManager manager = new MissionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_mission_search"), Description("WEB5: searches missions on STARNET by a free-text search term.")]
        public static async Task<string> MissionSearch(string avatarId, string searchTerm)
        {
            MissionManager manager = new MissionManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_mission_complete"), Description("WEB5: marks a mission complete for an avatar.")]
        public static async Task<string> MissionComplete(string avatarId, string missionId, string? completionNotes = null)
        {
            MissionManager manager = new MissionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.CompleteMissionAsync(Guid.Parse(avatarId), Guid.Parse(missionId), completionNotes);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_mission_get_leaderboard"), Description("WEB5: gets the leaderboard for a mission.")]
        public static async Task<string> MissionGetLeaderboard(string missionId, int limit = 50)
        {
            MissionManager manager = new MissionManager(Guid.Empty, _starDna);
            var result = await manager.GetMissionLeaderboardAsync(Guid.Parse(missionId), limit);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_mission_get_rewards"), Description("WEB5: gets the reward list for a mission.")]
        public static async Task<string> MissionGetRewards(string missionId)
        {
            MissionManager manager = new MissionManager(Guid.Empty, _starDna);
            var result = await manager.GetMissionRewardsAsync(Guid.Parse(missionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_mission_get_stats"), Description("WEB5: gets aggregate mission statistics for an avatar.")]
        public static async Task<string> MissionGetStats(string avatarId)
        {
            MissionManager manager = new MissionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.GetMissionStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        // ── OAPPs ───────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_oapp_load"), Description("WEB5 STARNET: loads a single OAPP by its GUID id.")]
        public static async Task<string> OAPPLoad(string avatarId, string oappId)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(oappId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_load_all_for_avatar"), Description("WEB5 STARNET: loads every OAPP published by or accessible to an avatar.")]
        public static async Task<string> OAPPLoadAllForAvatar(string avatarId)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_list_installed"), Description("WEB5 STARNET: lists every OAPP currently installed for an avatar.")]
        public static async Task<string> OAPPListInstalled(string avatarId)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.ListInstalledAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_search"), Description("WEB5 STARNET: searches OAPPs on STARNET by a free-text search term.")]
        public static async Task<string> OAPPSearch(string avatarId, string searchTerm)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_is_installed"), Description("WEB5 STARNET: checks whether a specific version of an OAPP is installed for an avatar.")]
        public static async Task<string> OAPPIsInstalled(string avatarId, string oappId, int version)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.IsInstalledAsync(Guid.Parse(avatarId), Guid.Parse(oappId), version);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_download"), Description("WEB5 STARNET: downloads an OAPP from STARNET to a local path. Leave fullDownloadPath empty for the default download directory.")]
        public static async Task<string> OAPPDownload(string avatarId, string oappId, int version, string fullDownloadPath = "")
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(oappId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_download_and_install"), Description("WEB5 STARNET: downloads and installs an OAPP from STARNET in one step. fullInstallPath is the local installation directory.")]
        public static async Task<string> OAPPDownloadAndInstall(string avatarId, string oappId, int version, string fullInstallPath, string fullDownloadPath = "")
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAndInstallAsync(Guid.Parse(avatarId), Guid.Parse(oappId), version, fullInstallPath, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_activate"), Description("WEB5 STARNET: activates an installed OAPP for an avatar (makes it the live running version).")]
        public static async Task<string> OAPPActivate(string avatarId, string oappId, int version)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.ActivateAsync(Guid.Parse(avatarId), Guid.Parse(oappId), version);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_deactivate"), Description("WEB5 STARNET: deactivates a running OAPP for an avatar.")]
        public static async Task<string> OAPPDeactivate(string avatarId, string oappId, int version)
        {
            OAPPManager manager = new OAPPManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DeactivateAsync(Guid.Parse(avatarId), Guid.Parse(oappId), version);
            return JsonSerializer.Serialize(result);
        }

        // ── CELESTIAL BODIES ─────────────────────────────────────────────────

        [McpServerTool(Name = "web5_celestial_body_load"), Description("WEB5 STARNET: loads a single celestial body (planet, moon, star, galaxy, dimension, etc.) by its GUID id.")]
        public static async Task<string> CelestialBodyLoad(string avatarId, string celestialBodyId)
        {
            CelestialBodyManager manager = new CelestialBodyManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(celestialBodyId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_celestial_body_load_all_for_avatar"), Description("WEB5 STARNET: loads every celestial body published by or visible to an avatar.")]
        public static async Task<string> CelestialBodyLoadAllForAvatar(string avatarId)
        {
            CelestialBodyManager manager = new CelestialBodyManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_celestial_body_search"), Description("WEB5 STARNET: searches celestial bodies by a free-text search term.")]
        public static async Task<string> CelestialBodySearch(string avatarId, string searchTerm)
        {
            CelestialBodyManager manager = new CelestialBodyManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_celestial_body_download"), Description("WEB5 STARNET: downloads a celestial body package from STARNET to a local path.")]
        public static async Task<string> CelestialBodyDownload(string avatarId, string celestialBodyId, int version, string fullDownloadPath = "")
        {
            CelestialBodyManager manager = new CelestialBodyManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(celestialBodyId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── STARNET HOLONS ───────────────────────────────────────────────────

        [McpServerTool(Name = "web5_holon_load"), Description("WEB5 STARNET: loads a single STARNET holon by its GUID id.")]
        public static async Task<string> StarHolonLoad(string avatarId, string holonId)
        {
            STARHolonManager manager = new STARHolonManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(holonId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_holon_load_all_for_avatar"), Description("WEB5 STARNET: loads every STARNET holon published by or accessible to an avatar.")]
        public static async Task<string> StarHolonLoadAllForAvatar(string avatarId)
        {
            STARHolonManager manager = new STARHolonManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_holon_search"), Description("WEB5 STARNET: searches STARNET holons by a free-text search term.")]
        public static async Task<string> StarHolonSearch(string avatarId, string searchTerm)
        {
            STARHolonManager manager = new STARHolonManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_holon_download"), Description("WEB5 STARNET: downloads a STARNET holon by id and version to a local path.")]
        public static async Task<string> StarHolonDownload(string avatarId, string holonId, int version, string fullDownloadPath = "")
        {
            STARHolonManager manager = new STARHolonManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(holonId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_holon_download_and_install"), Description("WEB5 STARNET: downloads and installs a STARNET holon in one step. fullInstallPath is the local installation directory.")]
        public static async Task<string> StarHolonDownloadAndInstall(string avatarId, string holonId, int version, string fullInstallPath, string fullDownloadPath = "")
        {
            STARHolonManager manager = new STARHolonManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAndInstallAsync(Guid.Parse(avatarId), Guid.Parse(holonId), version, fullInstallPath, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }
    }
}
