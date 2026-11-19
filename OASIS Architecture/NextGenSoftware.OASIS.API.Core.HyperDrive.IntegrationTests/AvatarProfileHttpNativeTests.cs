using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed class ProfileControllerFeature : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly Type[] _controllers;
        public ProfileControllerFeature(params Type[] controllers) => _controllers = controllers;
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var candidate in feature.Controllers.ToArray())
                if (!_controllers.Contains(candidate.AsType())) feature.Controllers.Remove(candidate);
        }
    }

    // Real compiled MVC actions, signed bearer validation and native account lookup.
    // This owned host does not claim to exercise the full production Startup/usage pipeline.
    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public Task ProfileHttpUsesAuthenticatedActorAndCanonicalNativeRevisions(string mode) => RunProfileHttp(mode, false);

    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public Task ProfileJavaScriptSdkUsesActualNativeHttp(string mode) => RunProfileHttp(mode, true);

    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public Task ProfileBrowserModalUsesActualNativeHttp(string mode) => RunProfileHttp(mode, true, true);

    [Fact]
    public Task PortraitBrowserUploadUsesActualHttpAndThreeNativeProviders() =>
        RunProfileHttp(HyperDriveModes.V2, true, true, portraitBrowser: true);

    private async Task RunProfileHttp(string mode, bool sdkOnly, bool browser = false, bool portraitBrowser = false)
    {
        RequireOwnedFixtures();
        var id = Guid.NewGuid();
        await using var mongo = new MongoStore(); await mongo.Activate();
        await using var bolt = new BoltStore(); await bolt.Activate(id);
        using var sqlite = new SqliteStore();
        Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider[] stores = [mongo.Provider, sqlite.Provider, bolt.Provider];
        await SeedRoutingStores(stores, id);
        var manager = Routing(stores, true, mode);
        var sdk = new AvatarManager(null, manager.OASISDNA, manager);

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env")))
            root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var project = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var artifact = Path.Combine(project, "bin", configuration, "net10.0", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual WEB4 API before this test.");
        Assert.True(File.GetLastWriteTimeUtc(artifact) >= File.GetLastWriteTimeUtc(
            Path.Combine(project, "Controllers/SelfAvatarProfileController.cs")), "API artifact is stale.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            var path = resolver.ResolveAssemblyToPath(name);
            return path == null ? null : context.LoadFromAssemblyPath(path);
        }
        FieldInfo? portraitManagerField = null;
        object? previousPortraitManager = null;
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var type = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SelfAvatarProfileController", true)!;
            Type? portraitController = null;
            if (portraitBrowser)
            {
                portraitController = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.AvatarProfileController", true)!;
                portraitManagerField = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Program", true)!
                    .GetField("_avatarManager", BindingFlags.Static | BindingFlags.NonPublic)!;
                Assert.NotNull(portraitManagerField);
                previousPortraitManager = portraitManagerField.GetValue(null);
                portraitManagerField.SetValue(null, sdk);
            }
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            if (portraitBrowser) builder.Configuration["OASIS:UseTestDataWhenLiveDataNotAvailable"] = "false";
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
            {
                parts.ApplicationParts.Clear(); parts.ApplicationParts.Add(new AssemblyPart(api));
                parts.FeatureProviders.Add(new ProfileControllerFeature(portraitController == null ? [type] : [type, portraitController]));
            }).AddControllersAsServices();
            builder.Services.AddTransient(type, _ => Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.NonPublic, null, [sdk], null)!);
            await using var app = builder.Build();
            using var portalFiles = browser
                ? new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT")
                    ?? throw new InvalidOperationException("Pinned portal checkout required."))
                : null;
            if (browser)
            {
                app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions { FileProvider = portalFiles });
                app.MapGet("/__profile_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                    await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/profile-browser-host.html")),
                    "text/html"));
            }
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var validation = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                ValidateIssuer = true, ValidIssuer = "owned-profile-fixture",
                ValidateAudience = true, ValidAudience = "owned-profile-http",
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            };
            app.Use(async (context, next) =>
            {
                var auth = context.Request.Headers.Authorization.ToString();
                if (auth.StartsWith("Bearer ", StringComparison.Ordinal))
                {
                    try
                    {
                        var principal = new JwtSecurityTokenHandler().ValidateToken(auth[7..], validation, out _);
                        var actor = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                        var stored = await stores[0].LoadAvatarAsync(actor);
                        if (!stored.IsError && stored.Result?.Id == actor)
                        {
                            context.User = principal;
                            context.Items["Avatar"] = stored.Result;
                        }
                    }
                    catch (SecurityTokenException) { context.Response.StatusCode = 401; return; }
                }
                await next(context);
            });
            app.MapControllers();
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features
                    .Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/avatar/profile")).StatusCode);
                string Token(Guid actor) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                    issuer: "owned-profile-fixture", audience: "owned-profile-http",
                    claims: [new Claim("sub", actor.ToString("D"))], expires: DateTime.UtcNow.AddMinutes(5),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(id));
                foreach (var store in stores)
                {
                    var provider = store.ProviderType.Value;
                    if (sdkOnly)
                    {
                        if (browser)
                        {
                            var selected = manager.PublishRoutingOverrides(new NextGenSoftware.OASIS.API.Core.Configuration.ProviderRoutingOverrides(), provider);
                            Assert.False(selected.IsError, selected.Message);
                        }
                        if (portraitBrowser)
                        {
                            var before = await Read(store, id);
                            var previousPortrait = (await store.LoadAvatarDetailAsync(id)).Result.Portrait;
                            using var uploaded = await ExchangeSdkRequest(new
                                { baseUrl = address, token = Token(id), providerType = provider.ToString(), imageId = Array.IndexOf(stores, store) + 1 });
                            Assert.False(uploaded.RootElement.TryGetProperty("isError", out var portraitError) && portraitError.GetBoolean(),
                                "Actual portrait browser/HTTP verification failed.");
                            Assert.True(uploaded.RootElement.GetProperty("portraitBrowserVerified").GetBoolean());
                            var image = uploaded.RootElement.GetProperty("imageBase64").GetString();
                            Assert.NotEqual(previousPortrait, image);
                            var detail = await store.LoadAvatarDetailAsync(id);
                            Assert.False(detail.IsError, detail.Message);
                            Assert.Equal(image, detail.Result.Portrait);
                            Assert.Equal(42, detail.Result.XP);
                            Assert.Equal("stored-hash", (await store.LoadAvatarAsync(id)).Result.Password);
                            var after = await Read(store, id);
                            Assert.Equal(before.Title, after.Title);
                            Assert.Equal(before.FirstName, after.FirstName);
                            Assert.Equal(before.LastName, after.LastName);
                            Assert.Equal(before.Username, after.Username);
                            Assert.Equal(before.Email, after.Email);
                            Assert.Equal(before.Address, after.Address);
                            Console.WriteLine("Portrait native persistence verified: " + provider +
                                "; actual browser upload/read/reload, unchanged profile fields, password and XP.");
                            continue;
                        }
                        var sdkTitle = (browser ? "Browser " : "SDK ") + mode + " " + provider;
                        using var sdkResult = await ExchangeSdkRequest(new
                            { baseUrl = address, token = Token(id), providerType = provider.ToString(), title = sdkTitle });
                        Assert.False(sdkResult.RootElement.TryGetProperty("isError", out var sdkError) && sdkError.GetBoolean(),
                            "Actual JavaScript SDK/native HTTP verification failed.");
                        var sdkNative = await Read(store, id);
                        Assert.Equal(sdkTitle, sdkNative.Title); Assert.Equal(string.Empty, sdkNative.Address);
                        if (browser) Assert.True(sdkResult.RootElement.GetProperty("browserVerified").GetBoolean());
                        Assert.Equal(sdkNative.AvatarRevision.VersionId, sdkResult.RootElement.GetProperty("avatarVersionId").GetGuid());
                        Assert.Equal(sdkNative.DetailRevision.VersionId, sdkResult.RootElement.GetProperty("detailVersionId").GetGuid());
                        foreach (var replica in stores)
                        {
                            var actual = await Read(replica, id); SameRevisions(sdkNative, actual);
                            Assert.Equal(sdkTitle, actual.Title); Assert.Equal(string.Empty, actual.Address);
                            if (browser)
                            {
                                Assert.Equal(sdkResult.RootElement.GetProperty("username").GetString(), actual.Username);
                                Assert.Equal(sdkResult.RootElement.GetProperty("email").GetString(), actual.Email);
                                Assert.Equal(sdkResult.RootElement.GetProperty("firstName").GetString(), actual.FirstName);
                                Assert.Equal(sdkResult.RootElement.GetProperty("lastName").GetString(), actual.LastName);
                            }
                            Assert.Equal("stored-hash", (await replica.LoadAvatarAsync(id)).Result.Password);
                            Assert.Equal(42, (await replica.LoadAvatarDetailAsync(id)).Result.XP);
                        }
                        continue;
                    }
                    var get = await client.GetAsync("/api/avatar/profile?providerType=" + provider);
                    Assert.Equal(HttpStatusCode.OK, get.StatusCode);
                    Assert.True(get.Headers.CacheControl?.NoStore);
                    var openingText = await get.Content.ReadAsStringAsync();
                    Assert.DoesNotContain("stored-hash", openingText);
                    Assert.DoesNotContain("password", openingText, StringComparison.OrdinalIgnoreCase);
                    using var opening = JsonDocument.Parse(openingText);
                    var document = opening.RootElement.GetProperty("result");
                    Assert.Equal(provider.ToString(), document.GetProperty("providerType").GetString());
                    var profile = document.GetProperty("profile");
                    var title = "HTTP " + mode + " " + provider;
                    var body = JsonSerializer.Serialize(new
                    {
                        avatarRevision = profile.GetProperty("avatarRevision"),
                        detailRevision = profile.GetProperty("detailRevision"),
                        changes = new { title, address = (string?)null },
                        providerType = provider.ToString()
                    });
                    async Task<HttpResponseMessage> Patch(string json) => await client.PatchAsync(
                        "/api/avatar/profile", new StringContent(json, Encoding.UTF8, "application/json"));
                    using var bad = await Patch(body.Insert(1, "\"avatarId\":\"" + Guid.NewGuid() + "\","));
                    Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
                    using var saved = await Patch(body);
                    Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
                    Assert.True(saved.Headers.CacheControl?.NoStore);
                    var savedText = await saved.Content.ReadAsStringAsync();
                    Assert.DoesNotContain("stored-hash", savedText);
                    Assert.DoesNotContain("password", savedText, StringComparison.OrdinalIgnoreCase);
                    using var result = JsonDocument.Parse(savedText);
                    Assert.False(result.RootElement.GetProperty("isError").GetBoolean());
                    Assert.True(result.RootElement.GetProperty("isSaved").GetBoolean());
                    var confirmed = result.RootElement.GetProperty("result").GetProperty("profile");
                    Assert.Equal(title, confirmed.GetProperty("title").GetString());
                    Assert.Equal(string.Empty, confirmed.GetProperty("address").GetString());
                    var native = await Read(store, id);
                    foreach (var replica in stores)
                    {
                        var actual = await Read(replica, id);
                        SameRevisions(native, actual);
                        Assert.Equal(title, actual.Title); Assert.Equal(string.Empty, actual.Address);
                        var account = await replica.LoadAvatarAsync(id);
                        var detail = await replica.LoadAvatarDetailAsync(id);
                        Assert.Equal("stored-hash", account.Result.Password);
                        Assert.Equal(42, detail.Result.XP);
                    }
                    Assert.Equal(native.AvatarRevision.VersionId,
                        confirmed.GetProperty("avatarRevision").GetProperty("versionId").GetGuid());
                    using var stale = await Patch(body);
                    Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
                    using var staleJson = JsonDocument.Parse(await stale.Content.ReadAsStringAsync());
                    Assert.Equal("AVATAR_PROFILE_REVISION_CONFLICT",
                        staleJson.RootElement.GetProperty("errorCode").GetString());
                    using var reloaded = await client.GetAsync("/api/avatar/profile?providerType=" + provider);
                    using var reload = JsonDocument.Parse(await reloaded.Content.ReadAsStringAsync());
                    Assert.Equal(title, reload.RootElement.GetProperty("result").GetProperty("profile").GetProperty("title").GetString());
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(Guid.NewGuid()));
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/avatar/profile")).StatusCode);
                Assert.Equal(mode, manager.OASISDNA.OASIS.HyperDriveMode);
            }
            finally
            {
                using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await app.StopAsync(stop.Token);
            }
        }
        finally
        {
            if (portraitManagerField != null)
            {
                portraitManagerField.SetValue(null, previousPortraitManager);
                Assert.Same(previousPortraitManager, portraitManagerField.GetValue(null));
                Console.WriteLine("Owned portrait HTTP application manager restored.");
            }
            AssemblyLoadContext.Default.Resolving -= Resolve;
        }
    }

    // The test hosts HTTP only. CI owns the separate fixed Node command.
    private static async Task<JsonDocument> ExchangeSdkRequest(object request, TimeSpan? handoffTimeout = null)
    {
        var timeout = handoffTimeout ?? TimeSpan.FromSeconds(45);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(3))
            throw new ArgumentOutOfRangeException(nameof(handoffTimeout));
        var configured = Environment.GetEnvironmentVariable("OASIS_PROFILE_SDK_HANDOFF_ROOT");
        var runnerTemp = Environment.GetEnvironmentVariable("RUNNER_TEMP");
        if (string.IsNullOrEmpty(configured) || string.IsNullOrEmpty(runnerTemp))
            throw new InvalidOperationException("Owned CI SDK handoff paths are required.");
        var root = Path.GetFullPath(configured);
        var parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(runnerTemp)) + Path.DirectorySeparatorChar;
        if (!root.StartsWith(parent, StringComparison.Ordinal) ||
            !new DirectoryInfo(root).Name.StartsWith("oasis-profile-sdk-", StringComparison.Ordinal) ||
            !Directory.Exists(root))
            throw new InvalidOperationException("The SDK handoff must remain inside its owned CI directory.");
        var stem = Guid.NewGuid().ToString("N");
        var input = Path.Combine(root, stem + ".request.json");
        var output = Path.Combine(root, stem + ".response.json");
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(root, stem + ".response.json")
            { NotifyFilter = NotifyFilters.FileName };
        watcher.Created += (_, _) => ready.TrySetResult(true);
        watcher.Renamed += (_, _) => ready.TrySetResult(true);
        watcher.Error += (_, error) => ready.TrySetException(error.GetException());
        watcher.EnableRaisingEvents = true;
        try
        {
            var options = new FileStreamOptions
                { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            await using (var stream = new FileStream(input + ".tmp", options))
            {
                await JsonSerializer.SerializeAsync(stream, request);
                await stream.FlushAsync();
            }
            File.Move(input + ".tmp", input);
            await ready.Task.WaitAsync(timeout);
            return JsonDocument.Parse(await File.ReadAllTextAsync(output));
        }
        finally
        {
            foreach (var owned in new[] { input + ".tmp", input, output + ".tmp", output })
                File.Delete(owned);
        }
    }
}
