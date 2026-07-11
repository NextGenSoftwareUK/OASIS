using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Assembles a rich avatar context block from Web4 (karma) and Web5 (quests) in parallel.
    /// Used both as an internal context-injection helper and as the standalone GET /v1/context/avatar/{id} endpoint.
    /// </summary>
    public class StarnetContextManager : OASISManager
    {
        private static readonly HttpClient _http = new HttpClient();

        public StarnetContextManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public StarnetContextManager(IOASISStorageProvider provider, Guid avatarId, OASISDNA OASISDNA = null) : base(provider, avatarId, OASISDNA) { }

        private string Web4Base => Environment.GetEnvironmentVariable("WEB4_API_BASE_URL")
            ?? OASISDNA?.OASIS?.Web6?.Web4BaseUrl
            ?? "https://api.oasisomniverse.one";

        private string Web5Base => Environment.GetEnvironmentVariable("WEB5_API_BASE_URL")
            ?? OASISDNA?.OASIS?.Web6?.Web5BaseUrl
            ?? "https://api.star.oasisomniverse.one";

        public async Task<OASISResult<AvatarContextResponse>> GetAvatarContextAsync(Guid avatarId, string bearerToken = null)
        {
            OASISResult<AvatarContextResponse> result = new OASISResult<AvatarContextResponse>();

            if (avatarId == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "AvatarId is required.");
                return result;
            }

            var ctx = new AvatarContextResponse { AvatarId = avatarId, AssembledAtUtc = DateTime.UtcNow };

            Task<(int karma, string level, string name)> karmaTask = FetchKarmaAsync(avatarId, bearerToken);
            Task<List<QuestSummary>> questsTask = FetchQuestsAsync(avatarId, bearerToken);

            await Task.WhenAll(karmaTask, questsTask);

            (ctx.KarmaScore, ctx.KarmaLevel, ctx.DisplayName) = await karmaTask;
            ctx.ActiveQuests = await questsTask;

            result.Result = ctx;
            return result;
        }

        /// <summary>Formats the context block as a string suitable for injecting into an AI system message.</summary>
        public async Task<string> GetAvatarContextStringAsync(Guid avatarId, string bearerToken = null)
        {
            OASISResult<AvatarContextResponse> res = await GetAvatarContextAsync(avatarId, bearerToken);
            if (res.IsError || res.Result == null) return string.Empty;

            var ctx = res.Result;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[OASIS Avatar Context]");
            if (!string.IsNullOrEmpty(ctx.DisplayName)) sb.AppendLine($"Name: {ctx.DisplayName}");
            sb.AppendLine($"Karma: {ctx.KarmaScore} ({ctx.KarmaLevel})");
            if (ctx.ActiveQuests.Count > 0)
            {
                sb.AppendLine("Active Quests:");
                foreach (var q in ctx.ActiveQuests)
                    sb.AppendLine($"  - {q.Name}: {q.Progress}");
            }
            return sb.ToString().TrimEnd();
        }

        private async Task<(int karma, string level, string name)> FetchKarmaAsync(Guid avatarId, string token)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{Web4Base}/api/karma/{avatarId}");
                if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var resp = await _http.SendAsync(req);
                string body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return (0, "Unknown", null);
                using var doc = JsonDocument.Parse(body);
                var r = doc.RootElement.TryGetProperty("result", out var res) ? res : doc.RootElement;
                int karma = r.TryGetProperty("karmaTotal", out var k) ? k.GetInt32() : 0;
                string level = r.TryGetProperty("karmaLevel", out var l) ? l.GetString() : KarmaLevel(karma);
                string name = r.TryGetProperty("username", out var n) ? n.GetString() : null;
                return (karma, level, name);
            }
            catch { return (0, "Unknown", null); }
        }

        private async Task<List<QuestSummary>> FetchQuestsAsync(Guid avatarId, string token)
        {
            var list = new List<QuestSummary>();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{Web5Base}/api/quests/avatar/{avatarId}");
                if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var resp = await _http.SendAsync(req);
                string body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return list;
                using var doc = JsonDocument.Parse(body);
                var arr = doc.RootElement.TryGetProperty("result", out var r) ? r : doc.RootElement;
                if (arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var q in arr.EnumerateArray())
                    {
                        list.Add(new QuestSummary
                        {
                            Id = q.TryGetProperty("id", out var id) ? Guid.TryParse(id.GetString(), out Guid g) ? g : Guid.Empty : Guid.Empty,
                            Name = q.TryGetProperty("name", out var nm) ? nm.GetString() : "Unknown",
                            Progress = q.TryGetProperty("progress", out var p) ? p.GetString() : ""
                        });
                    }
                }
            }
            catch { }
            return list;
        }

        private static string KarmaLevel(int karma) => karma switch
        {
            < 100 => "Initiate",
            < 500 => "Apprentice",
            < 1000 => "Journeyman",
            < 5000 => "Adept",
            < 10000 => "Master",
            _ => "Grand Master"
        };
    }
}
