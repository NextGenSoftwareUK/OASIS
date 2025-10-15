using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class MissionManager : QuestManagerBase<Mission, DownloadedMission, InstalledMission, MissionDNA>
    public class MissionManager : QuestManagerBase<Mission, DownloadedMission, InstalledMission, STARNETDNA>
    {
        public MissionManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(MissionType),
            HolonType.Mission,
            HolonType.InstalledMission,
            "Mission",
            //"MissionId",
            "STARNETHolonId",
            "MissionName",
            "MissionType",
            "omission",
            "oasis_missions",
            "MissionDNA.json",
            "MissionDNAJSON")
        { }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(MissionType),
            HolonType.Mission,
            HolonType.InstalledMission,
            "Mission",
            //"MissionId",
            "STARNETHolonId",
            "MissionName",
            "MissionType",
            "omission",
            "oasis_missions",
            "MissionDNA.json",
            "MissionDNAJSON")
        { }

        /// <summary>
        /// Completes a mission for the specified avatar
        /// </summary>
        /// <param name="avatarId">The avatar completing the mission</param>
        /// <param name="missionId">The mission to complete</param>
        /// <param name="completionNotes">Optional completion notes</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> CompleteMissionAsync(Guid avatarId, Guid missionId, string completionNotes = null)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occurred in MissionManager.CompleteMissionAsync. Reason:";

            try
            {
                // Load the mission
                var missionResult = await LoadAsync(avatarId, missionId);
                if (missionResult.IsError || missionResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mission not found or could not be loaded. Reason: {missionResult.Message}");
                    return result;
                }

                // Update mission status to completed
                missionResult.Result.Status = MissionStatus.Completed;
                missionResult.Result.CompletedOn = DateTime.UtcNow;
                missionResult.Result.CompletedBy = avatarId;
                if (!string.IsNullOrEmpty(completionNotes))
                {
                    missionResult.Result.CompletionNotes = completionNotes;
                }

                // Save the updated mission
                var updateResult = await UpdateAsync(avatarId, missionResult.Result);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to save completed mission. Reason: {updateResult.Message}");
                    return result;
                }

                result.Result = true;
                result.Message = "Mission completed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets mission leaderboard for a specific mission
        /// </summary>
        /// <param name="missionId">The mission ID</param>
        /// <param name="limit">Number of entries to return</param>
        /// <returns>Mission leaderboard entries</returns>
        public async Task<OASISResult<List<MissionLeaderboard>>> GetMissionLeaderboardAsync(Guid missionId, int limit = 50)
        {
            OASISResult<List<MissionLeaderboard>> result = new OASISResult<List<MissionLeaderboard>>();
            string errorMessage = "Error occurred in MissionManager.GetMissionLeaderboardAsync. Reason:";

            try
            {
                // TODO: Implement actual leaderboard logic
                // This would typically query completed missions and rank by completion time, score, etc.
                var leaderboard = new List<MissionLeaderboard>();
                
                result.Result = leaderboard;
                result.Message = "Mission leaderboard retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets mission rewards for a specific mission
        /// </summary>
        /// <param name="missionId">The mission ID</param>
        /// <returns>Mission rewards</returns>
        public async Task<OASISResult<List<MissionReward>>> GetMissionRewardsAsync(Guid missionId)
        {
            OASISResult<List<MissionReward>> result = new OASISResult<List<MissionReward>>();
            string errorMessage = "Error occurred in MissionManager.GetMissionRewardsAsync. Reason:";

            try
            {
                // Load the mission to get its rewards
                var missionResult = await LoadAsync(AvatarId, missionId);
                if (missionResult.IsError || missionResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mission not found. Reason: {missionResult.Message}");
                    return result;
                }

                // TODO: Implement actual rewards logic
                // This would typically extract rewards from mission metadata or configuration
                var rewards = new List<MissionReward>();
                
                result.Result = rewards;
                result.Message = "Mission rewards retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets mission statistics for a specific avatar
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Mission statistics</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetMissionStatsAsync(Guid avatarId)
        {
            OASISResult<Dictionary<string, object>> result = new OASISResult<Dictionary<string, object>>();
            string errorMessage = "Error occurred in MissionManager.GetMissionStatsAsync. Reason:";

            try
            {
                // Load all missions for the avatar
                var missionsResult = await LoadAllForAvatarAsync(avatarId);
                if (missionsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to load avatar missions. Reason: {missionsResult.Message}");
                    return result;
                }

                var missions = missionsResult.Result?.ToList() ?? new List<IMission>();
                
                var stats = new Dictionary<string, object>
                {
                    ["totalMissions"] = missions.Count,
                    ["completedMissions"] = missions.Count(m => m.Status == MissionStatus.Completed),
                    ["activeMissions"] = missions.Count(m => m.Status == MissionStatus.Active),
                    ["pendingMissions"] = missions.Count(m => m.Status == MissionStatus.Pending),
                    ["totalRewards"] = missions.Where(m => m.Status == MissionStatus.Completed).Sum(m => m.Rewards?.Sum(r => r.Amount) ?? 0)
                };

                result.Result = stats;
                result.Message = "Mission statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occurred. Reason: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Mission leaderboard entry
    /// </summary>
    public class MissionLeaderboard
    {
        public Guid AvatarId { get; set; }
        public string AvatarName { get; set; }
        public int Score { get; set; }
        public DateTime CompletedAt { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Mission reward
    /// </summary>
    public class MissionReward
    {
        public string Type { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
    }
}