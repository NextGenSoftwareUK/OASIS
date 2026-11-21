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
        using var session = await OpenUrbitNativeSessionAsync(includeMongo: false);
        var output = Path.Combine(session.Root, "TestResults/UrbitContactsInstallCurrent", session.Phase + "-web4.json");
        Assert.True(Directory.Exists(Path.GetDirectoryName(output)));
        Assert.False(File.Exists(output));
        var hash = await VerifyUrbitMvcAsync(session.Adapter, session.Owner, session.Expected,
            count => Assert.Equal(count, session.Observed.Calls));
        var proof = new {
            phase = session.Phase, nativeWeb4MvcQualified = true, ownerFullCount = 3, ownerSubsetCount = 2,
            anonymousStatus = 401, forgedFieldStatus = 400, foreignStatus = 403, inactiveStatus = 503,
            nativePointReadCount = session.Observed.Calls, successfulQueryReceipts = 2, rejectedQueryReceipts = 0,
            privateApiProjectionSha256 = hash, controlledIdentityAndOutbox = true,
            productionJwtQualified = false, durableBillingQualified = false, cqlQualified = false, liveDeployment = false
        };
        await using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, proof);
    }

    private sealed record UrbitNativeSession(string Root, string Phase, Guid Owner,
        IReadOnlyDictionary<string, UrbitContactsV1Client.ContactFields> Expected, UrbitNativePointHandler Observed,
        HttpClient Http, UrbitContactsV1Client Reader, UrbitContactBookQuerySource Adapter,
        string? MongoUri, string? Database, string? ResourceOwner) : IDisposable
    {
        public void Dispose() { Adapter.Dispose(); Reader.Dispose(); Http.Dispose(); }
    }

    private static async Task<UrbitNativeSession> OpenUrbitNativeSessionAsync(bool includeMongo)
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
        var expectedKeys = includeMongo ? new[] { "origin", "phase", "loginCode", "mongoUri", "mongoDatabase", "owner" }
            : new[] { "origin", "phase", "loginCode" };
        if (keys.Length != expectedKeys.Length || keys.Distinct().Count() != expectedKeys.Length || keys.Except(expectedKeys).Any())
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

        string? mongoUri = null, database = null, resourceOwner = null;
        if (includeMongo)
        {
            using var ready = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(root, "TestResults/UrbitContactsInstallCurrent/mongo-ready.json")));
            mongoUri = config.RootElement.GetProperty("mongoUri").GetString();
            database = config.RootElement.GetProperty("mongoDatabase").GetString();
            resourceOwner = config.RootElement.GetProperty("owner").GetString();
            var data = ready.RootElement;
            if (string.IsNullOrEmpty(mongoUri) || string.IsNullOrEmpty(database)
                || mongoUri != data.GetProperty("mongoUri").GetString() || database != data.GetProperty("database").GetString()
                || resourceOwner != data.GetProperty("owner").GetString() || !data.GetProperty("internalNetwork").GetBoolean()
                || !data.GetProperty("noPublishedPorts").GetBoolean() || !data.GetProperty("replicaReady").GetBoolean()
                || resourceOwner == null || !Regex.IsMatch(resourceOwner, @"\Aurbit-contacts-[0-9]+-[0-9]+\z"))
                throw new InvalidDataException("Owned replica metadata required.");
            var parts = resourceOwner.Split('-');
            if (database != "urbit_web4_" + parts[2] + "_" + parts[3])
                throw new InvalidDataException("Exact owned database required.");
        }
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
        var nativeHttp = new HttpClient(observed);
        var reader = new UrbitContactsV1Client(origin, nativeHttp);
        var adapter = new UrbitContactBookQuerySource(reader, owner, UrbitConnection, UrbitCids);
        try
        {
            var activation = await adapter.ActivateQuerySourceAsync();
            Assert.False(activation.IsError, "Owned native activation failed.");
            return new UrbitNativeSession(root, phase!, owner, expected, observed, nativeHttp, reader, adapter,
                mongoUri, database, resourceOwner);
        }
        catch { adapter.Dispose(); reader.Dispose(); nativeHttp.Dispose(); throw; }
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
