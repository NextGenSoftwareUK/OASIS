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

        // ── CHAPTERS ────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_chapter_load"), Description("WEB5 STARNET: loads a single quest chapter by its GUID id.")]
        public static async Task<string> ChapterLoad(string avatarId, string chapterId)
        {
            ChapterManager manager = new ChapterManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(chapterId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_chapter_load_all_for_avatar"), Description("WEB5 STARNET: loads every chapter published by or accessible to an avatar.")]
        public static async Task<string> ChapterLoadAllForAvatar(string avatarId)
        {
            ChapterManager manager = new ChapterManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_chapter_search"), Description("WEB5 STARNET: searches chapters on STARNET by a free-text search term.")]
        public static async Task<string> ChapterSearch(string avatarId, string searchTerm)
        {
            ChapterManager manager = new ChapterManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_chapter_download"), Description("WEB5 STARNET: downloads a chapter from STARNET to a local path.")]
        public static async Task<string> ChapterDownload(string avatarId, string chapterId, int version, string fullDownloadPath = "")
        {
            ChapterManager manager = new ChapterManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(chapterId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── GEO HOTSPOTS ────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_geo_hotspot_load"), Description("WEB5 STARNET: loads a single geo hotspot by its GUID id.")]
        public static async Task<string> GeoHotSpotLoad(string avatarId, string hotspotId)
        {
            GeoHotSpotManager manager = new GeoHotSpotManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(hotspotId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_geo_hotspot_load_all_for_avatar"), Description("WEB5 STARNET: loads every geo hotspot published by or accessible to an avatar.")]
        public static async Task<string> GeoHotSpotLoadAllForAvatar(string avatarId)
        {
            GeoHotSpotManager manager = new GeoHotSpotManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_geo_hotspot_search"), Description("WEB5 STARNET: searches geo hotspots on STARNET by a free-text search term.")]
        public static async Task<string> GeoHotSpotSearch(string avatarId, string searchTerm)
        {
            GeoHotSpotManager manager = new GeoHotSpotManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_geo_hotspot_download"), Description("WEB5 STARNET: downloads a geo hotspot from STARNET to a local path.")]
        public static async Task<string> GeoHotSpotDownload(string avatarId, string hotspotId, int version, string fullDownloadPath = "")
        {
            GeoHotSpotManager manager = new GeoHotSpotManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(hotspotId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── INVENTORY ITEMS ──────────────────────────────────────────────────

        [McpServerTool(Name = "web5_inventory_item_load"), Description("WEB5 STARNET: loads a single inventory item by its GUID id.")]
        public static async Task<string> InventoryItemLoad(string avatarId, string itemId)
        {
            InventoryItemManager manager = new InventoryItemManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(itemId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_inventory_item_load_all_for_avatar"), Description("WEB5 STARNET: loads every inventory item owned by or accessible to an avatar.")]
        public static async Task<string> InventoryItemLoadAllForAvatar(string avatarId)
        {
            InventoryItemManager manager = new InventoryItemManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_inventory_item_search"), Description("WEB5 STARNET: searches inventory items on STARNET by a free-text search term.")]
        public static async Task<string> InventoryItemSearch(string avatarId, string searchTerm)
        {
            InventoryItemManager manager = new InventoryItemManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_inventory_item_download"), Description("WEB5 STARNET: downloads an inventory item definition from STARNET to a local path.")]
        public static async Task<string> InventoryItemDownload(string avatarId, string itemId, int version, string fullDownloadPath = "")
        {
            InventoryItemManager manager = new InventoryItemManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(itemId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── LIBRARIES ────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_library_load"), Description("WEB5 STARNET: loads a single OASIS library by its GUID id.")]
        public static async Task<string> LibraryLoad(string avatarId, string libraryId)
        {
            LibraryManager manager = new LibraryManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(libraryId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_library_load_all_for_avatar"), Description("WEB5 STARNET: loads every library published by or accessible to an avatar.")]
        public static async Task<string> LibraryLoadAllForAvatar(string avatarId)
        {
            LibraryManager manager = new LibraryManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_library_search"), Description("WEB5 STARNET: searches libraries on STARNET by a free-text search term.")]
        public static async Task<string> LibrarySearch(string avatarId, string searchTerm)
        {
            LibraryManager manager = new LibraryManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_library_download"), Description("WEB5 STARNET: downloads a library from STARNET to a local path.")]
        public static async Task<string> LibraryDownload(string avatarId, string libraryId, int version, string fullDownloadPath = "")
        {
            LibraryManager manager = new LibraryManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(libraryId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_library_download_and_install"), Description("WEB5 STARNET: downloads and installs a library from STARNET in one step.")]
        public static async Task<string> LibraryDownloadAndInstall(string avatarId, string libraryId, int version, string fullInstallPath, string fullDownloadPath = "")
        {
            LibraryManager manager = new LibraryManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAndInstallAsync(Guid.Parse(avatarId), Guid.Parse(libraryId), version, fullInstallPath, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── OAPP TEMPLATES ───────────────────────────────────────────────────

        [McpServerTool(Name = "web5_oapp_template_load"), Description("WEB5 STARNET: loads a single OAPP template by its GUID id.")]
        public static async Task<string> OAPPTemplateLoad(string avatarId, string templateId)
        {
            OAPPTemplateManager manager = new OAPPTemplateManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(templateId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_template_load_all_for_avatar"), Description("WEB5 STARNET: loads every OAPP template published by or accessible to an avatar.")]
        public static async Task<string> OAPPTemplateLoadAllForAvatar(string avatarId)
        {
            OAPPTemplateManager manager = new OAPPTemplateManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_template_search"), Description("WEB5 STARNET: searches OAPP templates on STARNET by a free-text search term.")]
        public static async Task<string> OAPPTemplateSearch(string avatarId, string searchTerm)
        {
            OAPPTemplateManager manager = new OAPPTemplateManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_oapp_template_download"), Description("WEB5 STARNET: downloads an OAPP template from STARNET to a local path.")]
        public static async Task<string> OAPPTemplateDownload(string avatarId, string templateId, int version, string fullDownloadPath = "")
        {
            OAPPTemplateManager manager = new OAPPTemplateManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(templateId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── RUNTIMES ─────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_runtime_load"), Description("WEB5 STARNET: loads a single OASIS runtime by its GUID id.")]
        public static async Task<string> RuntimeLoad(string avatarId, string runtimeId)
        {
            RuntimeManager manager = new RuntimeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(runtimeId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_runtime_load_all_for_avatar"), Description("WEB5 STARNET: loads every runtime published by or accessible to an avatar.")]
        public static async Task<string> RuntimeLoadAllForAvatar(string avatarId)
        {
            RuntimeManager manager = new RuntimeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_runtime_search"), Description("WEB5 STARNET: searches runtimes on STARNET by a free-text search term.")]
        public static async Task<string> RuntimeSearch(string avatarId, string searchTerm)
        {
            RuntimeManager manager = new RuntimeManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_runtime_download"), Description("WEB5 STARNET: downloads a runtime from STARNET to a local path.")]
        public static async Task<string> RuntimeDownload(string avatarId, string runtimeId, int version, string fullDownloadPath = "")
        {
            RuntimeManager manager = new RuntimeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(runtimeId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_runtime_download_and_install"), Description("WEB5 STARNET: downloads and installs a runtime from STARNET in one step.")]
        public static async Task<string> RuntimeDownloadAndInstall(string avatarId, string runtimeId, int version, string fullInstallPath, string fullDownloadPath = "")
        {
            RuntimeManager manager = new RuntimeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAndInstallAsync(Guid.Parse(avatarId), Guid.Parse(runtimeId), version, fullInstallPath, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── PLUGINS ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_plugin_load"), Description("WEB5 STARNET: loads a single OASIS plugin by its GUID id.")]
        public static async Task<string> PluginLoad(string avatarId, string pluginId)
        {
            PluginManager manager = new PluginManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(pluginId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_plugin_load_all_for_avatar"), Description("WEB5 STARNET: loads every plugin published by or accessible to an avatar.")]
        public static async Task<string> PluginLoadAllForAvatar(string avatarId)
        {
            PluginManager manager = new PluginManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_plugin_search"), Description("WEB5 STARNET: searches plugins on STARNET by a free-text search term.")]
        public static async Task<string> PluginSearch(string avatarId, string searchTerm)
        {
            PluginManager manager = new PluginManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_plugin_download_and_install"), Description("WEB5 STARNET: downloads and installs a plugin from STARNET in one step.")]
        public static async Task<string> PluginDownloadAndInstall(string avatarId, string pluginId, int version, string fullInstallPath, string fullDownloadPath = "")
        {
            PluginManager manager = new PluginManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAndInstallAsync(Guid.Parse(avatarId), Guid.Parse(pluginId), version, fullInstallPath, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_plugin_activate"), Description("WEB5 STARNET: activates an installed plugin for an avatar.")]
        public static async Task<string> PluginActivate(string avatarId, string pluginId, int version)
        {
            PluginManager manager = new PluginManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.ActivateAsync(Guid.Parse(avatarId), Guid.Parse(pluginId), version);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_plugin_deactivate"), Description("WEB5 STARNET: deactivates a running plugin for an avatar.")]
        public static async Task<string> PluginDeactivate(string avatarId, string pluginId, int version)
        {
            PluginManager manager = new PluginManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DeactivateAsync(Guid.Parse(avatarId), Guid.Parse(pluginId), version);
            return JsonSerializer.Serialize(result);
        }

        // ── CELESTIAL SPACES ─────────────────────────────────────────────────

        [McpServerTool(Name = "web5_celestial_space_load"), Description("WEB5 STARNET: loads a single celestial space (universe, multiverse, dimension, etc.) by its GUID id.")]
        public static async Task<string> CelestialSpaceLoad(string avatarId, string celestialSpaceId)
        {
            CelestialSpaceManager manager = new CelestialSpaceManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(celestialSpaceId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_celestial_space_load_all_for_avatar"), Description("WEB5 STARNET: loads every celestial space published by or visible to an avatar.")]
        public static async Task<string> CelestialSpaceLoadAllForAvatar(string avatarId)
        {
            CelestialSpaceManager manager = new CelestialSpaceManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_celestial_space_search"), Description("WEB5 STARNET: searches celestial spaces by a free-text search term.")]
        public static async Task<string> CelestialSpaceSearch(string avatarId, string searchTerm)
        {
            CelestialSpaceManager manager = new CelestialSpaceManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_celestial_space_download"), Description("WEB5 STARNET: downloads a celestial space package from STARNET to a local path.")]
        public static async Task<string> CelestialSpaceDownload(string avatarId, string celestialSpaceId, int version, string fullDownloadPath = "")
        {
            CelestialSpaceManager manager = new CelestialSpaceManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(celestialSpaceId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── STAR GEO NFTS ────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_star_geo_nft_load"), Description("WEB5 STARNET: loads a single STAR geo-spatial NFT by its GUID id.")]
        public static async Task<string> STARGeoNFTLoad(string avatarId, string nftId)
        {
            STARGeoNFTManager manager = new STARGeoNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(nftId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_load_all_for_avatar"), Description("WEB5 STARNET: loads every STAR geo-spatial NFT published by or owned by an avatar.")]
        public static async Task<string> STARGeoNFTLoadAllForAvatar(string avatarId)
        {
            STARGeoNFTManager manager = new STARGeoNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_search"), Description("WEB5 STARNET: searches STAR geo-spatial NFTs on STARNET by a free-text search term.")]
        public static async Task<string> STARGeoNFTSearch(string avatarId, string searchTerm)
        {
            STARGeoNFTManager manager = new STARGeoNFTManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_download"), Description("WEB5 STARNET: downloads a STAR geo-spatial NFT from STARNET to a local path.")]
        public static async Task<string> STARGeoNFTDownload(string avatarId, string nftId, int version, string fullDownloadPath = "")
        {
            STARGeoNFTManager manager = new STARGeoNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(nftId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_delete"), Description("WEB5 STARNET: soft-deletes a STAR geo-spatial NFT by id (set softDelete=false to hard delete).")]
        public static async Task<string> STARGeoNFTDelete(string avatarId, string nftId, int version, bool softDelete = true)
        {
            STARGeoNFTManager manager = new STARGeoNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DeleteAsync(Guid.Parse(avatarId), Guid.Parse(nftId), version, softDelete);
            return JsonSerializer.Serialize(result);
        }

        // ── STAR GEO NFT COLLECTIONS ─────────────────────────────────────────

        [McpServerTool(Name = "web5_star_geo_nft_collection_load"), Description("WEB5 STARNET: loads a single STAR geo-spatial NFT collection by its GUID id.")]
        public static async Task<string> STARGeoNFTCollectionLoad(string avatarId, string collectionId)
        {
            STARGeoNFTCollectionManager manager = new STARGeoNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(collectionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_collection_load_all_for_avatar"), Description("WEB5 STARNET: loads every STAR geo-spatial NFT collection owned by an avatar.")]
        public static async Task<string> STARGeoNFTCollectionLoadAllForAvatar(string avatarId)
        {
            STARGeoNFTCollectionManager manager = new STARGeoNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_collection_search"), Description("WEB5 STARNET: searches STAR geo-spatial NFT collections on STARNET by a free-text search term.")]
        public static async Task<string> STARGeoNFTCollectionSearch(string avatarId, string searchTerm)
        {
            STARGeoNFTCollectionManager manager = new STARGeoNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_geo_nft_collection_download"), Description("WEB5 STARNET: downloads a STAR geo-spatial NFT collection from STARNET to a local path.")]
        public static async Task<string> STARGeoNFTCollectionDownload(string avatarId, string collectionId, int version, string fullDownloadPath = "")
        {
            STARGeoNFTCollectionManager manager = new STARGeoNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(collectionId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── STAR NFTS ────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_star_nft_load"), Description("WEB5 STARNET: loads a single STAR NFT by its GUID id.")]
        public static async Task<string> STARNFTLoad(string avatarId, string nftId)
        {
            STARNFTManager manager = new STARNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(nftId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_load_all_for_avatar"), Description("WEB5 STARNET: loads every STAR NFT published by or owned by an avatar.")]
        public static async Task<string> STARNFTLoadAllForAvatar(string avatarId)
        {
            STARNFTManager manager = new STARNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_search"), Description("WEB5 STARNET: searches STAR NFTs on STARNET by a free-text search term.")]
        public static async Task<string> STARNFTSearch(string avatarId, string searchTerm)
        {
            STARNFTManager manager = new STARNFTManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_download"), Description("WEB5 STARNET: downloads a STAR NFT from STARNET to a local path.")]
        public static async Task<string> STARNFTDownload(string avatarId, string nftId, int version, string fullDownloadPath = "")
        {
            STARNFTManager manager = new STARNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(nftId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_delete"), Description("WEB5 STARNET: soft-deletes a STAR NFT by id (set softDelete=false to hard delete).")]
        public static async Task<string> STARNFTDelete(string avatarId, string nftId, int version, bool softDelete = true)
        {
            STARNFTManager manager = new STARNFTManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DeleteAsync(Guid.Parse(avatarId), Guid.Parse(nftId), version, softDelete);
            return JsonSerializer.Serialize(result);
        }

        // ── STAR NFT COLLECTIONS ─────────────────────────────────────────────

        [McpServerTool(Name = "web5_star_nft_collection_load"), Description("WEB5 STARNET: loads a single STAR NFT collection by its GUID id.")]
        public static async Task<string> STARNFTCollectionLoad(string avatarId, string collectionId)
        {
            STARNFTCollectionManager manager = new STARNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(collectionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_collection_load_all_for_avatar"), Description("WEB5 STARNET: loads every STAR NFT collection owned by an avatar.")]
        public static async Task<string> STARNFTCollectionLoadAllForAvatar(string avatarId)
        {
            STARNFTCollectionManager manager = new STARNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_collection_search"), Description("WEB5 STARNET: searches STAR NFT collections on STARNET by a free-text search term.")]
        public static async Task<string> STARNFTCollectionSearch(string avatarId, string searchTerm)
        {
            STARNFTCollectionManager manager = new STARNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_nft_collection_download"), Description("WEB5 STARNET: downloads a STAR NFT collection from STARNET to a local path.")]
        public static async Task<string> STARNFTCollectionDownload(string avatarId, string collectionId, int version, string fullDownloadPath = "")
        {
            STARNFTCollectionManager manager = new STARNFTCollectionManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(collectionId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── STAR ZOMES ───────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_star_zome_load"), Description("WEB5 STARNET: loads a single STAR Zome by its GUID id. Zomes are the callable module containers inside Holochain OAPPs.")]
        public static async Task<string> STARZomeLoad(string avatarId, string zomeId)
        {
            STARZomeManager manager = new STARZomeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(zomeId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_zome_load_all_for_avatar"), Description("WEB5 STARNET: loads every STAR Zome published by or accessible to an avatar.")]
        public static async Task<string> STARZomeLoadAllForAvatar(string avatarId)
        {
            STARZomeManager manager = new STARZomeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_zome_search"), Description("WEB5 STARNET: searches STAR Zomes on STARNET by a free-text search term.")]
        public static async Task<string> STARZomeSearch(string avatarId, string searchTerm)
        {
            STARZomeManager manager = new STARZomeManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_star_zome_download"), Description("WEB5 STARNET: downloads a STAR Zome from STARNET to a local path.")]
        public static async Task<string> STARZomeDownload(string avatarId, string zomeId, int version, string fullDownloadPath = "")
        {
            STARZomeManager manager = new STARZomeManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.DownloadAsync(Guid.Parse(avatarId), Guid.Parse(zomeId), version, fullDownloadPath);
            return JsonSerializer.Serialize(result);
        }

        // ── GAME ─────────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_game_load"), Description("WEB5 STARNET: loads a single game by its GUID id.")]
        public static async Task<string> GameLoad(string avatarId, string gameId)
        {
            GameManager manager = new GameManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAsync(Guid.Parse(avatarId), Guid.Parse(gameId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_game_load_all_for_avatar"), Description("WEB5 STARNET: loads every game published by or accessible to an avatar.")]
        public static async Task<string> GameLoadAllForAvatar(string avatarId)
        {
            GameManager manager = new GameManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_game_search"), Description("WEB5 STARNET: searches games on STARNET by a free-text search term.")]
        public static async Task<string> GameSearch(string avatarId, string searchTerm)
        {
            GameManager manager = new GameManager(Guid.Parse(avatarId), _starDna);
            var result = manager.Search(Guid.Parse(avatarId), searchTerm);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_game_get_shared_assets"), Description("WEB5 STARNET: gets inventory items shared across all games for an avatar.")]
        public static async Task<string> GameGetSharedAssets(string avatarId)
        {
            GameManager manager = new GameManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.GetSharedAssetsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_game_get_cross_game_quests"), Description("WEB5 STARNET: gets quests that span multiple games for an avatar.")]
        public static async Task<string> GameGetCrossGameQuests(string avatarId)
        {
            GameManager manager = new GameManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.GetCrossGameQuestsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web5_game_get_avatar_karma"), Description("WEB5 STARNET: gets the total karma accumulated by an avatar across all games.")]
        public static async Task<string> GameGetAvatarKarma(string avatarId)
        {
            GameManager manager = new GameManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.GetAvatarKarmaAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }
    }
}
