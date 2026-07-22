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
// TODO: ONODE.Core references removed to avoid circular dependency - these methods need to be moved to ONODE project
// using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
// using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

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
                if (_avatarKarma.TryGetValue(avatarId, out var karma) && karma > 0)
                {
                    result.Result = karma;
                    result.Message = "Karma retrieved successfully.";
                }
                else
                {
                    // In-memory cache empty — load from persistent store
                    var avatarDetail = AvatarManager.Instance.LoadAvatarDetail(avatarId);
                    if (avatarDetail != null && !avatarDetail.IsError && avatarDetail.Result != null)
                    {
                        result.Result = avatarDetail.Result.Karma;
                        result.Message = "Karma retrieved successfully.";
                    }
                    else
                    {
                        result.Result = 0;
                        result.Message = "No karma found for this avatar.";
                    }
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

                // Update karma statistics in settings system whenever karma changes
                try
                {
                    var karmaStats = new Dictionary<string, object>
                    {
                        ["totalKarma"] = _avatarKarma[avatarId],
                        ["karmaTransactions"] = _karmaTransactions[avatarId].Count,
                        ["lastKarmaChange"] = DateTime.UtcNow,
                        ["lastKarmaAmount"] = amount,
                        ["lastKarmaSource"] = sourceType.ToString()
                    };
                    await HolonManager.Instance.SaveSettingsAsync(avatarId, "karma", karmaStats);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the main operation
                    Console.WriteLine($"Warning: Failed to save karma statistics: {ex.Message}");
                }

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

                // Update karma statistics in settings system whenever karma changes
                try
                {
                    var karmaStats = new Dictionary<string, object>
                    {
                        ["totalKarma"] = _avatarKarma[avatarId],
                        ["karmaTransactions"] = _karmaTransactions[avatarId].Count,
                        ["lastKarmaChange"] = DateTime.UtcNow,
                        ["lastKarmaAmount"] = -amount,
                        ["lastKarmaSource"] = sourceType.ToString()
                    };
                    await HolonManager.Instance.SaveSettingsAsync(avatarId, "karma", karmaStats);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the main operation
                    Console.WriteLine($"Warning: Failed to save karma statistics: {ex.Message}");
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
                // OASIS platform actions
                case KarmaTypePositive.CreateAvatar: return 25;
                case KarmaTypePositive.CompleteProfile: return 40;
                case KarmaTypePositive.CreateBenevolentOApp: return 75;
                case KarmaTypePositive.DeployBenevolentOApp: return 100;
                case KarmaTypePositive.CreateBenevolentQuest: return 60;
                case KarmaTypePositive.CompleteBenevolentQuest: return 80;
                //case KarmaTypePositive.CreateHolon: return 1; // TODO: Define what a BenevolentHolon is before rewarding this
                case KarmaTypePositive.GiftSeeds: return 50;
                case KarmaTypePositive.DonateSeeds: return 75;
                case KarmaTypePositive.PlaceBenevolentGeoNFT: return 25;
                case KarmaTypePositive.CollectBenevolentGeoNFT: return 15;
                case KarmaTypePositive.MintBenevolentNFT: return 20;
                case KarmaTypePositive.MintBenevolentGeoNFT: return 20;
                case KarmaTypePositive.PayWithSeeds: return 50;
                case KarmaTypePositive.DonateWithSeeds: return 75;
                case KarmaTypePositive.RewardWithSeeds: return 60;
                case KarmaTypePositive.SendInviteToJoinSeeds: return 30;
                case KarmaTypePositive.AcceptInviteToJoinSeeds: return 25;
                case KarmaTypePositive.ContributingToTheOASISWithCode: return 500;
                case KarmaTypePositive.ContributingToTheOASISWithPR: return 450;
                case KarmaTypePositive.ContributingToTheOASISWithSupport: return 300;
                case KarmaTypePositive.ContributingToTheOASISWithMarketing: return 450;
                case KarmaTypePositive.ContributingToTheOASISWithFunding: return 500;
                case KarmaTypePositive.ContributingToTheOASISWithSales: return 450;
                case KarmaTypePositive.ContributingToTheOASIS: return 300;
                // OurWorld in-game actions (lower than real-world equivalents)
                //case KarmaTypePositive.OurWorld: return 230;
                case KarmaTypePositive.OurWorldPickupLitter: return 150;
                case KarmaTypePositive.OurWorldHelpOtherPlayer: return 100;
                case KarmaTypePositive.OurWorldHelpOtherPlayers: return 150;
                case KarmaTypePositive.OurWorldDefendPlayer: return 125;
                case KarmaTypePositive.OurWorldDefendPlayers: return 175;
                case KarmaTypePositive.OurWorldDefendBase: return 150;
                case KarmaTypePositive.OurWorldBeSelfless: return 175;
                case KarmaTypePositive.OurWorldBeAHero: return 200;
                case KarmaTypePositive.OurWorldBeASuperHero: return 250;
                case KarmaTypePositive.OurWorldBeSuperman: return 300;
                case KarmaTypePositive.OurWorlBeATeamPlayer: return 150;
                case KarmaTypePositive.OurWorldLevelUp: return 100;
                case KarmaTypePositive.OurWorldBeingPresent: return 100;
                case KarmaTypePositive.OurWorldBeingDetermined: return 125;
                case KarmaTypePositive.OurWorldBeingMindful: return 100;
                case KarmaTypePositive.OurWorldBeingHappy: return 100;
                case KarmaTypePositive.OurWorldBeingPeaceful: return 100;
                case KarmaTypePositive.OurWorldBeingWise: return 125;
                case KarmaTypePositive.OurWorldBeingPositive: return 100;
                case KarmaTypePositive.OurWorldBeingFast: return 100;
                case KarmaTypePositive.OurWorldBeingSuperFast: return 150;
                case KarmaTypePositive.OurWorldBeingStrong: return 100;
                case KarmaTypePositive.OurWorldBeingSuperStrong: return 150;
                case KarmaTypePositive.OurWorldBeingGrateful: return 100;
                case KarmaTypePositive.OurWorldSpeakingYourTruth: return 125;
                case KarmaTypePositive.OurWorldOther: return 75;
                // Real-world positive actions (significantly higher than platform actions to reflect real impact)
                case KarmaTypePositive.PickupLitter: return 200;
                case KarmaTypePositive.HelpOtherPerson: return 225;
                case KarmaTypePositive.HelpOtherPeople: return 275;
                case KarmaTypePositive.DefendOtherPerson: return 250;
                case KarmaTypePositive.DefendOtherPeople: return 300;
                case KarmaTypePositive.BeSelfless: return 250;
                case KarmaTypePositive.BeAHero: return 280;
                case KarmaTypePositive.BeASuperHero: return 330;
                case KarmaTypePositive.BeSuperman: return 400;
                case KarmaTypePositive.BeATeamPlayer: return 200;
                case KarmaTypePositive.LevelUp: return 200;
                case KarmaTypePositive.BeingPresent: return 215;
                case KarmaTypePositive.BeingDetermined: return 200;
                case KarmaTypePositive.BeingMindful: return 215;
                case KarmaTypePositive.BeingHappy: return 215;
                case KarmaTypePositive.BeingPeaceful: return 215;
                case KarmaTypePositive.BeingWise: return 225;
                case KarmaTypePositive.BeingPositive: return 215;
                case KarmaTypePositive.BeingFast: return 200;
                case KarmaTypePositive.BeingSuperFast: return 225;
                case KarmaTypePositive.BeingStrong: return 215;
                case KarmaTypePositive.BeingSuperStrong: return 250;
                case KarmaTypePositive.BeingGrateful: return 215;
                case KarmaTypePositive.SpeakingYourTruth: return 225;
                case KarmaTypePositive.NutritionEatDrinkHealthy: return 200;
                case KarmaTypePositive.SelfHelpImprovement: return 215;
                case KarmaTypePositive.HelpingTheEnvironment: return 400;
                case KarmaTypePositive.HelpingAnimals: return 400;
                case KarmaTypePositive.KeepThePeace: return 400;
                case KarmaTypePositive.HelpLocalCommunity: return 400;
                case KarmaTypePositive.HelpElderly: return 450;
                case KarmaTypePositive.HelpTheHomeless: return 450;
                case KarmaTypePositive.HelpKidsOnStreets: return 500;
                case KarmaTypePositive.HelpKids: return 450;
                case KarmaTypePositive.Volunteering: return 350;
                case KarmaTypePositive.ContributingTowardsAGoodCauseContributor: return 350;
                case KarmaTypePositive.ContributingTowardsAGoodCauseSharer: return 350;
                case KarmaTypePositive.ContributingTowardsAGoodCauseAdministrator: return 350;
                case KarmaTypePositive.ContributingTowardsAGoodCauseCreatorOrganiser: return 777;
                case KarmaTypePositive.ContributingTowardsAGoodCauseFunder: return 500;
                case KarmaTypePositive.ContributingTowardsAGoodCauseSpeaker: return 350;
                case KarmaTypePositive.ContributingTowardsAGoodCausePeacefulProtesterActivist: return 350;
                case KarmaTypePositive.Other: return 210;
                default: return 0;
            }
        }

        private int GetKarmaForType(KarmaTypeNegative karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypeNegative.AttackPhysciallyOtherPersonOrPeople: return 2000;
                case KarmaTypeNegative.AttackVerballyOtherPersonOrPeople: return 500;
                case KarmaTypeNegative.BeingSelfish: return 50;
                case KarmaTypeNegative.DisrespectPersonOrPeople: return 50;
                case KarmaTypeNegative.DropLitter: return 500;
                case KarmaTypeNegative.HarmingAnimals: return 2000;
                case KarmaTypeNegative.HarmingChildren: return 2000;
                case KarmaTypeNegative.HarmingNature: return 2000;
                case KarmaTypeNegative.NotTeamPlayer: return 15;
                case KarmaTypeNegative.NutritionEatDrinkUnhealthy: return 10;
                case KarmaTypeNegative.SpamAbuse: return 150;
                case KarmaTypeNegative.OurWorldAttackOtherPlayer: return 200;
                case KarmaTypeNegative.OurWorldBeSelfish: return 50;
                case KarmaTypeNegative.OurWorldDisrespectOtherPlayer: return 20;
                case KarmaTypeNegative.OurWorldDropLitter: return 250;
                case KarmaTypeNegative.OurWorldNotTeamPlayer: return 15;
                case KarmaTypeNegative.Other: return 5;
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

        #region Karma Weighting Voting (using Proposal holon)
        // TODO: This region is commented out because it requires ONODE.Core which creates a circular dependency
        // These methods should be moved to the ONODE project
        /*
        /// <summary>
        /// Votes for a positive karma weighting using the Proposal holon system
        /// </summary>
        public async Task<OASISResult<IKarmaWeightingProposal>> VoteForPositiveKarmaWeightingAsync(
            KarmaTypePositive karmaType, int weighting, Guid avatarId)
        {
            var result = new OASISResult<IKarmaWeightingProposal>();

            try
            {
                // Get or create proposal for this karma type
                var proposalCategory = $"KarmaWeighting.Positive.{karmaType}";
                var proposalResult = await GetOrCreateKarmaWeightingProposalAsync(
                    karmaType, null, weighting, true, avatarId, proposalCategory);

                if (proposalResult.IsError || proposalResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(proposalResult, result);
                    return result;
                }

                var proposal = proposalResult.Result;

                // Check if user already voted
                if (proposal.HasUserVoted(avatarId))
                {
                    OASISErrorHandling.HandleError(ref result, "You have already voted on this karma weighting proposal.");
                    return result;
                }

                // Add vote (accept = true means they support this weighting)
                if (!proposal.AddVote(avatarId, true))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to add vote.");
                    return result;
                }

                // Save updated proposal
                var saveResult = await HolonManager.Instance.SaveHolonAsync<IKarmaWeightingProposal>(proposal, avatarId);
                if (saveResult.IsError || saveResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    return result;
                }

                result.Result = saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error voting for positive karma weighting: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Votes for a negative karma weighting using the Proposal holon system
        /// </summary>
        public async Task<OASISResult<IKarmaWeightingProposal>> VoteForNegativeKarmaWeightingAsync(
            KarmaTypeNegative karmaType, int weighting, Guid avatarId)
        {
            var result = new OASISResult<IKarmaWeightingProposal>();

            try
            {
                // Get or create proposal for this karma type
                var proposalCategory = $"KarmaWeighting.Negative.{karmaType}";
                var proposalResult = await GetOrCreateKarmaWeightingProposalAsync(
                    null, karmaType, weighting, false, avatarId, proposalCategory);

                if (proposalResult.IsError || proposalResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(proposalResult, result);
                    return result;
                }

                var proposal = proposalResult.Result;

                // Check if user already voted
                if (proposal.HasUserVoted(avatarId))
                {
                    OASISErrorHandling.HandleError(ref result, "You have already voted on this karma weighting proposal.");
                    return result;
                }

                // Add vote (accept = true means they support this weighting)
                if (!proposal.AddVote(avatarId, true))
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to add vote.");
                    return result;
                }

                // Save updated proposal
                var saveResult = await HolonManager.Instance.SaveHolonAsync<IKarmaWeightingProposal>(proposal, avatarId);
                if (saveResult.IsError || saveResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                    return result;
                }

                result.Result = saveResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error voting for negative karma weighting: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets or creates a karma weighting proposal
        /// </summary>
        private async Task<OASISResult<IKarmaWeightingProposal>> GetOrCreateKarmaWeightingProposalAsync(
            KarmaTypePositive? positiveKarmaType,
            KarmaTypeNegative? negativeKarmaType,
            int proposedWeighting,
            bool isPositiveKarma,
            Guid avatarId,
            string proposalCategory)
        {
            var result = new OASISResult<IKarmaWeightingProposal>();

            try
            {
                // Search for existing proposal
                var searchResult = await HolonManager.Instance.SearchHolonsAsync(
                    proposalCategory,
                    avatarId,
                    default(Guid),
                    false,
                    HolonType.Proposal
                );

                IKarmaWeightingProposal proposal = null;

                if (!searchResult.IsError && searchResult.Result != null)
                {
                    // Find matching proposal - need to load as typed holon
                    foreach (var holon in searchResult.Result)
                    {
                        var loadedProposal = await HolonManager.Instance.LoadHolonAsync<IKarmaWeightingProposal>(holon.Id);
                        if (!loadedProposal.IsError && loadedProposal.Result != null)
                        {
                            var prop = loadedProposal.Result;
                            if (prop.ProposalCategory == proposalCategory &&
                                prop.ProposedWeighting == proposedWeighting &&
                                prop.IsPositiveKarma == isPositiveKarma)
                            {
                                proposal = prop;
                                break;
                            }
                        }
                    }
                }

                // Create new proposal if not found
                if (proposal == null)
                {
                    // Get avatar name
                    string avatarName = avatarId.ToString();
                    try
                    {
                        var avatarLoad = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                        if (!avatarLoad.IsError && avatarLoad.Result != null)
                        {
                            avatarName = avatarLoad.Result.Username ?? avatarLoad.Result.Name ?? avatarId.ToString();
                        }
                    }
                    catch
                    {
                        // Use ID if name lookup fails
                    }

                    proposal = new KarmaWeightingProposal
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Karma Weighting Proposal: {proposalCategory} - Weighting: {proposedWeighting}",
                        Description = $"Proposal for {proposalCategory} with weighting {proposedWeighting}",
                        HolonType = HolonType.Proposal,
                        CreatedByAvatarId = avatarId,
                        CreatedByAvatarName = avatarName,
                        CreatedDate = DateTime.UtcNow,
                        PositiveKarmaType = positiveKarmaType,
                        NegativeKarmaType = negativeKarmaType,
                        ProposedWeighting = proposedWeighting,
                        IsPositiveKarma = isPositiveKarma,
                        ProposalCategory = proposalCategory,
                        IsNewHolon = true
                    };
                }

                result.Result = proposal;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting or creating karma weighting proposal: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Lists all karma weighting proposals
        /// </summary>
        public async Task<OASISResult<IEnumerable<IKarmaWeightingProposal>>> ListKarmaWeightingProposalsAsync(
            bool onlyMine = false, Guid? avatarId = null)
        {
            var result = new OASISResult<IEnumerable<IKarmaWeightingProposal>>();

            try
            {
                var searchAvatarId = onlyMine && avatarId.HasValue ? avatarId.Value : default(Guid);

                // Load all Proposal holons with ProposalType = "KarmaWeighting"
                var searchResult = await HolonManager.Instance.SearchHolonsAsync(
                    "KarmaWeighting",
                    searchAvatarId,
                    default(Guid),
                    onlyMine,
                    HolonType.Proposal
                );

                if (searchResult.IsError)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(searchResult, result);
                    return result;
                }

                // Filter to only KarmaWeighting proposals - need to load as typed holons
                var proposals = new List<IKarmaWeightingProposal>();
                if (searchResult.Result != null)
                {
                    foreach (var holon in searchResult.Result)
                    {
                        var loadedProposal = await HolonManager.Instance.LoadHolonAsync<IKarmaWeightingProposal>(holon.Id);
                        if (!loadedProposal.IsError && loadedProposal.Result != null)
                        {
                            var proposal = loadedProposal.Result;
                            if (proposal.ProposalType == "KarmaWeighting")
                            {
                                if (!onlyMine || proposal.CreatedByAvatarId == searchAvatarId)
                                {
                                    proposals.Add(proposal);
                                }
                            }
                        }
                    }
                }

                result.Result = proposals;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error listing karma weighting proposals: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets user's vote on a karma weighting proposal (if they voted)
        /// </summary>
        public async Task<OASISResult<bool?>> GetUserVoteOnKarmaWeightingProposalAsync(
            Guid proposalId, Guid avatarId)
        {
            var result = new OASISResult<bool?>();

            try
            {
                // Load the proposal holon using generic overload
                var loadResult = await HolonManager.Instance.LoadHolonAsync<IKarmaWeightingProposal>(proposalId);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
                    return result;
                }

                var proposal = loadResult.Result;

                // Get user's vote
                result.Result = proposal.GetUserVote(avatarId);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting user vote: {ex.Message}", ex);
            }

            return result;
        }
        */
        #endregion

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
