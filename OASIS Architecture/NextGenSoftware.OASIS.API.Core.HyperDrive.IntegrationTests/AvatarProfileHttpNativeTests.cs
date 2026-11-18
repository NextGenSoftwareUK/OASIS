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
    private sealed class ProfileControllerFeature(Type controller) : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var candidate in feature.Controllers.ToArray())
                if (candidate.AsType() != controller) feature.Controllers.Remove(candidate);
        }
    }

    // Real compiled MVC actions, signed bearer validation and native account lookup.
    // This owned host does not claim to exercise the full production Startup/usage pipeline.
    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public async Task ProfileHttpUsesAuthenticatedActorAndCanonicalNativeRevisions(string mode)
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
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var type = api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SelfAvatarProfileController", true)!;
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
            {
                parts.ApplicationParts.Clear(); parts.ApplicationParts.Add(new AssemblyPart(api));
                parts.FeatureProviders.Add(new ProfileControllerFeature(type));
            }).AddControllersAsServices();
            builder.Services.AddTransient(type, _ => Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.NonPublic, null, [sdk], null)!);
            await using var app = builder.Build();
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
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
