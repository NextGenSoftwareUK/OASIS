using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task RecordedRoutePopupUsesActualSdkHttpAndOwnedMongo()
        => await RunRecordedRouteBrowserHost(false);

    [Fact]
    public async Task MapRendererDisplaysSegmentedGpxInActualBrowser()
        => await RunRecordedRouteBrowserHost(true);

    private async Task RunRecordedRouteBrowserHost(bool rendererOnly)
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        await using var mongo = new MongoStore(); await mongo.Activate();
        var owner=Guid.NewGuid();var reader=Guid.NewGuid();IOASISStorageProvider[] stores=[mongo.Provider];
        await SeedRoutingStores(stores,owner);await SeedRoutingStores(stores,reader);
        var routing=Routing(stores,false);
        Assert.False(routing.PublishRoutingOverrides(new ProviderRoutingOverrides(autoFailoverEnabled:false,
            autoReplicationEnabled:false,autoLoadBalancingEnabled:false),ProviderType.MongoDBOASIS).IsError);
        var ownerBefore=await mongo.ReadNative(owner);var readerBefore=await mongo.ReadNative(reader);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Docker/oasis-dependency-versions.env")))root=root.Parent;
        Assert.NotNull(root);
        var configuration=typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var artifact=Path.Combine(root!.FullName,"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin",configuration,"net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact));var resolver=new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context,AssemblyName name)
        {var path=resolver.ResolveAssemblyToPath(name);return path==null?null:context.LoadFromAssemblyPath(path);}
        AssemblyLoadContext.Default.Resolving+=Resolve;
        try
        {
            var api=AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            var onode=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(artifact)!,"NextGenSoftware.OASIS.API.ONODE.Core.dll"));
            var map=api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.MapController",true)!;
            var routes=api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.MapRoutesController",true)!;
            var manager=onode.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Managers.MapManager",true)!;
            HolonManager Data()=>new(mongo.Provider,routing.OASISDNA,routing);
            var builder=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName="Development"});
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddControllers().ConfigureApplicationPartManager(parts=>
            {parts.ApplicationParts.Clear();parts.ApplicationParts.Add(new AssemblyPart(api));parts.FeatureProviders.Add(new ProfileControllerFeature([map,routes]));});
            builder.Services.AddSingleton<IControllerActivator>(new OwnedRouteBrowserControllerActivator(context=>
            {
                var type=context.ActionDescriptor.ControllerTypeInfo.AsType();var controller=Activator.CreateInstance(type)!;
                var actor=((IAvatar)context.HttpContext.Items["Avatar"]!).Id;
                if(type==map)map.GetField("_mapManager",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(controller,Activator.CreateInstance(manager,[Data(),actor]));
                else routes.GetField("data",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(controller,Data());
                return controller;
            }));
            builder.Services.AddAuthorization();builder.Services.AddCors();await using var app=builder.Build();
            using var portal=new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT")??throw new InvalidOperationException("Pinned Portal required."));
            app.UseStaticFiles(new StaticFileOptions{FileProvider=portal});
            app.MapGet("/__map_browser",async()=>Microsoft.AspNetCore.Http.Results.Content(await File.ReadAllTextAsync(Path.Combine(root.FullName,"Scripts/TestHosts/Fixtures/map-browser-host.html")),"text/html"));
            var key=new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var validation=new TokenValidationParameters{ValidateIssuerSigningKey=true,IssuerSigningKey=key,
                ValidateIssuer=true,ValidIssuer="owned-route-fixture",ValidateAudience=true,ValidAudience="owned-route-http",ValidateLifetime=true,ClockSkew=TimeSpan.Zero};
            app.Use(async(context,next)=>
            {
                if(context.Request.Path.StartsWithSegments("/api"))
                {
                    var bearer=context.Request.Headers.Authorization.ToString();
                    if(!bearer.StartsWith("Bearer ",StringComparison.Ordinal)){context.Response.StatusCode=401;return;}
                    try
                    {
                        var principal=new JwtSecurityTokenHandler().ValidateToken(bearer[7..],validation,out _);
                        var actor=Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                        var loaded=await mongo.Provider.LoadAvatarAsync(actor);
                        if(loaded.IsError||loaded.Result?.Id!=actor){context.Response.StatusCode=401;return;}
                        context.User=principal;context.Items["Avatar"]=loaded.Result;
                    }
                    catch(SecurityTokenException){context.Response.StatusCode=401;return;}
                }
                await next(context);
            });
            app.UseRouting();app.UseCors();app.UseAuthorization();app.MapControllers();await app.StartAsync();
            try
            {
                string Token(Guid id)=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("owned-route-fixture","owned-route-http",
                    [new Claim(ClaimTypes.NameIdentifier,id.ToString())],expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256)));
                var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var result=await ExchangeSdkRequest(new{baseUrl=address,token=Token(owner),avatarId=owner,otherToken=Token(reader),otherId=reader,providerType="MongoDBOASIS"},TimeSpan.FromMinutes(2));
                Assert.False(result.RootElement.TryGetProperty("isError",out var error)&&error.GetBoolean(),
                    result.RootElement.TryGetProperty("message",out var message)?message.GetString():"Route browser failed.");
                if(rendererOnly)
                {
                    Assert.True(result.RootElement.GetProperty("mapRendererVerified").GetBoolean());
                    SameRevisions(ownerBefore,await mongo.ReadNative(owner));SameRevisions(readerBefore,await mongo.ReadNative(reader));
                    Console.WriteLine("MAP_RENDERER_BROWSER_NATIVE_PROOF: actual shipping MapLibre/GPX controls, native WebGL canvas, visible route pixels and navigation, zero mutations and unchanged avatar revisions.");
                    return;
                }
                Assert.True(result.RootElement.GetProperty("recordedRoutePopupVerified").GetBoolean());
                var routeId=result.RootElement.GetProperty("routeId").GetGuid();var geometryId=result.RootElement.GetProperty("geometryId").GetGuid();
                var header=await Data().LoadHolonForWriteAsync(routeId,owner);var geometry=await Data().LoadHolonForWriteAsync(geometryId,owner);
                Assert.False(header.IsError,header.Message);Assert.False(geometry.IsError,geometry.Message);
                Assert.False(header.Result.Record.IsPublic);Assert.False(geometry.Result.Record.IsPublic);
                Assert.Equal(owner,header.Result.Record.CreatedByAvatarId);Assert.Equal(routeId,geometry.Result.Record.ParentHolonId);
                Assert.Equal("Shared morning walk",header.Result.Record.Name);
                using var summary=JsonDocument.Parse(header.Result.Record.MetaData["payload"].ToString()!);
                Assert.Equal("Our World",summary.RootElement.GetProperty("SourceApp").GetString());
                Assert.Equal("owned-browser-walk",summary.RootElement.GetProperty("SourceRouteId").GetString());
                using var points=JsonDocument.Parse(geometry.Result.Record.MetaData["payload"].ToString()!);
                Assert.Equal(2,points.RootElement.GetArrayLength());
                Assert.All(points.RootElement.EnumerateArray(),segment=>Assert.Equal(2,segment.GetArrayLength()));
                Assert.Equal(0,points.RootElement[0][0].GetProperty("Latitude").GetDouble());
                Assert.Equal("2026-11-21T00:00:00.1234567Z",points.RootElement[0][0].GetProperty("Utc").GetString());
                SameRevisions(ownerBefore,await mongo.ReadNative(owner));SameRevisions(readerBefore,await mongo.ReadNative(reader));
                Console.WriteLine("RECORDED_ROUTE_BROWSER_NATIVE_PROOF: actual popup/SDK/MVC/owned Mongo create, reopen, public reader, rename/revoke, provider revisions, precise timestamps and avatar guards passed.");
            }
            finally{using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await app.StopAsync(stop.Token);}
        }
        finally{AssemblyLoadContext.Default.Resolving-=Resolve;}
    }
    private sealed class OwnedRouteBrowserControllerActivator(Func<ControllerContext,object> create):IControllerActivator
    {
        public object Create(ControllerContext context)=>create(context);
        public void Release(ControllerContext context,object controller){if(controller is IDisposable disposable)disposable.Dispose();}
    }
}
