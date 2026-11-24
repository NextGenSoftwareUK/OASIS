using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>Calls <c>GET api/messaging/sync</c>. The supplied HttpClient carries the avatar's bearer session.</summary>
    public sealed class HttpEdgeMessagingApi : IEdgeMessagingApi
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        private readonly HttpClient _client;
        private readonly string _path;

        public HttpEdgeMessagingApi(HttpClient client, string syncPath = "api/messaging/sync")
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            if (_client.BaseAddress == null) throw new ArgumentException("HttpClient.BaseAddress is required.", nameof(client));
            _path = syncPath;
        }

        public async Task<OASISResult<EdgeServerSyncPage>> SyncAsync(DateTime? since, int limit, CancellationToken cancellationToken)
        {
            var result = new OASISResult<EdgeServerSyncPage>();
            var url = $"{_path}?limit={limit}" + (since.HasValue
                ? "&since=" + Uri.EscapeDataString(since.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)) : "");
            try
            {
                using (var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var envelope = string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<OASISResult<EdgeServerSyncPage>>(body, Json);
                    if (!response.IsSuccessStatusCode || envelope == null || envelope.IsError || envelope.Result == null)
                        return Fail(result, "MESSAGING_SYNC_FAILED",
                            envelope?.Message ?? $"The messaging sync endpoint returned HTTP {(int)response.StatusCode}.", null);
                    return envelope;
                }
            }
            catch (HttpRequestException ex) { return Fail(result, "MESSAGING_UNREACHABLE", "The hosted messaging API is unreachable.", ex); }
            catch (JsonException ex) { return Fail(result, "MESSAGING_INVALID_JSON", "The messaging response was not valid JSON.", ex); }
        }

        private static OASISResult<EdgeServerSyncPage> Fail(OASISResult<EdgeServerSyncPage> r, string code, string message, Exception ex)
        {
            r.IsError = true; r.ErrorCode = code; r.Message = message; r.Exception = ex;
            return r;
        }
    }
}
