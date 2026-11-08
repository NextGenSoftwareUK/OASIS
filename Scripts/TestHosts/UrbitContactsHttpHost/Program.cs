using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

// Cloud-only native qualification. Input is piped in memory, including the
// owned fake ship's login code. Never print request input or session cookies.
if (!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
    throw new InvalidOperationException("This native fixture runs only on the owned cloud Linux runner.");
var input = await Console.In.ReadToEndAsync();
if (System.Text.Encoding.UTF8.GetByteCount(input) > 8192)
    throw new InvalidDataException("Bounded native fixture input required.");
using var configuration = JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 4 });
var config = configuration.RootElement;
var fields = config.EnumerateObject().Select(p => p.Name).ToArray();
if (fields.Length != 4 || fields.Distinct(StringComparer.Ordinal).Count() != 4
    || fields.Except(new[] { "origin", "loginCode", "fixturePath", "phase" }, StringComparer.Ordinal).Any())
    throw new InvalidDataException("Exact native fixture input required.");
var origin = new Uri(config.GetProperty("origin").GetString()!, UriKind.Absolute);
if (origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.AbsolutePath != "/"
    || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
    throw new InvalidDataException("Only the owned published IPv4 loopback endpoint is admitted.");
var phase = config.GetProperty("phase").GetString();
if (phase is not ("beforeRestart" or "afterRestart"))
    throw new InvalidDataException("Explicit native lifecycle phase required.");
var loginCode = config.GetProperty("loginCode").GetString();
if (loginCode is not { Length: >= 8 and <= 128 } || loginCode.Any(c => !(c is >= 'a' and <= 'z' or '-')))
    throw new InvalidDataException("Owned fake ship login code has an invalid spelling.");
var fixtureBytes = await File.ReadAllBytesAsync(config.GetProperty("fixturePath").GetString()!);
if (Convert.ToHexString(SHA256.HashData(fixtureBytes)).ToLowerInvariant() != "53c7954266ddecb261bcda68637e02a16ef1d89651997a0ef4fa168f4d0b71bb")
    throw new InvalidDataException("Native teaching source fixture differs.");
using var fixture = JsonDocument.Parse(fixtureBytes);
if (fixture.RootElement.GetProperty("visibility").GetString() != "Private")
    throw new InvalidDataException("Private native source contract required.");

using var anonymous = UrbitContactsV1Client.FromSession(origin, new CookieContainer());
var denied = await anonymous.ReadBookPageAsync("0v1");
if (!denied.IsError || denied.Exception is not HttpRequestException { StatusCode: not null } nativeDenial)
    throw new InvalidDataException("Native private page was not denied to an unauthenticated HTTP reader.");
if (nativeDenial.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
    throw new InvalidDataException("Anonymous native response did not prove authentication denial.");

var cookies = new CookieContainer();
using (var login = new HttpClient(new HttpClientHandler {
    AllowAutoRedirect = false, UseProxy = false, UseCookies = true, CookieContainer = cookies
}) { Timeout = TimeSpan.FromSeconds(15) })
{
    using var form = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("password", loginCode) });
    using var response = await login.PostAsync(new Uri(origin, "/~/login"), form);
    response.EnsureSuccessStatusCode();
    if (cookies.GetCookies(origin).Count == 0)
        throw new InvalidDataException("Native login did not establish a session.");
}
using var client = UrbitContactsV1Client.FromSession(origin, cookies);
var pages = fixture.RootElement.GetProperty("pages").EnumerateArray().ToArray();
var ids = pages.Select(page => page.GetProperty("id").GetString()!).ToArray();
var subset = fixture.RootElement.GetProperty("subsetIds").EnumerateArray().Select(id => id.GetString()!).ToArray();
var fullRead = new List<UrbitContactsV1Client.BookPage>();
foreach (var selected in new[] { ids, subset })
{
    foreach (var id in selected)
    {
        var result = await client.ReadBookPageAsync(id);
        if (result.IsError || result.Result is null || result.LoadedCount != 1 || result.Result.RequestedId != id)
            throw new InvalidDataException("Native HTTP page read failed: " + result.ErrorCode);
        var expected = pages.Single(page => page.GetProperty("id").GetString() == id).GetProperty("overlay");
        var page = result.Result;
        if (page.Contact.Nickname is not null || page.Contact.Bio is not null || page.Contact.Status is not null
            || page.Overlay.Nickname != expected.GetProperty("nickname").GetString()
            || page.Overlay.Bio != expected.GetProperty("bio").GetString()
            || page.Overlay.Status != expected.GetProperty("status").GetString())
            throw new InvalidDataException("Native contact/overlay projection differs from the owned source.");
        if (ReferenceEquals(selected, ids)) fullRead.Add(page);
    }
}
if (fullRead.Count != 3 || subset.Length != 2)
    throw new InvalidDataException("Native three/two catalogue incomplete.");
var sourceHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(fullRead))).ToLowerInvariant();
Console.WriteLine(JsonSerializer.Serialize(new {
    phase, fullCount = 3, subsetCount = 2, anonymousDeniedStatus = (int)nativeDenial.StatusCode!.Value,
    projectedSourceSha256 = sourceHash, nativeHttpClientQualified = true,
    cqlQualified = false, liveDeployment = false
}));
