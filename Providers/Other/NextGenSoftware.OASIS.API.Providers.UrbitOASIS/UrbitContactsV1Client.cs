using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

/// <summary>Current Tlon contacts v1 reads, separate from the legacy graph/contact-store provider.</summary>
public sealed class UrbitContactsV1Client : IDisposable
{
    public sealed record Contact(string RequestedShip, string? Nickname, string? Bio, string? Status);
    public sealed record ContactFields(string? Nickname, string? Bio, string? Status);
    /// <summary>HasNativeFields is false for both an empty existing page and the
    /// native missing-page default; it is not an existence certificate.</summary>
    public sealed record BookPage(string RequestedId, ContactFields Contact, ContactFields Overlay, bool HasNativeFields);
    private readonly HttpClient http;
    private readonly Uri origin;
    private readonly bool ownsClient;
    private bool disposed;
    private sealed class PayloadLimitException : IOException { }

    /// <summary>The caller owns authentication and must disable redirects on its supplied transport.</summary>
    public UrbitContactsV1Client(Uri origin, HttpClient http, bool ownsClient = false)
    {
        if (origin == null || !origin.IsAbsoluteUri || origin.UserInfo.Length != 0
            || origin.Query.Length != 0 || origin.Fragment.Length != 0 || origin.AbsolutePath != "/"
            || !(origin.Scheme == "https" || origin.Scheme == "http" && origin.IsLoopback))
            throw new ArgumentException("One HTTPS origin or explicit HTTP loopback fixture is required.", nameof(origin));
        this.origin = origin;
        this.http = http ?? throw new ArgumentNullException(nameof(http));
        this.ownsClient = ownsClient;
    }

    /// <summary>Use an existing authenticated session. Login and writes are outside this read client.</summary>
    public static UrbitContactsV1Client FromSession(Uri origin, CookieContainer cookies)
    {
        var client = new HttpClient(new HttpClientHandler {
            AllowAutoRedirect = false, UseProxy = false, UseCookies = true,
            CookieContainer = cookies ?? throw new ArgumentNullException(nameof(cookies))
        });
        try { return new(origin, client, ownsClient: true); }
        catch { client.Dispose(); throw; }
    }

    private static void UniqueObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Contact JSON object required.");
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in value.EnumerateObject())
            if (!fields.Add(field.Name)) throw new InvalidDataException("Duplicate contact property.");
    }

    private static string? Text(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var field)) return null;
        UniqueObject(field);
        var type = field.GetProperty("type").GetString();
        if (type == "null" && !field.TryGetProperty("value", out _)) return null;
        if (type != "text" || field.GetProperty("value").ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Contact text field does not match the v1 tagged schema.");
        var value = field.GetProperty("value").GetString()!;
        if (value.Length > 16384) throw new PayloadLimitException();
        return value;
    }

    private static OASISResult<T> Error<T>(string code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private static ContactFields Fields(JsonElement value)
    {
        UniqueObject(value);
        return new(Text(value, "nickname"), Text(value, "bio"), Text(value, "status"));
    }

    public Task<OASISResult<Contact>> ReadContactAsync(string ship, CancellationToken cancellationToken = default)
    {
        // Path-safe spelling only; native @p identity qualification remains separate.
        if (ship == null || ship.Length > 64 || !Regex.IsMatch(ship,
            "\\A~(?:[a-z]{3}|[a-z]{6}(?:-{1,2}[a-z]{6}){0,7})\\z", RegexOptions.CultureInvariant))
            return Task.FromResult(Error<Contact>("URBIT_CONTACT_INVALID_INPUT", "One path-safe ship spelling is required."));
        return ReadAsync("/~/scry/contacts/v1/contact/" + Uri.EscapeDataString(ship) + ".json", value => {
            var fields = Fields(value);
            return new Contact(ship, fields.Nickname, fields.Bio, fields.Status);
        }, "URBIT_CONTACT", cancellationToken);
    }

    /// <summary>One explicitly selected local book page. Contact and overlay stay separate.</summary>
    public Task<OASISResult<BookPage>> ReadBookPageAsync(string id, CancellationToken cancellationToken = default)
    {
        // Bounded @uv spelling; the native agent remains authoritative for CID existence/type.
        if (!IsBookPageId(id))
            return Task.FromResult(Error<BookPage>("URBIT_CONTACT_PAGE_INVALID_INPUT", "One path-safe book CID is required."));
        return ReadAsync("/~/scry/contacts/v1/book/id/" + Uri.EscapeDataString(id) + ".json", value => {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2)
                throw new InvalidDataException("Contacts v1 page must contain exactly contact and overlay objects.");
            var contact = Fields(value[0]);
            var overlay = Fields(value[1]);
            return new BookPage(id, contact, overlay,
                value[0].EnumerateObject().MoveNext() || value[1].EnumerateObject().MoveNext());
        }, "URBIT_CONTACT_PAGE", cancellationToken);
    }

    internal static bool IsBookPageId(string? id) => id is { Length: <= 64 }
        && Regex.IsMatch(id, "\\A0v(?:0|[1-9a-v][0-9a-v]{0,4}(?:\\.[0-9a-v]{5})*)\\z", RegexOptions.CultureInvariant);

    private async Task<OASISResult<T>> ReadAsync<T>(string path, Func<JsonElement, T> project,
        string errorPrefix, CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var target = new Uri(origin, path);
            using var request = new HttpRequestMessage(HttpMethod.Get, target);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode != HttpStatusCode.OK || !string.Equals(response.Content.Headers.ContentType?.MediaType,
                    "application/json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unexpected contact response status or media type.");
            if (response.Content.Headers.ContentLength is long size && size > 64 * 1024) throw new PayloadLimitException();
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, deadline.Token)) > 0)
            {
                if (body.Length + count > 64 * 1024) throw new PayloadLimitException();
                body.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            var contact = project(document.RootElement);
            cancellationToken.ThrowIfCancellationRequested();
            return new(contact) { IsLoaded = true, LoadedCount = 1 };
        }
        catch (OperationCanceledException error)
        { return Error<T>(errorPrefix + (cancellationToken.IsCancellationRequested ? "_CANCELLED" : "_TIMEOUT"), "Contact read did not complete.", error); }
        catch (PayloadLimitException error)
        { return Error<T>(errorPrefix + "_LIMIT", "Contact response exceeds its bounded budget.", error); }
        catch (Exception error)
        { return Error<T>(errorPrefix + "_READ_FAILED", "The configured contacts v1 read failed.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ownsClient) http.Dispose();
    }
}
