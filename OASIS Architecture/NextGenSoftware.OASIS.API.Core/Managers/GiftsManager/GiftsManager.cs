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
    public partial class GiftsManager : OASISManager
    {
        private static GiftsManager _instance;
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<Gift>> _avatarGifts;
        private readonly Dictionary<Guid, List<GiftTransaction>> _giftTransactions;

        public static GiftsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GiftsManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public GiftsManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _avatarGifts = new Dictionary<Guid, List<Gift>>();
            _giftTransactions = new Dictionary<Guid, List<GiftTransaction>>();
        }

        public async Task<OASISResult<List<Gift>>> GetAllGiftsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<Gift>>();
            try
            {
                if (_avatarGifts.TryGetValue(avatarId, out var gifts))
                {
                    result.Result = gifts.ToList();
                    result.Message = "Gifts retrieved successfully.";
                }
                else
                {
                    result.Result = new List<Gift>();
                    result.Message = "No gifts found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving gifts: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<Gift>> SendGiftAsync(Guid fromAvatarId, Guid toAvatarId, GiftType giftType, string message = null, Dictionary<string, object> metadata = null)
        {
            var result = new OASISResult<Gift>();
            try
            {
                var gift = new Gift
                {
                    Id = Guid.NewGuid(),
                    FromAvatarId = fromAvatarId,
                    ToAvatarId = toAvatarId,
                    GiftType = giftType,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    IsReceived = false,
                    IsOpened = false,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                lock (_lockObject)
                {
                    if (!_avatarGifts.ContainsKey(toAvatarId))
                    {
                        _avatarGifts[toAvatarId] = new List<Gift>();
                    }
                    _avatarGifts[toAvatarId].Add(gift);

                    // Record transaction
                    var transaction = new GiftTransaction
                    {
                        Id = Guid.NewGuid(),
                        GiftId = gift.Id,
                        FromAvatarId = fromAvatarId,
                        ToAvatarId = toAvatarId,
                        TransactionType = GiftTransactionType.Sent,
                        Timestamp = DateTime.UtcNow
                    };

                    if (!_giftTransactions.ContainsKey(fromAvatarId))
                    {
                        _giftTransactions[fromAvatarId] = new List<GiftTransaction>();
                    }
                    _giftTransactions[fromAvatarId].Add(transaction);
                }

                // Update competition scores
                await UpdateGiftCompetitionScoresAsync(fromAvatarId, giftType);

                result.Result = gift;
                result.Message = "Gift sent successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = null;
                result.Message = $"Error sending gift: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> ReceiveGiftAsync(Guid avatarId, Guid giftId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_avatarGifts.TryGetValue(avatarId, out var gifts))
                {
                    var gift = gifts.FirstOrDefault(g => g.Id == giftId);
                    if (gift != null)
                    {
                        gift.IsReceived = true;
                        gift.ReceivedAt = DateTime.UtcNow;

                        // Record transaction
                        var transaction = new GiftTransaction
                        {
                            Id = Guid.NewGuid(),
                            GiftId = giftId,
                            FromAvatarId = gift.FromAvatarId,
                            ToAvatarId = avatarId,
                            TransactionType = GiftTransactionType.Received,
                            Timestamp = DateTime.UtcNow
                        };

                        if (!_giftTransactions.ContainsKey(avatarId))
                        {
                            _giftTransactions[avatarId] = new List<GiftTransaction>();
                        }
                        _giftTransactions[avatarId].Add(transaction);

                        result.Result = true;
                        result.Message = "Gift received successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Result = false;
                        result.Message = "Gift not found.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No gifts found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error receiving gift: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> OpenGiftAsync(Guid avatarId, Guid giftId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_avatarGifts.TryGetValue(avatarId, out var gifts))
                {
                    var gift = gifts.FirstOrDefault(g => g.Id == giftId);
                    if (gift != null && gift.IsReceived)
                    {
                        gift.IsOpened = true;
                        gift.OpenedAt = DateTime.UtcNow;

                        result.Result = true;
                        result.Message = "Gift opened successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Result = false;
                        result.Message = "Gift not found or not received yet.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No gifts found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error opening gift: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<GiftTransaction>>> GetGiftHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<GiftTransaction>>();
            try
            {
                if (_giftTransactions.TryGetValue(avatarId, out var transactions))
                {
                    result.Result = transactions
                        .OrderByDescending(t => t.Timestamp)
                        .Skip(offset)
                        .Take(limit)
                        .ToList();
                    result.Message = "Gift history retrieved successfully.";
                }
                else
                {
                    result.Result = new List<GiftTransaction>();
                    result.Message = "No gift history found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving gift history: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #region Competition Tracking

        private async Task UpdateGiftCompetitionScoresAsync(Guid avatarId, GiftType giftType)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;
                
                // Calculate score based on gift type
                var score = CalculateGiftScore(giftType);
                
                // Update social activity competition scores
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Monthly, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating gift competition scores: {ex.Message}");
            }
        }

        private long CalculateGiftScore(GiftType giftType)
        {
            return giftType switch
            {
                GiftType.Karma => 10,
                GiftType.Experience => 15,
                GiftType.Coin => 5,
                GiftType.Item => 20,
                GiftType.Egg => 50,
                GiftType.Badge => 25,
                GiftType.Title => 30,
                GiftType.Custom => 5,
                _ => 1
            };
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetGiftStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var sentGifts = _giftTransactions.GetValueOrDefault(avatarId, new List<GiftTransaction>())
                    .Where(t => t.TransactionType == GiftTransactionType.Sent).Count();
                
                var receivedGifts = _avatarGifts.GetValueOrDefault(avatarId, new List<Gift>()).Count;
                var openedGifts = _avatarGifts.GetValueOrDefault(avatarId, new List<Gift>())
                    .Count(g => g.IsOpened);

                var giftTypeDistribution = _avatarGifts.GetValueOrDefault(avatarId, new List<Gift>())
                    .GroupBy(g => g.GiftType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["sentGifts"] = sentGifts,
                    ["receivedGifts"] = receivedGifts,
                    ["openedGifts"] = openedGifts,
                    ["unopenedGifts"] = receivedGifts - openedGifts,
                    ["giftTypeDistribution"] = giftTypeDistribution,
                    ["totalScore"] = sentGifts * 10 + receivedGifts * 5 + openedGifts * 3,
                    ["mostCommonGiftType"] = giftTypeDistribution.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None"
                };

                result.Result = stats;
                result.Message = "Gift statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving gift statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion
    }

    public class Gift
    {
        public Guid Id { get; set; }
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public GiftType GiftType { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public bool IsReceived { get; set; }
        public bool IsOpened { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class GiftTransaction
    {
        public Guid Id { get; set; }
        public Guid GiftId { get; set; }
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
        public GiftTransactionType TransactionType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum GiftType
    {
        Karma,
        Experience,
        Coin,
        Item,
        Egg,
        Badge,
        Title,
        Custom
    }

    public enum GiftTransactionType
    {
        Sent,
        Received,
        Opened
    }
}
