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
    public partial class QuestManager : OASISManager
    {
        private static QuestManager _instance;
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<Quest>> _avatarQuests;
        private readonly Dictionary<Guid, List<QuestProgress>> _questProgress;

        public static QuestManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new QuestManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _avatarQuests = new Dictionary<Guid, List<Quest>>();
            _questProgress = new Dictionary<Guid, List<QuestProgress>>();
        }

        public async Task<OASISResult<List<Quest>>> GetAvailableQuestsAsync(Guid avatarId, QuestType? type = null, QuestDifficulty? difficulty = null)
        {
            var result = new OASISResult<List<Quest>>();
            try
            {
                // In a real implementation, this would query the database
                var mockQuests = new List<Quest>
                {
                    new Quest
                    {
                        Id = Guid.NewGuid(),
                        Name = "The Great Egg Hunt",
                        Description = "Discover and collect rare eggs across the OASIS",
                        Type = QuestType.EggQuest,
                        Difficulty = QuestDifficulty.Medium,
                        RewardKarma = 300,
                        RewardExperience = 150,
                        Requirements = new List<string> { "Find 5 rare eggs", "Visit 3 different locations" },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Quest
                    {
                        Id = Guid.NewGuid(),
                        Name = "Social Butterfly",
                        Description = "Connect with other avatars and build relationships",
                        Type = QuestType.SideQuest,
                        Difficulty = QuestDifficulty.Easy,
                        RewardKarma = 100,
                        RewardExperience = 50,
                        Requirements = new List<string> { "Send 20 messages", "Join 3 chat sessions" },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Quest
                    {
                        Id = Guid.NewGuid(),
                        Name = "OASIS Explorer",
                        Description = "Explore the vast world of the OASIS",
                        Type = QuestType.MainQuest,
                        Difficulty = QuestDifficulty.Hard,
                        RewardKarma = 500,
                        RewardExperience = 250,
                        Requirements = new List<string> { "Visit 10 unique locations", "Complete 5 missions" },
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                var filteredQuests = mockQuests.AsQueryable();

                if (type.HasValue)
                {
                    filteredQuests = filteredQuests.Where(q => q.Type == type.Value);
                }

                if (difficulty.HasValue)
                {
                    filteredQuests = filteredQuests.Where(q => q.Difficulty == difficulty.Value);
                }

                result.Result = filteredQuests.ToList();
                result.Message = "Available quests retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving available quests: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> StartQuestAsync(Guid avatarId, Guid questId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // In a real implementation, this would validate the quest exists and is available
                var progress = new QuestProgress
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    QuestId = questId,
                    Status = QuestStatus.InProgress,
                    StartedAt = DateTime.UtcNow,
                    Progress = 0
                };

                lock (_lockObject)
                {
                    if (!_questProgress.ContainsKey(avatarId))
                    {
                        _questProgress[avatarId] = new List<QuestProgress>();
                    }
                    _questProgress[avatarId].Add(progress);
                }

                result.Result = true;
                result.Message = "Quest started successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error starting quest: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> UpdateQuestProgressAsync(Guid avatarId, Guid questId, int progress, string note = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_questProgress.TryGetValue(avatarId, out var progresses))
                {
                    var questProgress = progresses.FirstOrDefault(p => p.QuestId == questId);
                    if (questProgress != null)
                    {
                        questProgress.Progress = Math.Min(progress, 100);
                        questProgress.LastUpdated = DateTime.UtcNow;
                        questProgress.Notes = note;

                        if (questProgress.Progress >= 100)
                        {
                            questProgress.Status = QuestStatus.Completed;
                            questProgress.CompletedAt = DateTime.UtcNow;

                            // Award rewards
                            await AwardQuestRewardsAsync(avatarId, questId);
                        }

                        result.Result = true;
                        result.Message = "Quest progress updated successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Result = false;
                        result.Message = "Quest progress not found.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No quest progress found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error updating quest progress: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<QuestProgress>>> GetQuestProgressAsync(Guid avatarId, QuestStatus? status = null)
        {
            var result = new OASISResult<List<QuestProgress>>();
            try
            {
                if (_questProgress.TryGetValue(avatarId, out var progresses))
                {
                    var filteredProgresses = progresses.AsQueryable();

                    if (status.HasValue)
                    {
                        filteredProgresses = filteredProgresses.Where(p => p.Status == status.Value);
                    }

                    result.Result = filteredProgresses.ToList();
                    result.Message = "Quest progress retrieved successfully.";
                }
                else
                {
                    result.Result = new List<QuestProgress>();
                    result.Message = "No quest progress found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving quest progress: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private async Task AwardQuestRewardsAsync(Guid avatarId, Guid questId)
        {
            try
            {
                // In a real implementation, this would get the quest details and award appropriate rewards
                // For now, award some default rewards
                var karmaManager = KarmaManager.Instance;
                await karmaManager.AddKarmaAsync(avatarId, 200, KarmaSourceType.Game, "Quest completion reward");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error awarding quest rewards: {ex.Message}");
            }
        }

        #region Competition Tracking

        private async Task UpdateQuestCompetitionScoresAsync(Guid avatarId, QuestType questType, QuestDifficulty difficulty)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;
                
                // Calculate score based on quest type and difficulty
                var score = CalculateQuestScore(questType, difficulty);
                
                // Update quest completion competition scores
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.QuestCompletion, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.QuestCompletion, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.QuestCompletion, SeasonType.Monthly, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quest competition scores: {ex.Message}");
            }
        }

        private long CalculateQuestScore(QuestType questType, QuestDifficulty difficulty)
        {
            var baseScore = difficulty switch
            {
                QuestDifficulty.Easy => 15,
                QuestDifficulty.Medium => 35,
                QuestDifficulty.Hard => 75,
                QuestDifficulty.Expert => 150,
                _ => 15
            };

            var typeMultiplier = questType switch
            {
                QuestType.MainQuest => 2.0,
                QuestType.SideQuest => 1.0,
                QuestType.MagicQuest => 1.5,
                QuestType.EggQuest => 1.8,
                _ => 1.0
            };

            return (long)(baseScore * typeMultiplier);
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetQuestStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var progresses = _questProgress.GetValueOrDefault(avatarId, new List<QuestProgress>());
                
                var totalQuests = progresses.Count;
                var completedQuests = progresses.Count(p => p.Status == QuestStatus.Completed);
                var inProgressQuests = progresses.Count(p => p.Status == QuestStatus.InProgress);
                var failedQuests = progresses.Count(p => p.Status == QuestStatus.Failed);

                var questTypeDistribution = progresses
                    .GroupBy(p => p.QuestType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var difficultyDistribution = progresses
                    .GroupBy(p => p.QuestDifficulty)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["totalQuests"] = totalQuests,
                    ["completedQuests"] = completedQuests,
                    ["inProgressQuests"] = inProgressQuests,
                    ["failedQuests"] = failedQuests,
                    ["completionRate"] = totalQuests > 0 ? (double)completedQuests / totalQuests : 0,
                    ["questTypeDistribution"] = questTypeDistribution,
                    ["difficultyDistribution"] = difficultyDistribution,
                    ["totalScore"] = progresses.Sum(p => CalculateQuestScore(p.QuestType, p.QuestDifficulty)),
                    ["averageCompletionTime"] = CalculateAverageCompletionTime(progresses)
                };

                result.Result = stats;
                result.Message = "Quest statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving quest statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private double CalculateAverageCompletionTime(List<QuestProgress> progresses)
        {
            var completedQuests = progresses.Where(p => p.Status == QuestStatus.Completed && p.CompletedAt.HasValue);
            if (!completedQuests.Any()) return 0;

            return completedQuests.Average(p => (p.CompletedAt.Value - p.StartedAt).TotalHours);
        }

        #endregion
    }

    public class Quest
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public QuestType Type { get; set; }
        public QuestDifficulty Difficulty { get; set; }
        public long RewardKarma { get; set; }
        public long RewardExperience { get; set; }
        public List<string> Requirements { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class QuestProgress
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public Guid QuestId { get; set; }
        public QuestStatus Status { get; set; }
        public int Progress { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string Notes { get; set; }
        public QuestType QuestType { get; set; }
        public QuestDifficulty QuestDifficulty { get; set; }
    }

    public enum QuestDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}
