using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class CompetitionManager : OASISManager
    {
        private static CompetitionManager _instance;
        private readonly object _lockObject = new object();
        private readonly Dictionary<CompetitionType, Dictionary<SeasonType, List<LeaderboardEntry>>> _leaderboards;
        private readonly Dictionary<Guid, List<League>> _avatarLeagues;
        private readonly Dictionary<Guid, List<Tournament>> _tournaments;

        public static CompetitionManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CompetitionManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public CompetitionManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _leaderboards = new Dictionary<CompetitionType, Dictionary<SeasonType, List<LeaderboardEntry>>>();
            _avatarLeagues = new Dictionary<Guid, List<League>>();
            _tournaments = new Dictionary<Guid, List<Tournament>>();
        }

        #region Leaderboard Methods

        public async Task<OASISResult<List<LeaderboardEntry>>> GetLeaderboardAsync(CompetitionType competitionType, SeasonType seasonType, int limit = 100, int offset = 0)
        {
            var result = new OASISResult<List<LeaderboardEntry>>();
            try
            {
                if (_leaderboards.TryGetValue(competitionType, out var seasons) && 
                    seasons.TryGetValue(seasonType, out var entries))
                {
                    result.Result = entries
                        .OrderByDescending(e => e.Score)
                        .Skip(offset)
                        .Take(limit)
                        .ToList();
                    result.Message = $"Leaderboard for {competitionType} {seasonType} retrieved successfully.";
                }
                else
                {
                    result.Result = new List<LeaderboardEntry>();
                    result.Message = $"No leaderboard data found for {competitionType} {seasonType}.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving leaderboard: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<LeaderboardEntry>> GetAvatarRankAsync(Guid avatarId, CompetitionType competitionType, SeasonType seasonType)
        {
            var result = new OASISResult<LeaderboardEntry>();
            try
            {
                if (_leaderboards.TryGetValue(competitionType, out var seasons) && 
                    seasons.TryGetValue(seasonType, out var entries))
                {
                    var entry = entries.FirstOrDefault(e => e.AvatarId == avatarId);
                    if (entry != null)
                    {
                        result.Result = entry;
                        result.Message = "Avatar rank retrieved successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = "Avatar not found in leaderboard.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Message = $"No leaderboard data found for {competitionType} {seasonType}.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving avatar rank: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> UpdateAvatarScoreAsync(Guid avatarId, CompetitionType competitionType, SeasonType seasonType, long scoreChange, Dictionary<string, object> additionalStats = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                lock (_lockObject)
                {
                    if (!_leaderboards.ContainsKey(competitionType))
                        _leaderboards[competitionType] = new Dictionary<SeasonType, List<LeaderboardEntry>>();
                    
                    if (!_leaderboards[competitionType].ContainsKey(seasonType))
                        _leaderboards[competitionType][seasonType] = new List<LeaderboardEntry>();

                    var entries = _leaderboards[competitionType][seasonType];
                    var entry = entries.FirstOrDefault(e => e.AvatarId == avatarId);

                    if (entry == null)
                    {
                        // Create new entry
                        entry = new LeaderboardEntry
                        {
                            AvatarId = avatarId,
                            CompetitionType = competitionType,
                            SeasonType = seasonType,
                            Score = scoreChange,
                            SeasonStart = GetSeasonStart(seasonType),
                            SeasonEnd = GetSeasonEnd(seasonType)
                        };
                        entries.Add(entry);
                    }
                    else
                    {
                        entry.Score += scoreChange;
                        entry.LastUpdated = DateTime.UtcNow;
                    }

                    // Update additional stats if provided
                    if (additionalStats != null)
                    {
                        foreach (var stat in additionalStats)
                        {
                            entry.Stats[stat.Key] = stat.Value;
                        }
                    }

                    // Recalculate ranks
                    RecalculateRanks(entries);
                }

                result.Result = true;
                result.Message = "Avatar score updated successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error updating avatar score: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion

        #region League Methods

        public async Task<OASISResult<List<League>>> GetAvailableLeaguesAsync(CompetitionType competitionType, SeasonType seasonType)
        {
            var result = new OASISResult<List<League>>();
            try
            {
                // In a real implementation, this would query the database
                var leagues = new List<League>
                {
                    new League
                    {
                        Name = "Bronze League",
                        LeagueType = LeagueType.Bronze,
                        CompetitionType = competitionType,
                        MinScore = 0,
                        MaxScore = 1000,
                        SeasonType = seasonType,
                        IsActive = true
                    },
                    new League
                    {
                        Name = "Silver League",
                        LeagueType = LeagueType.Silver,
                        CompetitionType = competitionType,
                        MinScore = 1001,
                        MaxScore = 5000,
                        SeasonType = seasonType,
                        IsActive = true
                    },
                    new League
                    {
                        Name = "Gold League",
                        LeagueType = LeagueType.Gold,
                        CompetitionType = competitionType,
                        MinScore = 5001,
                        MaxScore = 15000,
                        SeasonType = seasonType,
                        IsActive = true
                    }
                };

                result.Result = leagues;
                result.Message = "Available leagues retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving leagues: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<League>> GetAvatarLeagueAsync(Guid avatarId, CompetitionType competitionType, SeasonType seasonType)
        {
            var result = new OASISResult<League>();
            try
            {
                var rankResult = await GetAvatarRankAsync(avatarId, competitionType, seasonType);
                if (rankResult.IsError)
                {
                    result.IsError = true;
                    result.Message = rankResult.Message;
                    return await Task.FromResult(result);
                }

                var score = rankResult.Result.Score;
                var leaguesResult = await GetAvailableLeaguesAsync(competitionType, seasonType);
                
                if (leaguesResult.IsError)
                {
                    result.IsError = true;
                    result.Message = leaguesResult.Message;
                    return await Task.FromResult(result);
                }

                var league = leaguesResult.Result.FirstOrDefault(l => score >= l.MinScore && score <= l.MaxScore);
                if (league != null)
                {
                    result.Result = league;
                    result.Message = "Avatar league retrieved successfully.";
                }
                else
                {
                    result.IsError = true;
                    result.Message = "No league found for avatar's score.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving avatar league: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion

        #region Tournament Methods

        public async Task<OASISResult<List<Tournament>>> GetActiveTournamentsAsync(CompetitionType competitionType = CompetitionType.Karma)
        {
            var result = new OASISResult<List<Tournament>>();
            try
            {
                // In a real implementation, this would query the database
                var tournaments = new List<Tournament>
                {
                    new Tournament
                    {
                        Name = "Weekly Karma Championship",
                        Description = "Compete for the highest karma score this week!",
                        TournamentType = TournamentType.SingleElimination,
                        CompetitionType = competitionType,
                        Status = TournamentStatus.Registration,
                        StartDate = DateTime.UtcNow.AddDays(1),
                        EndDate = DateTime.UtcNow.AddDays(7),
                        RegistrationStart = DateTime.UtcNow,
                        RegistrationEnd = DateTime.UtcNow.AddDays(1),
                        MaxParticipants = 64,
                        CurrentParticipants = 0
                    }
                };

                result.Result = tournaments;
                result.Message = "Active tournaments retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving tournaments: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> JoinTournamentAsync(Guid avatarId, Guid tournamentId)
        {
            var result = new OASISResult<bool>();
            try
            {
                // In a real implementation, this would update the database
                result.Result = true;
                result.Message = "Successfully joined tournament.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error joining tournament: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion

        #region Helper Methods

        private void RecalculateRanks(List<LeaderboardEntry> entries)
        {
            var sortedEntries = entries.OrderByDescending(e => e.Score).ToList();
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var entry = sortedEntries[i];
                entry.PreviousRank = entry.Rank;
                entry.Rank = i + 1;
                entry.RankChange = entry.PreviousRank - entry.Rank;
            }
        }

        private DateTime GetSeasonStart(SeasonType seasonType)
        {
            var now = DateTime.UtcNow;
            return seasonType switch
            {
                SeasonType.Daily => now.Date,
                SeasonType.Weekly => now.Date.AddDays(-(int)now.DayOfWeek),
                SeasonType.Monthly => new DateTime(now.Year, now.Month, 1),
                SeasonType.Quarterly => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
                SeasonType.Yearly => new DateTime(now.Year, 1, 1),
                _ => now.Date
            };
        }

        private DateTime GetSeasonEnd(SeasonType seasonType)
        {
            var start = GetSeasonStart(seasonType);
            return seasonType switch
            {
                SeasonType.Daily => start.AddDays(1).AddTicks(-1),
                SeasonType.Weekly => start.AddDays(7).AddTicks(-1),
                SeasonType.Monthly => start.AddMonths(1).AddTicks(-1),
                SeasonType.Quarterly => start.AddMonths(3).AddTicks(-1),
                SeasonType.Yearly => start.AddYears(1).AddTicks(-1),
                _ => start.AddDays(1).AddTicks(-1)
            };
        }

        #endregion
    }
}
