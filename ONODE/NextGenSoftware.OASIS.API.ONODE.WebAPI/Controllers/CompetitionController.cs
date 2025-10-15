using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/competition")]
    public class CompetitionController : OASISControllerBase
    {
        public CompetitionController()
        {
        }

        #region Leaderboard Endpoints

        /// <summary>
        /// Get leaderboard for a specific competition type and season
        /// </summary>
        /// <param name="competitionType">Type of competition (Karma, Experience, etc.)</param>
        /// <param name="seasonType">Season type (Daily, Weekly, Monthly, etc.)</param>
        /// <param name="limit">Number of entries to return</param>
        /// <param name="offset">Number of entries to skip</param>
        /// <returns>Leaderboard entries</returns>
        [HttpGet("leaderboard/{competitionType}/{seasonType}")]
        public async Task<OASISResult<List<LeaderboardEntry>>> GetLeaderboard(
            CompetitionType competitionType, 
            SeasonType seasonType, 
            [FromQuery] int limit = 100, 
            [FromQuery] int offset = 0)
        {
            return await CompetitionManager.Instance.GetLeaderboardAsync(competitionType, seasonType, limit, offset);
        }

        /// <summary>
        /// Get current avatar's rank in a specific competition
        /// </summary>
        /// <param name="competitionType">Type of competition</param>
        /// <param name="seasonType">Season type</param>
        /// <returns>Avatar's leaderboard entry</returns>
        [Authorize]
        [HttpGet("my-rank/{competitionType}/{seasonType}")]
        public async Task<OASISResult<LeaderboardEntry>> GetMyRank(CompetitionType competitionType, SeasonType seasonType)
        {
            return await CompetitionManager.Instance.GetAvatarRankAsync(Avatar.Id, competitionType, seasonType);
        }

        /// <summary>
        /// Get avatar's rank by ID in a specific competition
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="competitionType">Type of competition</param>
        /// <param name="seasonType">Season type</param>
        /// <returns>Avatar's leaderboard entry</returns>
        [HttpGet("rank/{avatarId}/{competitionType}/{seasonType}")]
        public async Task<OASISResult<LeaderboardEntry>> GetAvatarRank(Guid avatarId, CompetitionType competitionType, SeasonType seasonType)
        {
            return await CompetitionManager.Instance.GetAvatarRankAsync(avatarId, competitionType, seasonType);
        }

        #endregion

        #region League Endpoints

        /// <summary>
        /// Get available leagues for a competition type and season
        /// </summary>
        /// <param name="competitionType">Type of competition</param>
        /// <param name="seasonType">Season type</param>
        /// <returns>Available leagues</returns>
        [HttpGet("leagues/{competitionType}/{seasonType}")]
        public async Task<OASISResult<List<League>>> GetAvailableLeagues(CompetitionType competitionType, SeasonType seasonType)
        {
            return await CompetitionManager.Instance.GetAvailableLeaguesAsync(competitionType, seasonType);
        }

        /// <summary>
        /// Get current avatar's league
        /// </summary>
        /// <param name="competitionType">Type of competition</param>
        /// <param name="seasonType">Season type</param>
        /// <returns>Avatar's current league</returns>
        [Authorize]
        [HttpGet("my-league/{competitionType}/{seasonType}")]
        public async Task<OASISResult<League>> GetMyLeague(CompetitionType competitionType, SeasonType seasonType)
        {
            return await CompetitionManager.Instance.GetAvatarLeagueAsync(Avatar.Id, competitionType, seasonType);
        }

        /// <summary>
        /// Get avatar's league by ID
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="competitionType">Type of competition</param>
        /// <param name="seasonType">Season type</param>
        /// <returns>Avatar's current league</returns>
        [HttpGet("league/{avatarId}/{competitionType}/{seasonType}")]
        public async Task<OASISResult<League>> GetAvatarLeague(Guid avatarId, CompetitionType competitionType, SeasonType seasonType)
        {
            return await CompetitionManager.Instance.GetAvatarLeagueAsync(avatarId, competitionType, seasonType);
        }

        #endregion

        #region Tournament Endpoints

        /// <summary>
        /// Get active tournaments
        /// </summary>
        /// <param name="competitionType">Type of competition (optional)</param>
        /// <returns>Active tournaments</returns>
        [HttpGet("tournaments")]
        public async Task<OASISResult<List<Tournament>>> GetActiveTournaments([FromQuery] CompetitionType competitionType = CompetitionType.Karma)
        {
            return await CompetitionManager.Instance.GetActiveTournamentsAsync(competitionType);
        }

        /// <summary>
        /// Join a tournament
        /// </summary>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("tournaments/{tournamentId}/join")]
        public async Task<OASISResult<bool>> JoinTournament(Guid tournamentId)
        {
            return await CompetitionManager.Instance.JoinTournamentAsync(Avatar.Id, tournamentId);
        }

        #endregion

        #region Stats and Analytics

        /// <summary>
        /// Get competition statistics for the current avatar
        /// </summary>
        /// <param name="competitionType">Type of competition</param>
        /// <param name="seasonType">Season type</param>
        /// <returns>Competition statistics</returns>
        [Authorize]
        [HttpGet("stats/{competitionType}/{seasonType}")]
        public async Task<OASISResult<Dictionary<string, object>>> GetMyStats(CompetitionType competitionType, SeasonType seasonType)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var rankResult = await CompetitionManager.Instance.GetAvatarRankAsync(Avatar.Id, competitionType, seasonType);
                var leagueResult = await CompetitionManager.Instance.GetAvatarLeagueAsync(Avatar.Id, competitionType, seasonType);

                var stats = new Dictionary<string, object>
                {
                    ["avatarId"] = Avatar.Id,
                    ["avatarName"] = Avatar.Name,
                    ["competitionType"] = competitionType.ToString(),
                    ["seasonType"] = seasonType.ToString(),
                    ["hasRank"] = !rankResult.IsError,
                    ["hasLeague"] = !leagueResult.IsError
                };

                if (!rankResult.IsError)
                {
                    stats["rank"] = rankResult.Result.Rank;
                    stats["score"] = rankResult.Result.Score;
                    stats["rankChange"] = rankResult.Result.RankChange;
                    stats["lastUpdated"] = rankResult.Result.LastUpdated;
                }

                if (!leagueResult.IsError)
                {
                    stats["league"] = leagueResult.Result.Name;
                    stats["leagueType"] = leagueResult.Result.LeagueType.ToString();
                }

                result.Result = stats;
                result.Message = "Competition statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving competition statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion
    }
}
