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
    public async Task KarmaPopupsUseActualSdkHttpAndOwnedMongo()
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
            var detail=await mongo.Provider.LoadAvatarDetailAsync(owner);
            Assert.False(detail.IsError,detail.Message);Assert.NotNull(detail.Result);
            detail.Result.KarmaAkashicRecords.Clear();
            var now=DateTime.UtcNow;
            for(var i=0;i<51;i++)detail.Result.KarmaAkashicRecords.Add(new KarmaAkashicRecord {
                AvatarId=owner,Date=now.AddSeconds(-i),Karma=i==0?7:i==1?0:1,
                KarmaSourceTitle="Owned Karma event "+i,
                KarmaEarntOrLost=new NextGenSoftware.Utilities.EnumValue<KarmaEarntOrLost>(i==0?KarmaEarntOrLost.Lost:KarmaEarntOrLost.Earnt),
                Provider=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.MongoDBOASIS)
            });
            var saved=await mongo.Provider.SaveAvatarDetailAsync(detail.Result);Assert.False(saved.IsError,saved.Message);
            var ownerBefore=await mongo.ReadNative(owner);var memberBefore=await mongo.ReadNative(member);
            FieldInfo? avatarField=null;object? previousAvatar=null;
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
            Assert.NotNull(root);
            var configuration = typeof(AvatarProfileCommitStampNativeTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
                "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
            Assert.True(File.Exists(artifact), "Build the actual WEB4 API before the Karma browser case.");
            var resolver = new AssemblyDependencyResolver(artifact);
            Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
            { var path = resolver.ResolveAssemblyToPath(name); return path == null ? null : context.LoadFromAssemblyPath(path); }
            AssemblyLoadContext.Default.Resolving += Resolve;
            try
            {
                var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
                avatarField=api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Program",true)!.GetField("_avatarManager",BindingFlags.Static|BindingFlags.NonPublic)!;
                previousAvatar=avatarField.GetValue(null);
                avatarField.SetValue(null,Activator.CreateInstance(typeof(AvatarManager),BindingFlags.Instance|BindingFlags.NonPublic,null,[mongo.Provider,routing.OASISDNA,routing],null));
                var controllers = new[] { "KarmaController" }
                    .Select(name => api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers." + name, true)!).ToArray();
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
                builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
                builder.Services.AddControllers().ConfigureApplicationPartManager(parts =>
                {
                    parts.ApplicationParts.Clear(); parts.ApplicationParts.Add(new AssemblyPart(api));
                    parts.FeatureProviders.Add(new ProfileControllerFeature(controllers));
                }).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
                builder.Services.AddCors();
                builder.Services.AddAuthorization();
                await using var app = builder.Build();
                using var portal = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Environment.GetEnvironmentVariable("OASIS_PROFILE_PORTAL_ROOT") ?? throw new InvalidOperationException("Pinned portal required."));
                app.UseStaticFiles(new StaticFileOptions { FileProvider = portal });
                app.MapGet("/__karma_browser", async () => Microsoft.AspNetCore.Http.Results.Content(
                    await File.ReadAllTextAsync(Path.Combine(root.FullName, "Scripts/TestHosts/Fixtures/karma-browser-host.html")), "text/html"));
                var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
                var validation = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                    ValidateIssuer = true, ValidIssuer = "owned-karma-fixture", ValidateAudience = true, ValidAudience = "owned-karma-http",
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
                    string Token(Guid id) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("owned-karma-fixture", "owned-karma-http",
                        [new Claim(ClaimTypes.NameIdentifier, id.ToString())], expires: DateTime.UtcNow.AddMinutes(5),
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var result=await ExchangeSdkRequest(new {baseUrl=address,token=Token(owner),avatarId=owner,
                        otherToken=Token(member),otherId=member,providerType="MongoDBOASIS"},TimeSpan.FromMinutes(2));
                    Assert.False(result.RootElement.TryGetProperty("isError",out var error)&&error.GetBoolean(),
                        result.RootElement.TryGetProperty("message",out var message)?message.GetString():"Actual Karma browser check failed.");
                    Assert.True(result.RootElement.GetProperty("karmaPopupsVerified").GetBoolean());
                    SameRevisions(ownerBefore,await mongo.ReadNative(owner));SameRevisions(memberBefore,await mongo.ReadNative(member));
                    Console.WriteLine("Karma popup native browser verified: actual SDK/controller/Mongo; 51 paged records, signed points, token-free cache, account switch, anonymous denial; original revisions preserved.");
                }
                finally { using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await app.StopAsync(stop.Token); }
            }
            finally
            {
                if(avatarField!=null){avatarField.SetValue(null,previousAvatar);Assert.Same(previousAvatar,avatarField.GetValue(null));}
                AssemblyLoadContext.Default.Resolving -= Resolve;
                Console.WriteLine("Owned Karma browser managers restored.");
            }
        }
        Console.WriteLine("Owned Karma Mongo scope disposed and database absence verified.");
    }
}
