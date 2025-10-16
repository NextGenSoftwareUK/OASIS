using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class KarmaManager : OASISManager
    {
        private static KarmaManager _instance;
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<KarmaTransaction>> _karmaTransactions;
        private readonly Dictionary<Guid, long> _avatarKarma;

        public static KarmaManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new KarmaManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public KarmaManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _karmaTransactions = new Dictionary<Guid, List<KarmaTransaction>>();
            _avatarKarma = new Dictionary<Guid, long>();
        }

        public async Task<OASISResult<long>> GetKarmaAsync(Guid avatarId)
        {
            var result = new OASISResult<long>();
            try
            {
                if (_avatarKarma.TryGetValue(avatarId, out var karma))
                {
                    result.Result = karma;
                    result.Message = "Karma retrieved successfully.";
                }
                else
                {
                    result.Result = 0;
                    result.Message = "No karma found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving karma: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> AddKarmaAsync(Guid avatarId, long amount, KarmaSourceType sourceType, string description = null, Guid? relatedEntityId = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                var transaction = new KarmaTransaction
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    Amount = amount,
                    SourceType = sourceType,
                    Description = description,
                    RelatedEntityId = relatedEntityId,
                    Timestamp = DateTime.UtcNow
                };

                lock (_lockObject)
                {
                    if (!_karmaTransactions.ContainsKey(avatarId))
                    {
                        _karmaTransactions[avatarId] = new List<KarmaTransaction>();
                    }
                    _karmaTransactions[avatarId].Add(transaction);

                    if (!_avatarKarma.ContainsKey(avatarId))
                    {
                        _avatarKarma[avatarId] = 0;
                    }
                    _avatarKarma[avatarId] += amount;
                }

                // Update competition scores
                await UpdateKarmaCompetitionScoresAsync(avatarId, amount);

                result.Result = true;
                result.Message = $"Karma added successfully. New total: {_avatarKarma[avatarId]}";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error adding karma: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> DeductKarmaAsync(Guid avatarId, long amount, KarmaSourceType sourceType, string description = null, Guid? relatedEntityId = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_avatarKarma.ContainsKey(avatarId) || _avatarKarma[avatarId] < amount)
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "Insufficient karma balance.";
                    return await Task.FromResult(result);
                }

                var transaction = new KarmaTransaction
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    Amount = -amount,
                    SourceType = sourceType,
                    Description = description,
                    RelatedEntityId = relatedEntityId,
                    Timestamp = DateTime.UtcNow
                };

                lock (_lockObject)
                {
                    if (!_karmaTransactions.ContainsKey(avatarId))
                    {
                        _karmaTransactions[avatarId] = new List<KarmaTransaction>();
                    }
                    _karmaTransactions[avatarId].Add(transaction);

                    _avatarKarma[avatarId] -= amount;
                }

                result.Result = true;
                result.Message = $"Karma deducted successfully. New total: {_avatarKarma[avatarId]}";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error deducting karma: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<KarmaTransaction>>> GetKarmaHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<KarmaTransaction>>();
            try
            {
                if (_karmaTransactions.TryGetValue(avatarId, out var transactions))
                {
                    result.Result = transactions
                        .OrderByDescending(t => t.Timestamp)
                        .Skip(offset)
                        .Take(limit)
                        .ToList();
                    result.Message = "Karma history retrieved successfully.";
                }
                else
                {
                    result.Result = new List<KarmaTransaction>();
                    result.Message = "No karma history found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving karma history: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(Guid avatarId, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, int karmaOverride = 0)
        {
            var result = new OASISResult<KarmaAkashicRecord>();
            try
            {
                // Load avatar detail
                var avatarDetailResult = AvatarManager.Instance.LoadAvatarDetail(avatarId);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar detail. Reason: {avatarDetailResult.Message}";
                    return await Task.FromResult(result);
                }
                var avatarDetail = avatarDetailResult.Result;

                // Calculate karma amount
                var karmaAmount = GetKarmaForType(karmaType);
                if (karmaType == KarmaTypePositive.Other)
                    karmaAmount = karmaOverride;

                // Update avatar totals
                avatarDetail.Karma += karmaAmount;

                // Create akashic record
                var record = new KarmaAkashicRecord
                {
                    AvatarId = avatarId,
                    Date = DateTime.Now,
                    Karma = karmaAmount,
                    TotalKarma = avatarDetail.Karma,
                    // Provider expects EnumValue<ProviderType>; use the current provider enum directly
                    Provider = ProviderManager.Instance.CurrentStorageProviderType,
                    KarmaSourceTitle = karamSourceTitle,
                    KarmaSourceDesc = karmaSourceDesc,
                    WebLink = webLink,
                    KarmaSource = new EnumValue<KarmaSourceType>(karmaSourceType),
                    KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(KarmaEarntOrLost.Earnt),
                    KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(KarmaTypeNegative.None),
                    KarmaTypePositive = new EnumValue<KarmaTypePositive>(karmaType)
                };

                if (avatarDetail.KarmaAkashicRecords == null)
                    avatarDetail.KarmaAkashicRecords = new List<IKarmaAkashicRecord>();
                avatarDetail.KarmaAkashicRecords.Add(record);

                // Persist avatar detail
                AvatarManager.Instance.SaveAvatarDetail(avatarDetail);

                // Update in-memory ledgers and competition
                var amount = Math.Abs(record.Karma);
                await AddKarmaAsync(avatarId, amount, record.KarmaSource.Value, karamSourceTitle);
                await UpdateKarmaCompetitionScoresAsync(avatarId, amount);

                result.Result = record;
                result.Message = "Karma added successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error adding karma: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(Guid avatarId, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, int karmaOverride = 0)
        {
            var result = new OASISResult<KarmaAkashicRecord>();
            try
            {
                // Load avatar detail
                var avatarDetailResult = AvatarManager.Instance.LoadAvatarDetail(avatarId);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar detail. Reason: {avatarDetailResult.Message}";
                    return await Task.FromResult(result);
                }
                var avatarDetail = avatarDetailResult.Result;

                // Calculate karma amount
                var karmaAmount = GetKarmaForType(karmaType);
                if (karmaType == KarmaTypeNegative.Other)
                    karmaAmount = karmaOverride;

                // Update avatar totals
                avatarDetail.Karma -= karmaAmount;

                // Create akashic record
                var record = new KarmaAkashicRecord
                {
                    AvatarId = avatarId,
                    Date = DateTime.Now,
                    Karma = karmaAmount,
                    TotalKarma = avatarDetail.Karma,
                    // Provider expects EnumValue<ProviderType>; use the current provider enum directly
                    Provider = ProviderManager.Instance.CurrentStorageProviderType,
                    KarmaSourceTitle = karamSourceTitle,
                    KarmaSourceDesc = karmaSourceDesc,
                    WebLink = webLink,
                    KarmaSource = new EnumValue<KarmaSourceType>(karmaSourceType),
                    KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(KarmaEarntOrLost.Lost),
                    KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(karmaType),
                    KarmaTypePositive = new EnumValue<KarmaTypePositive>(KarmaTypePositive.None)
                };

                if (avatarDetail.KarmaAkashicRecords == null)
                    avatarDetail.KarmaAkashicRecords = new List<IKarmaAkashicRecord>();
                avatarDetail.KarmaAkashicRecords.Add(record);

                // Persist avatar detail
                AvatarManager.Instance.SaveAvatarDetail(avatarDetail);

                // Update in-memory ledgers and competition (negative amount)
                var amount = Math.Abs(record.Karma);
                await DeductKarmaAsync(avatarId, amount, record.KarmaSource.Value, karamSourceTitle);
                await UpdateKarmaCompetitionScoresAsync(avatarId, -amount);

                result.Result = record;
                result.Message = "Karma removed successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error removing karma: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private int GetKarmaForType(KarmaTypePositive karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypePositive.BeAHero: return 7;
                case KarmaTypePositive.BeASuperHero: return 8;
                case KarmaTypePositive.BeATeamPlayer: return 5;
                case KarmaTypePositive.BeingDetermined: return 5;
                case KarmaTypePositive.BeingFast: return 5;
                case KarmaTypePositive.ContributingTowardsAGoodCauseAdministrator: return 3;
                case KarmaTypePositive.ContributingTowardsAGoodCauseSpeaker: return 8;
                case KarmaTypePositive.ContributingTowardsAGoodCauseContributor: return 5;
                case KarmaTypePositive.ContributingTowardsAGoodCauseCreatorOrganiser: return 10;
                case KarmaTypePositive.ContributingTowardsAGoodCauseFunder: return 8;
                case KarmaTypePositive.ContributingTowardsAGoodCausePeacefulProtesterActivist: return 5;
                case KarmaTypePositive.ContributingTowardsAGoodCauseSharer: return 3;
                case KarmaTypePositive.HelpingAnimals: return 5;
                case KarmaTypePositive.HelpingTheEnvironment: return 5;
                case KarmaTypePositive.Other: return 2;
                case KarmaTypePositive.OurWorld: return 5;
                case KarmaTypePositive.SelfHelpImprovement: return 2;
                default: return 0;
            }
        }

        private int GetKarmaForType(KarmaTypeNegative karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypeNegative.AttackPhysciallyOtherPersonOrPeople: return 10;
                case KarmaTypeNegative.AttackVerballyOtherPersonOrPeople: return 5;
                case KarmaTypeNegative.BeingSelfish: return 3;
                case KarmaTypeNegative.DisrespectPersonOrPeople: return 4;
                case KarmaTypeNegative.DropLitter: return 9;
                case KarmaTypeNegative.HarmingAnimals: return 10;
                case KarmaTypeNegative.HarmingChildren: return 9;
                case KarmaTypeNegative.HarmingNature: return 10;
                case KarmaTypeNegative.NotTeamPlayer: return 3;
                case KarmaTypeNegative.NutritionEatDiary: return 6;
                case KarmaTypeNegative.NutritionEatDrinkUnhealthy: return 3;
                case KarmaTypeNegative.NutritionEatMeat: return 7;
                case KarmaTypeNegative.Other: return 1;
                case KarmaTypeNegative.OurWorldAttackOtherPlayer: return 7;
                case KarmaTypeNegative.OurWorldBeSelfish: return 4;
                case KarmaTypeNegative.OurWorldDisrespectOtherPlayer: return 5;
                case KarmaTypeNegative.OurWorldDropLitter: return 7;
                case KarmaTypeNegative.OurWorldNotTeamPlayer: return 3;
                default: return 0;
            }
        }

        public async Task<OASISResult<bool>> TransferKarmaAsync(Guid fromAvatarId, Guid toAvatarId, long amount, string description = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Check if sender has enough karma
                if (!_avatarKarma.ContainsKey(fromAvatarId) || _avatarKarma[fromAvatarId] < amount)
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "Insufficient karma balance for transfer.";
                    return await Task.FromResult(result);
                }

                // Deduct from sender
                var deductResult = await DeductKarmaAsync(fromAvatarId, amount, KarmaSourceType.Platform, $"Transferred to {toAvatarId}");
                if (deductResult.IsError)
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = deductResult.Message;
                    return await Task.FromResult(result);
                }

                // Add to recipient
                var addResult = await AddKarmaAsync(toAvatarId, amount, KarmaSourceType.Platform, $"Received from {toAvatarId}");
                if (addResult.IsError)
                {
                    // Rollback the deduction
                    await AddKarmaAsync(fromAvatarId, amount, KarmaSourceType.Platform, "Transfer rollback");
                    result.IsError = true;
                    result.Result = false;
                    result.Message = addResult.Message;
                    return await Task.FromResult(result);
                }

                result.Result = true;
                result.Message = $"Karma transfer successful. {amount} karma transferred from {fromAvatarId} to {toAvatarId}";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error transferring karma: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #region Competition Tracking

        private async Task UpdateKarmaCompetitionScoresAsync(Guid avatarId, long karmaAmount)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;
                
                // Update karma competition scores (karma is the primary competition metric)
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.Karma, SeasonType.Daily, karmaAmount);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.Karma, SeasonType.Weekly, karmaAmount);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.Karma, SeasonType.Monthly, karmaAmount);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.Karma, SeasonType.Quarterly, karmaAmount);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.Karma, SeasonType.Yearly, karmaAmount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating karma competition scores: {ex.Message}");
            }
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetKarmaStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var totalKarma = _avatarKarma.GetValueOrDefault(avatarId, 0);
                var transactions = _karmaTransactions.GetValueOrDefault(avatarId, new List<KarmaTransaction>());
                
                var totalEarned = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var totalSpent = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
                var netKarma = totalEarned - totalSpent;

                var sourceDistribution = transactions
                    .GroupBy(t => t.SourceType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Sum(t => Math.Abs(t.Amount)));

                var recentActivity = transactions
                    .OrderByDescending(t => t.Timestamp)
                    .Take(10)
                    .Select(t => new { t.SourceType, t.Amount, t.Description, t.Timestamp })
                    .ToList();

                var stats = new Dictionary<string, object>
                {
                    ["totalKarma"] = totalKarma,
                    ["totalEarned"] = totalEarned,
                    ["totalSpent"] = totalSpent,
                    ["netKarma"] = netKarma,
                    ["transactionCount"] = transactions.Count,
                    ["sourceDistribution"] = sourceDistribution,
                    ["recentActivity"] = recentActivity,
                    ["averageTransaction"] = transactions.Count > 0 ? transactions.Average(t => Math.Abs(t.Amount)) : 0,
                    ["largestEarning"] = transactions.Where(t => t.Amount > 0).Max(t => t.Amount),
                    ["largestSpending"] = Math.Abs(transactions.Where(t => t.Amount < 0).Min(t => t.Amount))
                };

                result.Result = stats;
                result.Message = "Karma statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving karma statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion
    }

    public class KarmaTransaction
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public long Amount { get; set; }
        public KarmaSourceType SourceType { get; set; }
        public string Description { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum KarmaCategory
    {
        Quest,
        SocialActivity,
        HelpfulAction,
        Gift,
        Transfer,
        System,
        Achievement,
        Event,
        Penalty
    }
}
