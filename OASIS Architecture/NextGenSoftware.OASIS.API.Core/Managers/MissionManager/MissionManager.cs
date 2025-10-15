using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class MissionManager : OASISManager
    {
        private static MissionManager _instance;
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<Mission>> _avatarMissions;
        private readonly Dictionary<Guid, List<MissionProgress>> _missionProgress;

        public static MissionManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MissionManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _avatarMissions = new Dictionary<Guid, List<Mission>>();
            _missionProgress = new Dictionary<Guid, List<MissionProgress>>();
        }

        public async Task<OASISResult<List<Mission>>> GetAvailableMissionsAsync(Guid avatarId, MissionType? type = null, MissionDifficulty? difficulty = null)
        {
            var result = new OASISResult<List<Mission>>();
            try
            {
                // In a real implementation, this would query the database
                var mockMissions = new List<Mission>
                {
                    new Mission
                    {
                        Id = Guid.NewGuid(),
                        Name = "OASIS Explorer",
                        Description = "Explore 5 different locations in the OASIS",
                        Type = MissionType.Exploration,
                        Difficulty = MissionDifficulty.Easy,
                        RewardKarma = 100,
                        RewardExperience = 50,
                        Requirements = new List<string> { "Visit 5 unique locations" },
                        IsActive = true
                    },
                    new Mission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Social Butterfly",
                        Description = "Send 10 messages to other avatars",
                        Type = MissionType.Social,
                        Difficulty = MissionDifficulty.Medium,
                        RewardKarma = 200,
                        RewardExperience = 100,
                        Requirements = new List<string> { "Send 10 messages" },
                        IsActive = true
                    },
                    new Mission
                    {
                        Id = Guid.NewGuid(),
                        Name = "Egg Collector",
                        Description = "Discover 3 rare eggs",
                        Type = MissionType.Collection,
                        Difficulty = MissionDifficulty.Hard,
                        RewardKarma = 500,
                        RewardExperience = 250,
                        Requirements = new List<string> { "Find 3 rare eggs" },
                        IsActive = true
                    }
                };

                var filteredMissions = mockMissions.AsQueryable();

                if (type.HasValue)
                {
                    filteredMissions = filteredMissions.Where(m => m.Type == type.Value);
                }

                if (difficulty.HasValue)
                {
                    filteredMissions = filteredMissions.Where(m => m.Difficulty == difficulty.Value);
                }

                result.Result = filteredMissions.ToList();
                result.Message = "Available missions retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving available missions: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> StartMissionAsync(Guid avatarId, Guid missionId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // In a real implementation, this would validate the mission exists and is available
                var progress = new MissionProgress
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    MissionId = missionId,
                    Status = MissionStatus.InProgress,
                    StartedAt = DateTime.UtcNow,
                    Progress = 0
                };

                lock (_lockObject)
                {
                    if (!_missionProgress.ContainsKey(avatarId))
                    {
                        _missionProgress[avatarId] = new List<MissionProgress>();
                    }
                    _missionProgress[avatarId].Add(progress);
                }

                result.Result = true;
                result.Message = "Mission started successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error starting mission: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> UpdateMissionProgressAsync(Guid avatarId, Guid missionId, int progress, string note = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_missionProgress.TryGetValue(avatarId, out var progresses))
                {
                    var missionProgress = progresses.FirstOrDefault(p => p.MissionId == missionId);
                    if (missionProgress != null)
                    {
                        missionProgress.Progress = Math.Min(progress, 100);
                        missionProgress.LastUpdated = DateTime.UtcNow;
                        missionProgress.Notes = note;

                        if (missionProgress.Progress >= 100)
                        {
                            missionProgress.Status = MissionStatus.Completed;
                            missionProgress.CompletedAt = DateTime.UtcNow;

                            // Award rewards
                            await AwardMissionRewardsAsync(avatarId, missionId);
                        }

                        result.Result = true;
                        result.Message = "Mission progress updated successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Result = false;
                        result.Message = "Mission progress not found.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No mission progress found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error updating mission progress: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<MissionProgress>>> GetMissionProgressAsync(Guid avatarId, MissionStatus? status = null)
        {
            var result = new OASISResult<List<MissionProgress>>();
            try
            {
                if (_missionProgress.TryGetValue(avatarId, out var progresses))
                {
                    var filteredProgresses = progresses.AsQueryable();

                    if (status.HasValue)
                    {
                        filteredProgresses = filteredProgresses.Where(p => p.Status == status.Value);
                    }

                    result.Result = filteredProgresses.ToList();
                    result.Message = "Mission progress retrieved successfully.";
                }
                else
                {
                    result.Result = new List<MissionProgress>();
                    result.Message = "No mission progress found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving mission progress: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private async Task AwardMissionRewardsAsync(Guid avatarId, Guid missionId)
        {
            try
            {
                // In a real implementation, this would get the mission details and award appropriate rewards
                // For now, award some default rewards
                var karmaManager = KarmaManager.Instance;
                await karmaManager.AddKarmaAsync(avatarId, 100, KarmaSourceType.Quest, "Mission completion reward");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error awarding mission rewards: {ex.Message}");
            }
        }

        #region Competition Tracking

        private async Task UpdateMissionCompetitionScoresAsync(Guid avatarId, MissionType missionType, MissionDifficulty difficulty)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;
                
                // Calculate score based on mission type and difficulty
                var score = CalculateMissionScore(missionType, difficulty);
                
                // Update quest completion competition scores
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.QuestCompletion, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.QuestCompletion, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.QuestCompletion, SeasonType.Monthly, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating mission competition scores: {ex.Message}");
            }
        }

        private long CalculateMissionScore(MissionType missionType, MissionDifficulty difficulty)
        {
            var baseScore = difficulty switch
            {
                MissionDifficulty.Easy => 10,
                MissionDifficulty.Medium => 25,
                MissionDifficulty.Hard => 50,
                MissionDifficulty.Expert => 100,
                _ => 10
            };

            var typeMultiplier = missionType switch
            {
                MissionType.Exploration => 1.0,
                MissionType.Social => 1.2,
                MissionType.Collection => 1.5,
                MissionType.Combat => 2.0,
                MissionType.Crafting => 1.3,
                _ => 1.0
            };

            return (long)(baseScore * typeMultiplier);
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetMissionStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var progresses = _missionProgress.GetValueOrDefault(avatarId, new List<MissionProgress>());
                
                var totalMissions = progresses.Count;
                var completedMissions = progresses.Count(p => p.Status == MissionStatus.Completed);
                var inProgressMissions = progresses.Count(p => p.Status == MissionStatus.InProgress);
                var failedMissions = progresses.Count(p => p.Status == MissionStatus.Failed);

                var missionTypeDistribution = progresses
                    .GroupBy(p => p.MissionType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var difficultyDistribution = progresses
                    .GroupBy(p => p.MissionDifficulty)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["totalMissions"] = totalMissions,
                    ["completedMissions"] = completedMissions,
                    ["inProgressMissions"] = inProgressMissions,
                    ["failedMissions"] = failedMissions,
                    ["completionRate"] = totalMissions > 0 ? (double)completedMissions / totalMissions : 0,
                    ["missionTypeDistribution"] = missionTypeDistribution,
                    ["difficultyDistribution"] = difficultyDistribution,
                    ["totalScore"] = progresses.Sum(p => CalculateMissionScore(p.MissionType, p.MissionDifficulty)),
                    ["averageCompletionTime"] = CalculateAverageCompletionTime(progresses)
                };

                result.Result = stats;
                result.Message = "Mission statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving mission statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private double CalculateAverageCompletionTime(List<MissionProgress> progresses)
        {
            var completedMissions = progresses.Where(p => p.Status == MissionStatus.Completed && p.CompletedAt.HasValue);
            if (!completedMissions.Any()) return 0;

            return completedMissions.Average(p => (p.CompletedAt.Value - p.StartedAt).TotalHours);
        }

        #endregion
    }

    public class Mission
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public MissionType Type { get; set; }
        public MissionDifficulty Difficulty { get; set; }
        public long RewardKarma { get; set; }
        public long RewardExperience { get; set; }
        public List<string> Requirements { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MissionProgress
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public Guid MissionId { get; set; }
        public MissionStatus Status { get; set; }
        public int Progress { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string Notes { get; set; }
        public MissionType MissionType { get; set; }
        public MissionDifficulty MissionDifficulty { get; set; }
    }

    public enum MissionType
    {
        Exploration,
        Social,
        Collection,
        Combat,
        Crafting,
        Custom
    }

    public enum MissionDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    public enum MissionStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}
