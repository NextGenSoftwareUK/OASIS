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

    public async Task<OASISResult<Contact>> ReadContactAsync(string ship, CancellationToken cancellationToken = default)
    {
        OASISResult<Contact> Error(string code, string message, Exception? error = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            // This is a bounded, path-safe spelling check. Native @p validation
            // and source identity still require actual ship qualification.
            if (ship == null || ship.Length > 64 || !Regex.IsMatch(ship,
                "\\A~(?:[a-z]{3}|[a-z]{6}(?:-{1,2}[a-z]{6}){0,7})\\z", RegexOptions.CultureInvariant))
                return Error("URBIT_CONTACT_INVALID_INPUT", "One canonical path-safe ship spelling is required.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var target = new Uri(origin, "/~/scry/contacts/v1/contact/" + Uri.EscapeDataString(ship) + ".json");
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
            UniqueObject(document.RootElement);
            var contact = new Contact(ship, Text(document.RootElement, "nickname"),
                Text(document.RootElement, "bio"), Text(document.RootElement, "status"));
            cancellationToken.ThrowIfCancellationRequested();
            return new(contact) { IsLoaded = true, LoadedCount = 1 };
        }
        catch (OperationCanceledException error)
        { return Error(cancellationToken.IsCancellationRequested ? "URBIT_CONTACT_CANCELLED" : "URBIT_CONTACT_TIMEOUT", "Contact read did not complete.", error); }
        catch (PayloadLimitException error)
        { return Error("URBIT_CONTACT_LIMIT", "Contact response exceeds its bounded budget.", error); }
        catch (Exception error)
        { return Error("URBIT_CONTACT_READ_FAILED", "The configured contacts v1 read failed.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ownsClient) http.Dispose();
    }
}
