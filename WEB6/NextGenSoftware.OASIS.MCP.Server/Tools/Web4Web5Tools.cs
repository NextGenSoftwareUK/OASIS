using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB4 (OASIS API/ONODE) and WEB5 (STAR API/STARNET) are large, pre-existing, already-complete REST APIs with
    /// their own full Swagger documentation (hundreds of endpoints across avatar/karma/NFT/wallet/holon/map/quest/
    /// mission/celestial-body/OAPP/STARNET-template management, etc). Rather than hand-duplicating every single
    /// endpoint as a bespoke typed tool, these two tools give an MCP client genuine, complete coverage of every
    /// endpoint on each API via real HTTP forwarding - call any method/path/body exactly as documented in each
    /// API's own Swagger UI (https://api.web4.oasisomniverse.one/swagger, https://api.starnet.oasisomniverse.one/swagger).
    /// </summary>
    [McpServerToolType]
    public static class Web4Web5Tools
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static string Web4BaseUrl => Environment.GetEnvironmentVariable("WEB4_API_BASE_URL") ?? "https://api.web4.oasisomniverse.one";

        private static string Web5BaseUrl => Environment.GetEnvironmentVariable("WEB5_API_BASE_URL") ?? "https://api.starnet.oasisomniverse.one";

        [McpServerTool(Name = "web4_request"), Description("WEB4 (OASIS API/ONODE): calls any endpoint of the full WEB4 REST API (avatar register/login, karma, NFTs, wallets, holons, maps, quests, missions, search, chat, etc - see https://api.web4.oasisomniverse.one/swagger for the complete list). path should start with /api/... and httpMethod is GET/POST/PUT/DELETE.")]
        public static async Task<string> Web4Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
        {
            return await ForwardRequestAsync(Web4BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);
        }

        [McpServerTool(Name = "web5_request"), Description("WEB5 (STAR API/STARNET): calls any endpoint of the full WEB5 STAR REST API (celestial bodies/spaces, missions, quests, OAPPs, templates, runtimes, libraries, STARNET holon publish/download/install, etc - see https://api.starnet.oasisomniverse.one/swagger for the complete list). path should start with /api/... and httpMethod is GET/POST/PUT/DELETE.")]
        public static async Task<string> Web5Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
        {
            return await ForwardRequestAsync(Web5BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);
        }

        // ── Priority 6: web7/8/9/10 HTTP passthrough tools ──────────────────────────

        private static string Web7BaseUrl  => Environment.GetEnvironmentVariable("WEB7_API_BASE_URL")  ?? "https://api.web7.oasisomniverse.one";
        private static string Web8BaseUrl  => Environment.GetEnvironmentVariable("WEB8_API_BASE_URL")  ?? "https://api.web8.oasisomniverse.one";
        private static string Web9BaseUrl  => Environment.GetEnvironmentVariable("WEB9_API_BASE_URL")  ?? "https://api.web9.oasisomniverse.one";
        private static string Web10BaseUrl => Environment.GetEnvironmentVariable("WEB10_API_BASE_URL") ?? "https://api.web10.oasisomniverse.one";

        [McpServerTool(Name = "web7_request"), Description("WEB7 (Symbiosis): calls any endpoint of the full WEB7 REST API (symbiosis sessions, bio-signal submission, collective consciousness spaces — see https://api.web7.oasisomniverse.one/swagger). path starts with /v1/...")]
        public static async Task<string> Web7Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
            => await ForwardRequestAsync(Web7BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

        [McpServerTool(Name = "web8_request"), Description("WEB8 (Galactic Mesh): calls any endpoint of the full WEB8 REST API (node registration, heartbeats, Dijkstra routing, message relay, protocol bridge — see https://api.web8.oasisomniverse.one/swagger). path starts with /v1/...")]
        public static async Task<string> Web8Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
            => await ForwardRequestAsync(Web8BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

        [McpServerTool(Name = "web9_request"), Description("WEB9 (Singularity): calls any endpoint of the full WEB9 REST API (unified health aggregation across Web4-Web8 — see https://api.web9.oasisomniverse.one/swagger). path starts with /v1/...")]
        public static async Task<string> Web9Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
            => await ForwardRequestAsync(Web9BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

        [McpServerTool(Name = "web10_request"), Description("WEB10 (The Source): calls any endpoint of the full WEB10 REST API (root OASIS identity and Web9 unified status — see https://api.web10.oasisomniverse.one/swagger). path starts with /v1/...")]
        public static async Task<string> Web10Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
            => await ForwardRequestAsync(Web10BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

        private static async Task<string> ForwardRequestAsync(string baseUrl, string httpMethod, string path, string? queryString, string? bodyJson, string? bearerToken)
        {
            string url = $"{baseUrl.TrimEnd('/')}{(path.StartsWith('/') ? path : "/" + path)}";

            if (!string.IsNullOrEmpty(queryString))
                url += queryString.StartsWith('?') ? queryString : $"?{queryString}";

            using HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(httpMethod.ToUpperInvariant()), url);

            if (!string.IsNullOrEmpty(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            if (!string.IsNullOrEmpty(bodyJson))
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Serialize(new
                {
                    statusCode = (int)response.StatusCode,
                    isSuccess = response.IsSuccessStatusCode,
                    body = TryParseJson(body)
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = true, message = ex.Message, url });
            }
        }

        /// <summary>Returns the response body as a parsed JSON element where possible, falling back to the raw string for non-JSON responses.</summary>
        private static object TryParseJson(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(body);
            }
            catch
            {
                return body;
            }
        }
    }
}
