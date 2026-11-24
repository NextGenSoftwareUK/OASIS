using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Http
{
    /// <summary>
    /// Sends one JSON request to a map vendor with a deadline, no redirects and a
    /// response size bound, and returns the parsed document or the vendor's own error
    /// message. Nothing is retried: a failed call is reported, not repeated.
    /// </summary>
    public sealed class MapHttpClient
    {
        public const int DefaultMaxResponseBytes = 4 * 1024 * 1024;

        private static readonly HttpClient SharedClient = new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        })
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };

        private readonly HttpClient _client;

        public MapHttpClient(string serviceName, HttpClient client = null)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("A service name is required.", nameof(serviceName));
            ServiceName = serviceName;
            _client = client ?? SharedClient;
        }

        public string ServiceName { get; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxResponseBytes { get; set; } = DefaultMaxResponseBytes;
        public string UserAgent { get; set; } = "OASIS-Maps/2.0 (+https://oasisomniverse.one)";

        /// <summary>
        /// Sends the request. <paramref name="readVendorError"/> extracts the vendor's
        /// error text from a non-success JSON body; it may return null.
        /// </summary>
        public async Task<OASISResult<JsonDocument>> SendAsync(HttpRequestMessage request,
            Func<JsonElement, string> readVendorError, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(Timeout);

            try
            {
                request.Headers.UserAgent.ParseAdd(UserAgent);
                request.Headers.Accept.ParseAdd("application/json");

                using HttpResponseMessage response = await _client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);

                byte[] body = await ReadBoundedAsync(response.Content, deadline.Token).ConfigureAwait(false);
                string text = new UTF8Encoding(false, true).GetString(body);

                if (!response.IsSuccessStatusCode)
                {
                    string detail = TryReadVendorError(text, readVendorError);
                    return MapResults.Error<JsonDocument>(ServiceName + " returned HTTP " + (int)response.StatusCode
                        + (detail == null ? "." : ": " + detail));
                }

                string mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0)
                    return MapResults.Error<JsonDocument>(ServiceName + " returned " + (mediaType.Length == 0 ? "no content type" : mediaType) + " instead of JSON.");

                return MapResults.Ok(JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 }));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return MapResults.Error<JsonDocument>(ServiceName + " did not respond within " + Timeout.TotalSeconds + " seconds.");
            }
            catch (HttpRequestException ex)
            {
                return MapResults.Error<JsonDocument>(ServiceName + " could not be reached: " + ex.Message);
            }
            catch (Exception ex) when (ex is JsonException || ex is DecoderFallbackException || ex is InvalidDataException)
            {
                return MapResults.Error<JsonDocument>(ServiceName + " returned an unreadable response: " + ex.Message);
            }
        }

        private async Task<byte[]> ReadBoundedAsync(HttpContent content, CancellationToken token)
        {
            if (content.Headers.ContentLength > MaxResponseBytes)
                throw new InvalidDataException("response exceeds " + MaxResponseBytes + " bytes.");

            using Stream stream = await content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[16384];
            int count;

            while ((count = await stream.ReadAsync(chunk.AsMemory(), token).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > MaxResponseBytes)
                    throw new InvalidDataException("response exceeds " + MaxResponseBytes + " bytes.");
                buffer.Write(chunk, 0, count);
            }

            return buffer.ToArray();
        }

        private static string TryReadVendorError(string text, Func<JsonElement, string> readVendorError)
        {
            if (readVendorError == null || string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(text);
                return readVendorError(document.RootElement);
            }
            catch (JsonException)
            {
                return text.Length > 300 ? text.Substring(0, 300) : text;
            }
        }
    }
}
