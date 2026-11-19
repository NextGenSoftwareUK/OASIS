using System.Collections;
using System.IdentityModel.Tokens.Jwt;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task SubscriptionAdminCatalogueUsesActualWeb6BrowserAndHolonStorage()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var web4Artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration, "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        var web6Artifact = Path.Combine(root.FullName, "WEB6/NextGenSoftware.OASIS.Web6.WebAPI/bin", configuration, "net10.0/NextGenSoftware.OASIS.Web6.WebAPI.dll");
        Assert.True(File.Exists(web4Artifact) && File.Exists(web6Artifact), "Build both actual API artifacts before this test.");
        var resolvers = new[] { new AssemblyDependencyResolver(web6Artifact), new AssemblyDependencyResolver(web4Artifact) };
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var resolver in resolvers)
            {
                var path = resolver.ResolveAssemblyToPath(name);
                if (path != null) return context.LoadFromAssemblyPath(path);
            }
            return null;
        }
        var mongo = new MongoClient(MongoUri);
        var databaseName = "subscription_catalogue_" + Guid.NewGuid().ToString("N");
        Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
        var database = mongo.GetDatabase(databaseName);
        var avatarField = typeof(AvatarManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        var holonField = typeof(HolonManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previousAvatar = avatarField.GetValue(null); var previousHolon = holonField.GetValue(null);
        var previousDna = OASISDNAManager.OASISDNA;
        var previousActor = OASISRequestContext.CurrentAvatarId;
        var previousCurrentAvatar = OASISRequestContext.CurrentAvatar;
        var previousPath = Environment.GetEnvironmentVariable("WEB6_MODEL_CATALOGUE_PATH");
        var previousSystem = Environment.GetEnvironmentVariable("WEB6_SYSTEM_AVATAR_ID");
        var previousAdminKey = Environment.GetEnvironmentVariable("WEB6_ADMIN_API_KEY");
        MongoDBOASIS? provider = null;
        Type? catalogueType = null; object? originalCatalogue = null; object? originalError = null;
        object? versions = null; object[] versionEntries = [];
        object? auditQueue = null; object[] auditEntries = [];
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            provider = new MongoDBOASIS(MongoUri, databaseName);
            var activated = await provider.ActivateProviderAsync(); Assert.False(activated.IsError, activated.Message);
            var routing = Routing([provider], false, HyperDriveModes.Legacy);
            routing.OASISDNA.OASIS.Security = new SecuritySettings { SecretKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) };
            var actors = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
            routing.OASISDNA.OASIS.OASISSystemAccountId = actors[0].ToString("D");
            OASISDNAManager.OASISDNA = routing.OASISDNA;
            var avatars = new AvatarManager(provider, routing.OASISDNA, routing);
            var holons = new HolonManager(provider, routing.OASISDNA, routing);
            avatarField.SetValue(null, avatars); holonField.SetValue(null, holons);
            foreach (var (id, index) in actors.Select((id, index) => (id, index)))
            {
                var saved = await provider.SaveAvatarAsync(new Avatar { Id = id, Username = "catalogue-" + id.ToString("N"),
                    Email = id.ToString("N") + "@example.test", IsActive = true,
                    AvatarType = new EnumValue<AvatarType>(index == 0 ? AvatarType.Wizard : AvatarType.User) });
                Assert.False(saved.IsError, saved.Message);
                var read = await provider.LoadAvatarAsync(id); Assert.False(read.IsError, read.Message);
                Assert.Equal(index == 0 ? AvatarType.Wizard : AvatarType.User, read.Result.AvatarType.Value);
            }
            OASISRequestContext.CurrentAvatarId = actors[0];
            var owner = await avatars.LoadAvatarAsync(actors[0]); Assert.False(owner.IsError, owner.Message);
            OASISRequestContext.CurrentAvatar = owner.Result;
            var bootstrap = Path.Combine(root.FullName, "WEB6/NextGenSoftware.OASIS.Web6.Core/Configuration/web6-model-catalogue.json");
            Assert.True(File.Exists(bootstrap));
            Environment.SetEnvironmentVariable("WEB6_MODEL_CATALOGUE_PATH", bootstrap);
            Environment.SetEnvironmentVariable("WEB6_SYSTEM_AVATAR_ID", actors[0].ToString("D"));
            Environment.SetEnvironmentVariable("WEB6_ADMIN_API_KEY", null);
            var web4 = AssemblyLoadContext.Default.LoadFromAssemblyPath(web4Artifact);
            var web6 = AssemblyLoadContext.Default.LoadFromAssemblyPath(web6Artifact);
            var web6Core = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("NextGenSoftware.OASIS.Web6.Core"));
            catalogueType = web6Core.GetType("NextGenSoftware.OASIS.Web6.Core.Managers.ModelCatalogueManager", true)!;
            originalCatalogue = catalogueType.GetMethod("GetActiveCatalogue")!.Invoke(null, null)!;
            originalError = catalogueType.GetField("PublishedCatalogueError", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
            versions = catalogueType.GetField("Versions", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            versionEntries = ((IEnumerable)versions).Cast<object>().ToArray();
            var runtime = web6Core.GetType("NextGenSoftware.OASIS.Web6.Core.RuntimeConfig", true)!;
            auditQueue = runtime.GetField("_auditLog", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            auditEntries = ((IEnumerable)auditQueue).Cast<object>().ToArray();
            var originalVersion = (string)originalCatalogue.GetType().GetProperty("Version")!.GetValue(originalCatalogue)!;
            const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["SUBSCRIPTION_MONGODB_CONNECTION_STRING"] = MongoUri, ["SUBSCRIPTION_MONGODB_DATABASE"] = databaseName,
                ["SUBSCRIPTION_LEDGER_INITIAL_STATE"] = "empty-new-installation" }).Build();
            var repositoryType = web4.GetType(ns + "MongoSubscriptionUsageRepository", true)!;
            var repository = Activator.CreateInstance(repositoryType, [config])!;
            var web4Controller = web4.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionAdminController", true)!;
            var web6Controller = web6.GetType("NextGenSoftware.OASIS.Web6.WebAPI.Controllers.AdminController", true)!;
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton(repositoryType, repository);
            builder.Services.AddControllers().ConfigureApplicationPartManager(parts => {
                parts.ApplicationParts.Clear(); parts.ApplicationParts.Add(new AssemblyPart(web4)); parts.ApplicationParts.Add(new AssemblyPart(web6));
                parts.FeatureProviders.Add(new ProfileControllerFeature(web4Controller, web6Controller));
            }).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
            await using var app = builder.Build();
            using var portalFiles = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned portal required."));
            app.UseStaticFiles(new StaticFileOptions { FileProvider = portalFiles });
            app.MapGet("/__subscription_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/subscription-admin-browser-host.html")), "text/html"));
            app.UseMiddleware(web6.GetType("NextGenSoftware.OASIS.Web6.WebAPI.Middleware.JwtMiddleware", true)!);
            app.Use(async (context, next) => {
                if (context.Items["AvatarId"] is Guid id)
                {
                    var read = await avatars.LoadAvatarAsync(id);
                    if (read.IsError || read.Result == null) { context.Response.StatusCode = 503; return; }
                    context.Items["Avatar"] = read.Result;
                    OASISRequestContext.CurrentAvatar = read.Result;
                }
                await next(context);
            });
            app.UseAuthorization(); app.MapControllers();
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(routing.OASISDNA.OASIS.Security.SecretKey));
                var issuer = string.IsNullOrEmpty(routing.OASISDNA.OASIS.Security.Oidc?.Issuer) ? "OASIS" : routing.OASISDNA.OASIS.Security.Oidc.Issuer;
                string Token(Guid actor) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: issuer, audience: issuer,
                    claims: [new Claim("id", actor.ToString("D")), new Claim("sub", actor.ToString("D"))],
                    expires: DateTime.UtcNow.AddMinutes(actor == actors[2] ? -1 : 5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                using var result = await ExchangeSdkRequest(new { baseUrl = address, originalVersion,
                    actors = actors.Select(actor => new { id = actor.ToString("D"), token = Token(actor) }).ToArray() });
                Assert.False(result.RootElement.TryGetProperty("isError", out var failed) && failed.GetBoolean(), "Actual WEB6 catalogue browser check failed.");
                Assert.True(result.RootElement.GetProperty("subscriptionAdminCatalogueBrowserVerified").GetBoolean());
                Assert.Equal(3, result.RootElement.GetProperty("actorsVerified").GetInt32());
                Assert.True(result.RootElement.GetProperty("lateSessionResponseIgnored").GetBoolean());
                var published = result.RootElement.GetProperty("publishedVersion").GetString()!;
                Console.WriteLine("Subscription catalogue browser evidence: " + result.RootElement.GetRawText());
                OASISRequestContext.CurrentAvatarId = actors[0];
                var settings = await holons.GetAllSettingsAsync(actors[0], "web6-model-catalogue");
                Assert.False(settings.IsError, settings.Message);
                Assert.Equal(published, settings.Result["activeVersion"].ToString());
                Assert.True(settings.Result.ContainsKey("version:" + originalVersion));
                Assert.Equal(2, settings.Result.Keys.Count(key => key.StartsWith("version:", StringComparison.Ordinal)));
                using var persisted = JsonDocument.Parse(settings.Result["version:" + published].ToString()!);
                Assert.Equal(actors[0].ToString("D"), persisted.RootElement.GetProperty("CreatedBy").GetString());
                Assert.Equal("Owned browser catalogue publication", persisted.RootElement.GetProperty("Reason").GetString());
                var documentType = originalCatalogue.GetType();
                var stored = JsonSerializer.Deserialize(settings.Result["version:" + published].ToString()!, documentType)!;
                foreach (var field in new[] { "Models", "UnitPrices" })
                    Assert.Equal(JsonSerializer.Serialize(documentType.GetProperty(field)!.GetValue(originalCatalogue)),
                        JsonSerializer.Serialize(documentType.GetProperty(field)!.GetValue(stored)));
                catalogueType.GetField("_catalogue", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, originalCatalogue);
                versions.GetType().GetMethod("Clear")!.Invoke(versions, null);
                await (Task)catalogueType.GetMethod("LoadPublishedAsync")!.Invoke(null, [actors[0]])!;
                Assert.Equal(published, catalogueType.GetProperty("CatalogueVersion")!.GetValue(null));
                var audit = ((IEnumerable)runtime.GetMethod("GetAuditLog")!.Invoke(null, null)!).Cast<object>().ToArray();
                var publication = Assert.Single(audit.Where(entry => (string)entry.GetType().GetProperty("Action")!.GetValue(entry)! == "PublishModelCatalogue"));
                Assert.Equal(actors[0].ToString("D"), publication.GetType().GetProperty("Actor")!.GetValue(publication));
                Console.WriteLine("Subscription catalogue native final state verified: original approved model/unit prices unchanged, bootstrap and one new immutable version persisted through actual HolonManager/Mongo, exact Wizard actor/reason and one publication audit, active version reload from native settings after cache reset. No multi-host publication, provider execution, Stripe or full production Startup/hosted acceptance.");
            }
            finally { await app.StopAsync(); }
        }
        finally
        {
            if (catalogueType != null && originalCatalogue != null)
            {
                catalogueType.GetField("_catalogue", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, originalCatalogue);
                catalogueType.GetField("PublishedCatalogueError", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, originalError);
                versions!.GetType().GetMethod("Clear")!.Invoke(versions, null);
                foreach (var pair in versionEntries)
                    versions.GetType().GetMethod("TryAdd")!.Invoke(versions, [pair.GetType().GetProperty("Key")!.GetValue(pair), pair.GetType().GetProperty("Value")!.GetValue(pair)]);
            }
            if (auditQueue != null)
            {
                auditQueue.GetType().GetMethod("Clear")!.Invoke(auditQueue, null);
                foreach (var entry in auditEntries) auditQueue.GetType().GetMethod("Enqueue")!.Invoke(auditQueue, [entry]);
            }
            avatarField.SetValue(null, previousAvatar); holonField.SetValue(null, previousHolon);
            OASISDNAManager.OASISDNA = previousDna; OASISRequestContext.CurrentAvatarId = previousActor; OASISRequestContext.CurrentAvatar = previousCurrentAvatar;
            Environment.SetEnvironmentVariable("WEB6_MODEL_CATALOGUE_PATH", previousPath);
            Environment.SetEnvironmentVariable("WEB6_SYSTEM_AVATAR_ID", previousSystem);
            Environment.SetEnvironmentVariable("WEB6_ADMIN_API_KEY", previousAdminKey);
            AssemblyLoadContext.Default.Resolving -= Resolve;
            if (provider != null) { var stopped = await provider.DeActivateProviderAsync(); Assert.False(stopped.IsError, stopped.Message); }
            Assert.StartsWith("subscription_catalogue_", databaseName);
            await mongo.DropDatabaseAsync(databaseName);
            Assert.DoesNotContain(databaseName, await (await mongo.ListDatabaseNamesAsync()).ToListAsync());
            Console.WriteLine("Owned catalogue database absence verified; native provider, manager/DNA/request/catalogue/audit/process state restored.");
        }
    }
}
