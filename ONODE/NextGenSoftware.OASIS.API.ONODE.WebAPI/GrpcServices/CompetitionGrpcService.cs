using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GrpcServices
{
    public class CompetitionGrpcService : CompetitionService.CompetitionServiceBase
    {
        public override async Task<JsonResponse> GetLeaderboard(GetLeaderboardRequest request, ServerCallContext context)
        {
            try
            {
                var ct = ParseEnum(request.CompetitionType, CompetitionType.Karma);
                var st = ParseEnum(request.SeasonType, SeasonType.Daily);
                var result = await CompetitionManager.Instance.GetLeaderboardAsync(ct, st, request.Limit, request.Offset);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetAvatarRank(GetAvatarRankRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var ct = ParseEnum(request.CompetitionType, CompetitionType.Karma);
                var st = ParseEnum(request.SeasonType, SeasonType.Daily);
                var result = await CompetitionManager.Instance.GetAvatarRankAsync(avatarId, ct, st);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetLeagues(GetLeaguesRequest request, ServerCallContext context)
        {
            try
            {
                var ct = ParseEnum(request.CompetitionType, CompetitionType.Karma);
                var st = ParseEnum(request.SeasonType, SeasonType.Daily);
                var result = await CompetitionManager.Instance.GetAvailableLeaguesAsync(ct, st);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetAvatarLeague(GetAvatarLeagueRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new JsonResponse { IsError = true, Message = "Invalid avatar ID." };
                var ct = ParseEnum(request.CompetitionType, CompetitionType.Karma);
                var st = ParseEnum(request.SeasonType, SeasonType.Daily);
                var result = await CompetitionManager.Instance.GetAvatarLeagueAsync(avatarId, ct, st);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<JsonResponse> GetActiveTournaments(GetActiveTournamentsRequest request, ServerCallContext context)
        {
            try
            {
                var ct = ParseEnum(request.CompetitionType, CompetitionType.Karma);
                var result = await CompetitionManager.Instance.GetActiveTournamentsAsync(ct);
                return result.IsError
                    ? new JsonResponse { IsError = true, Message = result.Message }
                    : new JsonResponse { Json = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new JsonResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OASISGrpcResponse> JoinTournament(JoinTournamentRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid avatar ID." };
                if (!Guid.TryParse(request.TournamentId, out var tournamentId)) return new OASISGrpcResponse { IsError = true, Message = "Invalid tournament ID." };
                var result = await CompetitionManager.Instance.JoinTournamentAsync(avatarId, tournamentId);
                return result.IsError ? new OASISGrpcResponse { IsError = true, Message = result.Message } : new OASISGrpcResponse();
            }
            catch (Exception ex) { return new OASISGrpcResponse { IsError = true, Message = ex.Message }; }
        }

        private static async Task ActivateProviderIfSpecifiedAsync(string providerType, bool setGlobally)
        {
            if (string.IsNullOrWhiteSpace(providerType)) return;
            if (!System.Enum.TryParse<ProviderType>(providerType, true, out var pt)) return;
            if (pt == ProviderType.Default || pt == ProviderType.None) return;
            await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(pt, null, false, setGlobally);
        }

        private static T ParseEnum<T>(string value, T defaultValue) where T : struct, System.Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            return System.Enum.TryParse<T>(value, true, out var result) ? result : defaultValue;
        }
    }
}
