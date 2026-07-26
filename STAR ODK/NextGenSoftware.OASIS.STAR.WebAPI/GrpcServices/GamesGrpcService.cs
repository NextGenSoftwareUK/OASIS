using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class GamesGrpcService : GamesService.GamesServiceBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        // ── Games ────────────────────────────────────────────────────────────

        public override async Task<GameListResponse> GetAllGames(GameEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new GameListResponse { IsError = true, Message = result.Message };
                var resp = new GameListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var g in result.Result)
                        resp.Items.Add(new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty, Description = g.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new GameListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> GetGame(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> CreateGame(GameMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> UpdateGame(GameMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var game = new Game { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.Game.UpdateAsync(avatarId, game);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameBoolMsg> DeleteGame(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.DeleteAsync(avatarId, id, 0);
                return new GameBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new GameBoolMsg { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> PublishGame(GamePublishMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.PublishAsync(avatarId, request.SourcePath, request.LaunchTarget, string.Empty, false, true, false, false);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> DownloadGame(GameDownloadMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.DownloadAsync(avatarId, id, request.Version, request.DownloadPath, false);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> ActivateGame(GameVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.ActivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameResponse> DeactivateGame(GameVersionedIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Game.DeactivateAsync(avatarId, id, request.Version);
                if (result.IsError)
                    return new GameResponse { IsError = true, Message = result.Message };
                var g = result.Result;
                return new GameResponse { IsError = false, Message = result.Message ?? "OK", Result = g == null ? null : new GameMessage { Id = g.Id.ToString(), Name = g.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new GameResponse { IsError = true, Message = ex.Message }; }
        }

        // ── Missions ─────────────────────────────────────────────────────────

        public override async Task<MissionListResponse> GetAllMissions(GameEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Missions.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new MissionListResponse { IsError = true, Message = result.Message };
                var resp = new MissionListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var m in result.Result)
                        resp.Items.Add(new MissionMessage { Id = m.Id.ToString(), Name = m.Name ?? string.Empty, Description = m.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new MissionListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MissionResponse> GetMission(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new MissionResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Missions.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new MissionResponse { IsError = true, Message = result.Message };
                var m = result.Result;
                return new MissionResponse { IsError = false, Message = result.Message ?? "OK", Result = m == null ? null : new MissionMessage { Id = m.Id.ToString(), Name = m.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new MissionResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MissionResponse> CreateMission(MissionMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Missions.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new MissionResponse { IsError = true, Message = result.Message };
                var m = result.Result;
                return new MissionResponse { IsError = false, Message = result.Message ?? "OK", Result = m == null ? null : new MissionMessage { Id = m.Id.ToString(), Name = m.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new MissionResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<MissionResponse> UpdateMission(MissionMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new MissionResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var mission = new Mission { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.Missions.UpdateAsync(avatarId, mission);
                if (result.IsError)
                    return new MissionResponse { IsError = true, Message = result.Message };
                var m = result.Result;
                return new MissionResponse { IsError = false, Message = result.Message ?? "OK", Result = m == null ? null : new MissionMessage { Id = m.Id.ToString(), Name = m.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new MissionResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameBoolMsg> DeleteMission(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Missions.DeleteAsync(avatarId, id, 0);
                return new GameBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new GameBoolMsg { IsError = true, Message = ex.Message }; }
        }

        // ── Quests ───────────────────────────────────────────────────────────

        public override async Task<QuestListResponse> GetAllQuests(GameEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Quests.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new QuestListResponse { IsError = true, Message = result.Message };
                var resp = new QuestListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var q in result.Result)
                        resp.Items.Add(new QuestMessage { Id = q.Id.ToString(), Name = q.Name ?? string.Empty, Description = q.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new QuestListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<QuestResponse> GetQuest(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new QuestResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Quests.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new QuestResponse { IsError = true, Message = result.Message };
                var q = result.Result;
                return new QuestResponse { IsError = false, Message = result.Message ?? "OK", Result = q == null ? null : new QuestMessage { Id = q.Id.ToString(), Name = q.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new QuestResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<QuestResponse> CreateQuest(QuestMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Quests.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new QuestResponse { IsError = true, Message = result.Message };
                var q = result.Result;
                return new QuestResponse { IsError = false, Message = result.Message ?? "OK", Result = q == null ? null : new QuestMessage { Id = q.Id.ToString(), Name = q.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new QuestResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<QuestResponse> UpdateQuest(QuestMessage request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new QuestResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var quest = new Quest { Id = id, Name = request.Name, Description = request.Description };
                var result = await _starAPI.Quests.UpdateAsync(avatarId, quest);
                if (result.IsError)
                    return new QuestResponse { IsError = true, Message = result.Message };
                var q = result.Result;
                return new QuestResponse { IsError = false, Message = result.Message ?? "OK", Result = q == null ? null : new QuestMessage { Id = q.Id.ToString(), Name = q.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new QuestResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameBoolMsg> DeleteQuest(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Quests.DeleteAsync(avatarId, id, 0);
                return new GameBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new GameBoolMsg { IsError = true, Message = ex.Message }; }
        }

        // ── Chapters ─────────────────────────────────────────────────────────

        public override async Task<ChapterListResponse> GetAllChapters(GameEmptyMsg request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Chapters.LoadAllAsync(avatarId, null);
                if (result.IsError)
                    return new ChapterListResponse { IsError = true, Message = result.Message };
                var resp = new ChapterListResponse { IsError = false, Message = result.Message ?? "OK" };
                if (result.Result != null)
                    foreach (var c in result.Result)
                        resp.Items.Add(new ChapterMessage { Id = c.Id.ToString(), Name = c.Name ?? string.Empty, Description = c.Description ?? string.Empty });
                return resp;
            }
            catch (Exception ex) { return new ChapterListResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ChapterResponse> GetChapter(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new ChapterResponse { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Chapters.LoadAsync(avatarId, id, 0);
                if (result.IsError)
                    return new ChapterResponse { IsError = true, Message = result.Message };
                var c = result.Result;
                return new ChapterResponse { IsError = false, Message = result.Message ?? "OK", Result = c == null ? null : new ChapterMessage { Id = c.Id.ToString(), Name = c.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new ChapterResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ChapterResponse> CreateChapter(ChapterMessage request, ServerCallContext context)
        {
            try
            {
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Chapters.CreateAsync(avatarId, request.Name, request.Description, default, string.Empty, null);
                if (result.IsError)
                    return new ChapterResponse { IsError = true, Message = result.Message };
                var c = result.Result;
                return new ChapterResponse { IsError = false, Message = result.Message ?? "OK", Result = c == null ? null : new ChapterMessage { Id = c.Id.ToString(), Name = c.Name ?? string.Empty } };
            }
            catch (Exception ex) { return new ChapterResponse { IsError = true, Message = ex.Message }; }
        }

        public override async Task<GameBoolMsg> DeleteChapter(GameIdMsg request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    return new GameBoolMsg { IsError = true, Message = "Invalid id format." };
                var avatarId = GrpcHelpers.GetAvatarId(context);
                var result = await _starAPI.Chapters.DeleteAsync(avatarId, id, 0);
                return new GameBoolMsg { IsError = result.IsError, Message = result.Message, Result = !result.IsError };
            }
            catch (Exception ex) { return new GameBoolMsg { IsError = true, Message = ex.Message }; }
        }

        // ── Competitions ─────────────────────────────────────────────────────
        // Competitions API not yet exposed on STARAPI — stub until available.

        public override Task<CompetitionListResponse> GetAllCompetitions(GameEmptyMsg request, ServerCallContext context)
            => Task.FromResult(new CompetitionListResponse { IsError = true, Message = "Competitions API is not yet available via gRPC in this release." });

        public override Task<CompetitionResponse> GetCompetition(GameIdMsg request, ServerCallContext context)
            => Task.FromResult(new CompetitionResponse { IsError = true, Message = "Competitions API is not yet available via gRPC in this release." });

        public override Task<CompetitionResponse> CreateCompetition(CompetitionMessage request, ServerCallContext context)
            => Task.FromResult(new CompetitionResponse { IsError = true, Message = "Competitions API is not yet available via gRPC in this release." });

        public override Task<GameBoolMsg> DeleteCompetition(GameIdMsg request, ServerCallContext context)
            => Task.FromResult(new GameBoolMsg { IsError = true, Message = "Competitions API is not yet available via gRPC in this release." });
    }
}
