// OGStarApiClient — HTTP client for the WEB5 STAR API (C:\Source\OASIS2\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI).
//
// Covers the editor-relevant endpoints:
//   GET  /api/games          — list all OGames registered in STAR
//   GET  /api/quests         — list quests (optionally filtered by gameId)
//   POST /api/quests/create  — create a new quest
//   GET  /api/missions       — list missions for a quest
//   GET  /api/geohotspots    — list geo hotspots
//   POST /api/quests/{id}/bind-objective — bind a quest objective to a map trigger
//
// All methods return raw JSON strings. Parse with whatever JSON library the host editor uses.
// .NET Standard 2.0 — no System.Text.Json; uses HttpWebRequest so it works in .NET 4.7.2 / Unity.

using System;
using System.IO;
using System.Net;
using System.Text;

namespace OGEditorSDK
{
    /// <summary>
    /// Lightweight synchronous HTTP client for the OASIS STAR API.
    /// For async-capable hosts (.NET 5+), wrap calls on a background thread.
    /// </summary>
    public sealed class OGStarApiClient : IDisposable
    {
        private readonly string _baseUrl;
        private readonly string _avatarId;
        private bool _disposed;

        /// <param name="baseUrl">STAR API base URL, e.g. "http://localhost:5000" or "https://star.oasisplatform.world"</param>
        /// <param name="avatarId">Authenticated avatar GUID — sent as X-Avatar-Id header.</param>
        public OGStarApiClient(string baseUrl, string avatarId)
        {
            _baseUrl  = baseUrl.TrimEnd('/');
            _avatarId = avatarId ?? string.Empty;
        }

        // ── Games ─────────────────────────────────────────────────────────────────

        /// <summary>Returns JSON array of OGames registered in STAR.</summary>
        public string GetGames() => Get("/api/games");

        // ── Quests ────────────────────────────────────────────────────────────────

        /// <summary>Returns JSON array of all quests, optionally filtered by OGame ID.</summary>
        public string GetQuests(string gameId = null)
        {
            string url = "/api/quests";
            if (!string.IsNullOrEmpty(gameId)) url += "?gameId=" + Uri.EscapeDataString(gameId);
            return Get(url);
        }

        /// <summary>Create a new quest. <paramref name="questJson"/> is the request body JSON.</summary>
        public string CreateQuest(string questJson) => Post("/api/quests/create", questJson);

        /// <summary>Bind a quest objective to a map trigger (thing, linedef, or sector).</summary>
        /// <param name="questId">Quest GUID.</param>
        /// <param name="bindingJson">JSON body, e.g. {"objectiveId":"…","mapPath":"…","triggerType":"Thing","triggerId":42}</param>
        public string BindObjective(string questId, string bindingJson) =>
            Post("/api/quests/" + Uri.EscapeDataString(questId) + "/bind-objective", bindingJson);

        // ── Missions ──────────────────────────────────────────────────────────────

        public string GetMissions(string questId = null)
        {
            string url = "/api/missions";
            if (!string.IsNullOrEmpty(questId)) url += "?questId=" + Uri.EscapeDataString(questId);
            return Get(url);
        }

        // ── GeoHotSpots ───────────────────────────────────────────────────────────

        public string GetGeoHotSpots(string gameId = null)
        {
            string url = "/api/geohotspots";
            if (!string.IsNullOrEmpty(gameId)) url += "?gameId=" + Uri.EscapeDataString(gameId);
            return Get(url);
        }

        // ── OGAssets (live catalog) ───────────────────────────────────────────────

        /// <summary>
        /// Returns JSON array of OGAssets from STAR for the given game.
        /// Shape: [{"thingType":5001,"displayName":"Blue Keycard","category":"Keys","gameId":"OQUAKE","nativeClassname":"item_key1"}, …]
        /// </summary>
        public string GetAssets(string gameId = null)
        {
            string url = "/api/oassets";
            if (!string.IsNullOrEmpty(gameId)) url += "?gameId=" + Uri.EscapeDataString(gameId);
            return Get(url);
        }

        // ── Portals ───────────────────────────────────────────────────────────────

        /// <summary>Persist a portal entry to STAR so the Omniverse kernel can load it at runtime.</summary>
        public string PostPortal(string portalJson) => Post("/api/portals", portalJson);

        // ── HTTP helpers ──────────────────────────────────────────────────────────

        private string Get(string path)
        {
            var req = CreateRequest(path, "GET");
            return ReadResponse(req);
        }

        private string Post(string path, string jsonBody)
        {
            var req = CreateRequest(path, "POST");
            byte[] body = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            req.ContentType   = "application/json";
            req.ContentLength = body.Length;
            using (var stream = req.GetRequestStream())
                stream.Write(body, 0, body.Length);
            return ReadResponse(req);
        }

        private HttpWebRequest CreateRequest(string path, string method)
        {
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + path);
            req.Method  = method;
            req.Timeout = 15000;
            req.Headers.Add("X-Avatar-Id", _avatarId);
            req.Accept = "application/json";
            return req;
        }

        private static string ReadResponse(HttpWebRequest req)
        {
            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr   = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException ex) when (ex.Response != null)
            {
                using (var sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                    throw new OGStarApiException(((HttpWebResponse)ex.Response).StatusCode, sr.ReadToEnd());
            }
        }

        public void Dispose() { _disposed = true; }
    }

    /// <summary>Thrown when the STAR API returns a non-2xx response.</summary>
    public sealed class OGStarApiException : Exception
    {
        public HttpStatusCode StatusCode  { get; }
        public string         ResponseBody { get; }

        public OGStarApiException(HttpStatusCode code, string body)
            : base("STAR API error " + (int)code + ": " + body)
        {
            StatusCode   = code;
            ResponseBody = body;
        }
    }
}
