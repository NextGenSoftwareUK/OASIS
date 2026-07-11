using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB5 (STAR API/STARNET) - specific, typed, in-process MCP tools wrapping the highest-value Quest/Mission/
    /// OAPP manager methods directly (constructed with an explicit avatarId rather than going through STARAPI's
    /// global AvatarManager.LoggedInAvatar "beam in" state, which doesn't fit a stateless per-call MCP tool model).
    /// Everything else on the WEB5 REST API (celestial bodies/spaces, templates, runtimes, libraries, STARNET
    /// holon publish/download/install, GeoNFTs) remains reachable via the web5_request generic passthrough in
    /// Web4Web5Tools.cs.
    /// </summary>
    [McpServerToolType]
    public static class Web5Tools
    {
        private static readonly STARDNA _starDna = new STARDNA();

        // ── QUESTS ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web5_quest_load_all_for_avatar"), Description("WEB5: loads every quest available to/started by an avatar.")]
        public static async Task<string> QuestLoadAllForAvatar(string avatarId)
        {
            QuestManager manager = new QuestManager(Guid.Parse(avatarId), _starDna);
            var result = await manager.LoadAllQuestsForAvatarAsync(Guid.Parse(avatarId));
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

        // ── OAPPS ───────────────────────────────────────────────────────────
        // Note: OAPPManager's Activate/Deactivate/ListInstalled methods are currently commented out in
        // ONODE.Core's source (NextGenSoftware.OASIS.API.ONODE.Core/Managers/STARNET/OAPP System/OAPPManager.cs,
        // lines 592-1018) - not something to silently re-enable here. PublishOAPPAsync is the one OAPP method
        // currently active, but it operates on local file system paths (fullPathToSource) on the machine running
        // this MCP server, which only makes sense if that machine is also the OAPP build host - left to
        // web5_request for now so the caller can supply the right context/paths explicitly via the REST API.

    }
}
