using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task ClanPopupUsesActualSdkHttpAndOwnedMongo()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        await using (var mongo = new MongoStore())
        {
            await mongo.Activate();
            var owner = Guid.NewGuid(); var member = Guid.NewGuid();
            IOASISStorageProvider[] stores = [mongo.Provider];
            await SeedRoutingStores(stores, owner); await SeedRoutingStores(stores, member);
            var routing = Routing(stores, false);
            Assert.False(routing.PublishRoutingOverrides(new ProviderRoutingOverrides(
                autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false),
                ProviderType.MongoDBOASIS).IsError);
            ClanManager Cold() => (ClanManager)Activator.CreateInstance(typeof(ClanManager), BindingFlags.Instance | BindingFlags.NonPublic,
                null, [routing.OASISDNA, routing, null], null)!;
            var search = (SearchManager)Activator.CreateInstance(typeof(SearchManager), BindingFlags.Instance | BindingFlags.NonPublic,
                null, [mongo.Provider, routing.OASISDNA, routing], null)!;
            var seeded = await Cold().CreateClanAsync(member, "Owned treasury clan", "Foreign clan state must be preserved.");
            Assert.False(seeded.IsError, seeded.Message); Assert.NotNull(seeded.Result);
            seeded.Result.Inventory.Add(new InventoryItem { Id = Guid.NewGuid(), Name = "Owned shared treasury item", Quantity = 0 });
            var treasury = await Cold().UpdateClanAsync(seeded.Result);
            Assert.False(treasury.IsError, treasury.Message);
            var foreignBefore = await mongo.ReadHolonDocumentNativeAsync(treasury.Result.Id);
            var ownerBefore = await mongo.ReadNative(owner); var memberBefore = await mongo.ReadNative(member);
            var ownerAccount = await mongo.Provider.LoadAvatarAsync(owner); var memberAccount = await mongo.Provider.LoadAvatarAsync(member);
            Assert.Equal(AvatarType.User, ownerAccount.Result.AvatarType.Value);
            Assert.Equal(AvatarType.User, memberAccount.Result.AvatarType.Value);
            var clanField = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var searchField = typeof(SearchManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previousClan = clanField.GetValue(null); var previousSearch = searchField.GetValue(null);
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
            Assert.NotNull(root);
            var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
                "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
            Assert.True(File.Exists(artifact), "Build the actual WEB4 API before the Clan browser case.");
            var resolver = new AssemblyDependencyResolver(artifact);
            Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
            { var path = resolver.ResolveAssemblyToPath(name); return path == null ? null : context.LoadFromAssemblyPath(path); }
            AssemblyLoadContext.Default.Resolving += Resolve;
            try
            {
                clanField.SetValue(null, Cold()); searchField.SetValue(null, search);
                var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
                var controllers = new[] { "ClanController", "AvatarProfileController" }
                    .Select(name => api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers." + name, true)!).ToArray();
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
                builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
                builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
                {
                    parts.ApplicationParts.Clear(); parts.ApplicationParts.Add(new AssemblyPart(api));
                    parts.FeatureProviders.Add(new ProfileControllerFeature(controllers));
                }).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
                builder.Services.AddAuthorization();
                await using var app = builder.Build();
                using var portal = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned portal required."));
                app.UseStaticFiles(new StaticFileOptions { FileProvider = portal });
                app.MapGet("/__clan_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                    await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/clan-browser-host.html")), "text/html"));
                var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
                var validation = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                    ValidateIssuer = true, ValidIssuer = "owned-clan-fixture", ValidateAudience = true, ValidAudience = "owned-clan-http",
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        var bearer = context.Request.Headers.Authorization.ToString();
                        if (!bearer.StartsWith("Bearer ", StringComparison.Ordinal)) { context.Response.StatusCode = 401; return; }
                        try
                        {
                            var principal = new JwtSecurityTokenHandler().ValidateToken(bearer[7..], validation, out _);
                            var id = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                            var saved = await mongo.Provider.LoadAvatarAsync(id);
                            if (saved.IsError || saved.Result?.Id != id) { context.Response.StatusCode = 401; return; }
                            context.User = principal; context.Items["Avatar"] = saved.Result;
                        }
                        catch (SecurityTokenException) { context.Response.StatusCode = 401; return; }
                    }
                    await next(context);
                });
                app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
                try
                {
                    string Token(Guid id) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("owned-clan-fixture", "owned-clan-http",
                        [new Claim(ClaimTypes.NameIdentifier, id.ToString())], expires: DateTime.UtcNow.AddMinutes(5),
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var result = await ExchangeSdkRequest(new { baseUrl = address, token = Token(owner), avatarId = owner,
                        memberToken = Token(member), memberId = member, memberUsername = memberAccount.Result.Username,
                        foreignClanId = treasury.Result.Id, providerType = "MongoDBOASIS" }, TimeSpan.FromMinutes(2));
                    Assert.False(result.RootElement.TryGetProperty("isError", out var error) && error.GetBoolean(),
                        result.RootElement.TryGetProperty("message", out var message) ? message.GetString() : "Actual Clan browser check failed.");
                    Assert.True(result.RootElement.GetProperty("clanPopupVerified").GetBoolean());
                    var createdId = result.RootElement.GetProperty("createdClanId").GetGuid();
                    var remaining = await Cold().ListClansAsync();
                    Assert.False(remaining.IsError, remaining.Message);
                    Assert.Equal(treasury.Result.Id, Assert.Single(remaining.Result).Id);
                    Assert.DoesNotContain(remaining.Result, item => item.Id == createdId);
                    Assert.Equal(foreignBefore, await mongo.ReadHolonDocumentNativeAsync(treasury.Result.Id));
                    SameRevisions(ownerBefore, await mongo.ReadNative(owner)); SameRevisions(memberBefore, await mongo.ReadNative(member));
                    Console.WriteLine("Clan popup native round trip verified: MongoDBOASIS; actual browser create/edit/membership/leave/delete and treasury; foreign clan and avatar revisions preserved.");
                }
                finally { using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await app.StopAsync(stop.Token); }
            }
            finally
            {
                clanField.SetValue(null, previousClan); searchField.SetValue(null, previousSearch);
                Assert.Same(previousClan, clanField.GetValue(null)); Assert.Same(previousSearch, searchField.GetValue(null));
                AssemblyLoadContext.Default.Resolving -= Resolve;
                Console.WriteLine("Owned Clan browser managers restored.");
            }
        }
        Console.WriteLine("Owned Clan Mongo scope disposed and database absence verified.");
    }
}
