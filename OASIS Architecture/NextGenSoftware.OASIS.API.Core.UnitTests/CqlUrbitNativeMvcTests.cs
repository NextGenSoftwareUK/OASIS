using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class UrbitNativeWeb4FactAttribute : FactAttribute
{
    public UrbitNativeWeb4FactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_URBIT_NATIVE_WEB4_INPUT") == null)
            Skip = "Explicit owned native fixture required; controlled runs are not native proof.";
    }
}

public partial class CqlV1Web4Tests
{
    [UrbitNativeWeb4Fact]
    public async Task UrbitNativeWeb4MvcReadsOwnedBook()
    {
        if (!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
            throw new InvalidOperationException("Owned cloud native fixture required.");
        const string inputName = "COSMIC_URBIT_NATIVE_WEB4_INPUT";
        var input = Environment.GetEnvironmentVariable(inputName);
        Environment.SetEnvironmentVariable(inputName, null);
        if (input == null || Encoding.UTF8.GetByteCount(input) > 4096)
            throw new InvalidDataException("Bounded private native input required.");
        using var config = JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 4 });
        var keys = config.RootElement.EnumerateObject().Select(field => field.Name).ToArray();
        if (keys.Length != 3 || keys.Distinct().Count() != 3 || keys.Except(new[] { "origin", "phase", "loginCode" }).Any())
            throw new InvalidDataException("Exact native input required.");
        var originText = config.RootElement.GetProperty("origin").GetString();
        var phase = config.RootElement.GetProperty("phase").GetString();
        var loginCode = config.RootElement.GetProperty("loginCode").GetString();
        if (originText != "http://127.0.0.1:80/" || phase is not ("beforeRestart" or "afterRestart")
            || loginCode == null || loginCode.Length is < 8 or > 128 || !Regex.IsMatch(loginCode, @"\A[a-z-]+\z"))
            throw new InvalidDataException("Owned loopback phase/session input required.");
        var origin = new Uri(originText);
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var fixturePath = Path.Combine(root, "Providers/Other/TestProjects/NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests/Fixtures/urbit-contacts-book-source.json");
        var raw = await File.ReadAllBytesAsync(fixturePath);
        if (Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant() != "75ac02bb8c0ad875ad227248cfe810441aa9f6550de39031e7381062b17293cb")
            throw new InvalidDataException("Pinned synthetic fixture required.");
        using var fixture = JsonDocument.Parse(raw);
        var pages = fixture.RootElement.GetProperty("pages").EnumerateArray().ToArray();
        Assert.Equal("Private", fixture.RootElement.GetProperty("visibility").GetString());
        Assert.Equal(UrbitCids, pages.Select(page => page.GetProperty("id").GetString()).ToArray());
        var owner = fixture.RootElement.GetProperty("sourceOwner").GetGuid();
        var expected = pages.ToDictionary(page => page.GetProperty("id").GetString()!, page => {
            var overlay = page.GetProperty("overlay");
            return new UrbitContactsV1Client.ContactFields(overlay.GetProperty("nickname").GetString(),
                overlay.GetProperty("bio").GetString(), overlay.GetProperty("status").GetString());
        });
        var output = Path.Combine(root, "TestResults/UrbitContactsInstallCurrent", phase + "-web4.json");
        Assert.True(Directory.Exists(Path.GetDirectoryName(output)));
        Assert.False(File.Exists(output));

        var cookies = new CookieContainer();
        using (var login = new HttpClient(new HttpClientHandler {
            AllowAutoRedirect = false, UseProxy = false, UseCookies = true, CookieContainer = cookies
        }) { Timeout = TimeSpan.FromSeconds(15) })
        {
            using var form = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("password", loginCode) });
            using var response = await login.PostAsync(new Uri(origin, "/~/login"), form);
            response.EnsureSuccessStatusCode();
            if (cookies.GetCookies(origin).Count == 0)
                throw new InvalidOperationException("Native login did not establish a session.");
        }
        var observed = new UrbitNativePointHandler(origin, cookies);
        using var nativeHttp = new HttpClient(observed);
        using var reader = new UrbitContactsV1Client(origin, nativeHttp);
        using var adapter = new UrbitContactBookQuerySource(reader, owner, UrbitConnection, UrbitCids);
        var activation = await adapter.ActivateQuerySourceAsync();
        Assert.False(activation.IsError, "Owned native activation failed.");
        var hash = await VerifyUrbitMvcAsync(adapter, owner, expected, count => Assert.Equal(count, observed.Calls));
        var proof = new {
            phase, nativeWeb4MvcQualified = true, ownerFullCount = 3, ownerSubsetCount = 2,
            anonymousStatus = 401, forgedFieldStatus = 400, foreignStatus = 403, inactiveStatus = 503,
            nativePointReadCount = observed.Calls, successfulQueryReceipts = 2, rejectedQueryReceipts = 0,
            privateApiProjectionSha256 = hash, controlledIdentityAndOutbox = true,
            productionJwtQualified = false, durableBillingQualified = false, cqlQualified = false, liveDeployment = false
        };
        await using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, proof);
    }

    private sealed class UrbitNativePointHandler : DelegatingHandler
    {
        private readonly Uri origin;
        internal int Calls;
        internal UrbitNativePointHandler(Uri origin, CookieContainer cookies) : base(new HttpClientHandler {
            AllowAutoRedirect = false, UseProxy = false, UseCookies = true, CookieContainer = cookies
        }) { this.origin = origin; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || request.Content != null || uri == null
                || uri.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority)
                || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || !UrbitCids.Any(cid => uri.AbsolutePath == "/~/scry/contacts/v1/book/id/" + cid + ".json"))
                throw new InvalidOperationException("Owned native transport requires exact selected book reads.");
            Interlocked.Increment(ref Calls);
            return base.SendAsync(request, token);
        }
    }
}
