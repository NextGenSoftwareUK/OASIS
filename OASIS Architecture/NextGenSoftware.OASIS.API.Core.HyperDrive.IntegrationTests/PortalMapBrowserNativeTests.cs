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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
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
    public async Task MapPopupCatalogueAndVisitUseActualSdkHttpAndOwnedMongo()
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
            var ownerBefore=await mongo.ReadNative(owner);var memberBefore=await mongo.ReadNative(member);
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
            Assert.NotNull(root);
            var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
                "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
            Assert.True(File.Exists(artifact), "Build the actual WEB4 API before the Map browser case.");
            var resolver = new AssemblyDependencyResolver(artifact);
            Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
            { var path = resolver.ResolveAssemblyToPath(name); return path == null ? null : context.LoadFromAssemblyPath(path); }
            AssemblyLoadContext.Default.Resolving += Resolve;
            try
            {
                var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
                var onode=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(artifact)!,"NextGenSoftware.OASIS.API.ONODE.Core.dll"));
                var mapType=onode.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Managers.MapManager",true)!;
                var locationType=onode.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Managers.MapLocation",true)!;
                HolonManager Data()=>new(mongo.Provider,routing.OASISDNA,routing);
                object Map(Guid id)=>Activator.CreateInstance(mapType,[Data(),id])!;
                var location=Activator.CreateInstance(locationType)!;var locationId=Guid.NewGuid();
                void Set(string property,object value)=>locationType.GetProperty(property)!.SetValue(location,value);
                Set("Id",locationId);Set("Name","Owned browser map destination");Set("Latitude",0d);Set("Longitude",0d);Set("IsActive",true);
                var row=(IHolon)locationType.GetMethod("ToHolon")!.Invoke(location,null)!;row.IsPublic=true;
                var seeded=await Data().SaveHolonAsync(row,owner,saveChildren:false);Assert.False(seeded.IsError,seeded.Message);Assert.True(seeded.IsSaved);
                var locationBefore=await mongo.ReadHolonDocumentNativeAsync(locationId);
                var scores=CompetitionManager.ForData(Data());
                var category=(string)typeof(CompetitionManager).GetMethod("GetLeaderboardCategory",BindingFlags.Instance|BindingFlags.NonPublic)!
                    .Invoke(scores,[CompetitionType.SocialActivity,SeasonType.Weekly])!;
                var broken=await Data().SaveSettingAsync(Guid.Empty,category,"entries","invalid owned weekly snapshot");Assert.False(broken.IsError,broken.Message);Assert.True(broken.Result);
                var weeklyId=(Guid)typeof(HolonManager).GetMethod("GetSettingsHolonId",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[Guid.Empty,category])!;
                var weeklyBefore=await mongo.ReadHolonDocumentNativeAsync(weeklyId);
                var controllers=new[]{api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.MapController",true)!};
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
                builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
                builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
                {
                    parts.ApplicationParts.Clear(); parts.ApplicationParts.Add(new AssemblyPart(api));
                    parts.FeatureProviders.Add(new ProfileControllerFeature(controllers));
                }).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
                builder.Services.AddSingleton<IControllerActivator>(new OwnedMapBrowserControllerActivator(controllers[0],context=>
                    Map(((IAvatar)context.HttpContext.Items["Avatar"]!).Id)));
                builder.Services.AddCors();
                builder.Services.AddAuthorization();
                await using var app = builder.Build();
                using var portal = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned portal required."));
                app.UseStaticFiles(new StaticFileOptions { FileProvider = portal });
                app.MapGet("/__map_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                    await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/map-browser-host.html")), "text/html"));
                var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
                var validation = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                    ValidateIssuer = true, ValidIssuer = "owned-map-fixture", ValidateAudience = true, ValidAudience = "owned-map-http",
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
                app.UseRouting();
                app.UseCors();
                app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
                try
                {
                    string Token(Guid id) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("owned-map-fixture", "owned-map-http",
                        [new Claim(ClaimTypes.NameIdentifier, id.ToString())], expires: DateTime.UtcNow.AddMinutes(5),
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var result=await ExchangeSdkRequest(new {baseUrl=address,token=Token(owner),avatarId=owner,
                        otherToken=Token(member),otherId=member,providerType="MongoDBOASIS",locationId},TimeSpan.FromMinutes(2));
                    Assert.False(result.RootElement.TryGetProperty("isError",out var error)&&error.GetBoolean(),
                        result.RootElement.TryGetProperty("message",out var message)?message.GetString():"Actual Map browser check failed.");
                    Assert.True(result.RootElement.GetProperty("mapPopupVerified").GetBoolean());
                    SameRevisions(ownerBefore,await mongo.ReadNative(owner));SameRevisions(memberBefore,await mongo.ReadNative(member));
                    Assert.Equal(locationBefore,await mongo.ReadHolonDocumentNativeAsync(locationId));
                    Assert.Equal(weeklyBefore,await mongo.ReadHolonDocumentNativeAsync(weeklyId));
                    foreach(var season in new[]{SeasonType.Daily,SeasonType.Monthly})
                    {
                        var board=await CompetitionManager.ForData(Data()).GetLeaderboardAsync(CompetitionType.SocialActivity,season);
                        Assert.False(board.IsError,board.Message);Assert.Equal(5,Assert.Single(board.Result).Score);
                    }
                    var historyTask=(Task)mapType.GetMethod("GetVisitHistoryAsync")!.Invoke(Map(owner),[owner,50,0])!;await historyTask;
                    using var history=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(historyTask.GetType().GetProperty("Result")!.GetValue(historyTask),new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
                    var visit=Assert.Single(history.RootElement.GetProperty("result").EnumerateArray());
                    Assert.Equal(locationId,visit.GetProperty("locationId").GetGuid());
                    Assert.Contains(locationId.ToString(),await mongo.ReadHolonDocumentNativeAsync(visit.GetProperty("id").GetGuid()));
                    Console.WriteLine("Map popup native browser verified: shipping SDK/popup, actual MVC/controller and owned Mongo; one persisted visit and warning, real zero-coordinate/search/history/stats, account isolation and HTTP400/401; profile/location snapshots unchanged.");
                }
                finally { using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await app.StopAsync(stop.Token); }
            }
            finally
            {
                AssemblyLoadContext.Default.Resolving -= Resolve;

            }
        }
        Console.WriteLine("Owned Map Mongo scope disposed and database absence verified.");
    }
    private sealed class OwnedMapBrowserControllerActivator(Type controllerType,Func<ControllerContext,object> manager) : IControllerActivator
    {
        public object Create(ControllerContext context)
        {
            var controller=Activator.CreateInstance(controllerType)!;
            controllerType.GetField("_mapManager",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(controller,manager(context));
            return controller;
        }
        public void Release(ControllerContext context,object controller)
        { if(controller is IDisposable disposable)disposable.Dispose(); }
    }

}
