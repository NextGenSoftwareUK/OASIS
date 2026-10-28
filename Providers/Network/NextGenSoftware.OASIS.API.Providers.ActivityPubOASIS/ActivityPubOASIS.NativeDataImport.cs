using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

/// <summary>
/// Reads the signed-in account's own social data from its Mastodon-compatible instance so NativeDataImportManager can
/// holonize it. The sources are <c>statuses</c>, <c>followers</c> and <c>following</c>; each item is a record keyed by its
/// native id and paged with the instance's own <c>max_id</c> cursor. Only the configured instance is ever contacted, and
/// an access token is required because the data belongs to the account.
/// </summary>
public partial class ActivityPubOASIS : INativeDataImportProvider
{
    private static readonly Regex NextLink = new Regex("<([^>]+)>\\s*;\\s*rel=\"next\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly IReadOnlyDictionary<string, (string Kind, int PageCap)> NativeSources = new Dictionary<string, (string, int)>
    {
        ["statuses"] = ("statuses", 40),
        ["followers"] = ("accounts", 80),
        ["following"] = ("accounts", 80)
    };

    public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.ActivityPubOASIS;
    public string DescribeNativeConnection(NativeConnection connection)
    {
        if (connection == null) return null;
        return "activitypub://" + ParseNativeConnection(connection).Instance.Host.ToLowerInvariant();
    }

    private static readonly string[] ConnectionSettings = { "instanceUrl", "accessToken" };

    /// <summary>The handler used to reach a caller's server. Replaced in tests; by default it checks every address before connecting.</summary>
    internal static Func<System.Net.Http.HttpMessageHandler> NativeConnectionHandlerFactory { get; set; } = NativeConnectionGuard.CreateGuardedHttpHandler;

    private static (Uri Instance, string Token) ParseNativeConnection(NativeConnection connection)
    {
        connection.RequireOnly(ConnectionSettings);
        if (!Uri.TryCreate(connection.Require("instanceUrl"), UriKind.Absolute, out var instance) || instance.Scheme != Uri.UriSchemeHttps)
            throw new NativeConnectionRefusedException("instanceUrl must be the https address of your Mastodon-compatible server, for example https://mastodon.social.");
        return (instance, connection.Require("accessToken"));
    }

    /// <summary>A short-lived reader for the caller's own server. Its handler checks every address before connecting.</summary>
    private static ActivityPubOASIS OpenNativeInstance(NativeConnection connection)
    {
        var (instance, token) = ParseNativeConnection(connection);
        try { return new ActivityPubOASIS(instance.GetLeftPart(UriPartial.Authority), token, NativeConnectionHandlerFactory()); }
        catch (ArgumentException cause) { throw new NativeConnectionRefusedException(cause.Message); }
    }

    public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
        try
        {
            if (connection != null)
            {
                using var instance = OpenNativeInstance(connection);
                return await instance.ListNativeDataSourcesAsync(null, cancellationToken);
            }
            var account = await VerifyNativeAccountAsync(cancellationToken);
            var sources = new List<NativeDataSource>
            {
                new NativeDataSource("statuses", "statuses", Count(account, "statuses_count")),
                new NativeDataSource("followers", "accounts", Count(account, "followers_count")),
                new NativeDataSource("following", "accounts", Count(account, "following_count"))
            };
            result.Result = sources;
            result.Message = $"3 native sources for @{account.GetProperty("acct").GetString()}.";
        }
        catch (Exception cause) when (cause is InvalidOperationException or HttpRequestException or InvalidDataException or JsonException)
        {
            OASISErrorHandling.HandleError(ref result, $"Listing native ActivityPub sources failed: {NativeConnectionGuard.Explain(cause)}", cause);
            result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_LIST_FAILED";
        }
        return result;
    }

    public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
        NativeConnection connection = null, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<NativeRecordPage>();
        try
        {
            if (connection != null)
            {
                using var instance = OpenNativeInstance(connection);
                return await instance.ReadNativeRecordsAsync(sourceName, continuationToken, pageSize, null, cancellationToken);
            }
            if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
            if (!NativeSources.TryGetValue(sourceName ?? "", out var source))
                throw new ArgumentException($"'{sourceName}' is not a native ActivityPub source (statuses, followers or following).");
            if (continuationToken != null && (continuationToken.Length == 0 || continuationToken.Length > 128 || continuationToken.Any(c => c < '0' || c > '9')))
                throw new ArgumentException("A native decimal max_id cursor is required.");

            var account = await VerifyNativeAccountAsync(cancellationToken);
            var id = account.GetProperty("id").GetString();
            var path = $"/api/v1/accounts/{Uri.EscapeDataString(id)}/{sourceName}?limit={Math.Min(pageSize, source.PageCap)}"
                       + (continuationToken == null ? "" : "&max_id=" + continuationToken);
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (json.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("The instance returned a non-array page.");

            var records = json.RootElement.EnumerateArray()
                .Select(item => source.Kind == "statuses" ? ToStatusRecord(sourceName, item) : ToAccountRecord(sourceName, item)).ToList();
            var duplicate = records.GroupBy(record => record.NativeKey).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null) throw new InvalidDataException($"The instance returned id {duplicate.Key} twice in one page.");

            result.Result = new NativeRecordPage(records, NextCursor(response));
        }
        catch (Exception cause) when (cause is InvalidOperationException or HttpRequestException or InvalidDataException or ArgumentException or JsonException or KeyNotFoundException)
        {
            OASISErrorHandling.HandleError(ref result, $"Reading native ActivityPub '{sourceName}' failed: {NativeConnectionGuard.Explain(cause)}", cause);
            result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
        }
        return result;
    }

    internal static NativeRecord ToStatusRecord(string sourceName, JsonElement status)
    {
        var fields = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["content"] = Text(status, "content"),
            ["url"] = Text(status, "url"),
            ["uri"] = Text(status, "uri"),
            ["created_at"] = Text(status, "created_at"),
            ["visibility"] = Text(status, "visibility"),
            ["language"] = Text(status, "language"),
            ["spoiler_text"] = Text(status, "spoiler_text"),
            ["in_reply_to_id"] = Text(status, "in_reply_to_id"),
            ["sensitive"] = Flag(status, "sensitive"),
            ["replies_count"] = Number(status, "replies_count"),
            ["reblogs_count"] = Number(status, "reblogs_count"),
            ["favourites_count"] = Number(status, "favourites_count"),
            ["media_count"] = status.TryGetProperty("media_attachments", out var media) && media.ValueKind == JsonValueKind.Array ? (long)media.GetArrayLength() : 0L
        };
        return new NativeRecord(sourceName, Id(status), fields);
    }

    internal static NativeRecord ToAccountRecord(string sourceName, JsonElement account)
    {
        var fields = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["username"] = Text(account, "username"),
            ["acct"] = Text(account, "acct"),
            ["display_name"] = Text(account, "display_name"),
            ["note"] = Text(account, "note"),
            ["url"] = Text(account, "url"),
            ["created_at"] = Text(account, "created_at"),
            ["bot"] = Flag(account, "bot"),
            ["locked"] = Flag(account, "locked"),
            ["followers_count"] = Number(account, "followers_count"),
            ["following_count"] = Number(account, "following_count"),
            ["statuses_count"] = Number(account, "statuses_count")
        };
        return new NativeRecord(sourceName, Id(account), fields);
    }

    /// <summary>The <c>max_id</c> of the instance's own next link, or null on the last page. Links to other hosts are refused.</summary>
    internal string NextCursor(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values)) return null;
        var matches = values.SelectMany(value => NextLink.Matches(value).Select(match => match.Groups[1].Value)).ToList();
        if (matches.Count == 0) return null;
        if (matches.Count > 1) throw new InvalidDataException("The instance sent more than one next link.");
        if (!Uri.TryCreate(new Uri(_instanceUrl), matches[0], out var next)
            || !string.Equals(next.GetLeftPart(UriPartial.Authority), _instanceUrl, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The instance's next link points outside the configured instance.");

        var cursor = System.Web.HttpUtility.ParseQueryString(next.Query)["max_id"];
        if (string.IsNullOrEmpty(cursor) || cursor.Length > 128 || cursor.Any(c => c < '0' || c > '9'))
            throw new InvalidDataException("The instance's next link has no valid max_id cursor.");
        return cursor;
    }

    private async Task<JsonElement> VerifyNativeAccountAsync(CancellationToken cancellationToken)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ActivityPubOASIS));
        if (string.IsNullOrEmpty(_accessToken))
            throw new InvalidOperationException("Native ActivityPub import reads the signed-in account's own data and needs an access token.");
        using var response = await _httpClient.GetAsync("/api/v1/accounts/verify_credentials", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object || Text(root, "id") == null)
            throw new InvalidDataException("The instance did not return an account id.");
        return root.Clone();
    }

    private static string Id(JsonElement item)
    {
        var id = Text(item, "id");
        if (string.IsNullOrEmpty(id) || id.Length > 128 || id.Any(c => c < '0' || c > '9'))
            throw new InvalidDataException("The instance returned an item without a decimal id.");
        return id;
    }

    private static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static object Flag(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    private static object Number(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static long? Count(JsonElement item, string name) => Number(item, name) as long?;
}
