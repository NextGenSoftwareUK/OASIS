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
                var deductResult = await DeductKarmaAsync(fromAvatarId, amount, KarmaCategory.Transfer, $"Transferred to {toAvatarId}");
                if (deductResult.IsError)
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = deductResult.Message;
                    return await Task.FromResult(result);
                }

                // Add to recipient
                var addResult = await AddKarmaAsync(toAvatarId, amount, KarmaCategory.Transfer, $"Received from {toAvatarId}");
                if (addResult.IsError)
                {
                    // Rollback the deduction
                    await AddKarmaAsync(fromAvatarId, amount, KarmaCategory.Transfer, "Transfer rollback");
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
