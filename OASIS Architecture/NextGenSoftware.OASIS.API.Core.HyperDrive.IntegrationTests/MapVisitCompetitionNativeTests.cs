using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task MapVisitsPersistScoresAndExposeActualCompetitionFailuresAcrossOwnedStores()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Docker/oasis-dependency-versions.env"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = GetType().Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var artifact = Path.Combine(root!.FullName, "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin", configuration,
            "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact), "Build the actual WEB4 API before the Map native case.");
        var resolver = new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        { var path = resolver.ResolveAssemblyToPath(name); return path == null ? null : context.LoadFromAssemblyPath(path); }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var onode = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(artifact)!,
                "NextGenSoftware.OASIS.API.ONODE.Core.dll"));
            var managerType = onode.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Managers.MapManager", true)!;
            var locationType = onode.GetType("NextGenSoftware.OASIS.API.ONODE.Core.Managers.MapLocation", true)!;
            var actor = Guid.NewGuid(); var other = Guid.NewGuid();
            await using (var mongo = new MongoStore())
            {
                await mongo.Activate();
                using var sqlite = new SqliteStore();
                Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
                var stores = new (IOASISStorageProvider Provider, Func<Guid, Task<string>> Native, Func<Guid, Task<string>> Account)[] {
                    (mongo.Provider, mongo.ReadHolonDocumentNativeAsync, mongo.ReadDocumentNativeAsync),
                    (sqlite.Provider, sqlite.ReadHolonMetadataJsonNativeAsync, sqlite.ReadMetadataJsonNativeAsync)
                };
                foreach(var store in stores)
                {
                    await SeedRoutingStores([store.Provider],actor);await SeedRoutingStores([store.Provider],other);
                    var actorBefore=await store.Account(actor);var otherBefore=await store.Account(other);
                    var routing=Routing([store.Provider],false);
                    Assert.False(routing.PublishRoutingOverrides(new ProviderRoutingOverrides(
                        autoFailoverEnabled:false,autoReplicationEnabled:false,autoLoadBalancingEnabled:false),
                        store.Provider.ProviderType.Value).IsError);
                    HolonManager Data()=>new(store.Provider,routing.OASISDNA,routing);
                    object Cold()=>Activator.CreateInstance(managerType,[Data(),actor])!;
                    CompetitionManager Scores()=>CompetitionManager.ForData(Data());
                    var globalProvider=ProviderManager.Instance.CurrentStorageProvider;
                    async Task<JsonDocument> Call(string name,params object?[] args)
                    {
                        var task=(Task)managerType.GetMethod(name)!.Invoke(Cold(),args)!;await task;
                        return JsonDocument.Parse(JsonSerializer.Serialize(task.GetType().GetProperty("Result")!.GetValue(task),
                            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                    }
                    var location=Activator.CreateInstance(locationType)!;
                    void Set(string property,object content)=>locationType.GetProperty(property)!.SetValue(location,content);
                    var locationId=Guid.NewGuid();Set("Id",locationId);Set("Name","Owned scored destination");
                    Set("Latitude",0d);Set("Longitude",0d);Set("IsActive",true);
                    var row=(IHolon)locationType.GetMethod("ToHolon")!.Invoke(location,null)!;row.IsPublic=true;
                    var saved=await Data().SaveHolonAsync(row,actor,saveChildren:false);
                    Assert.False(saved.IsError,saved.Message);Assert.True(saved.IsSaved);
                    var locationBefore=await store.Native(locationId);
                    using(var visit=await Call("VisitLocationAsync",actor,locationId,"Native visit one"))
                    {
                        Assert.False(visit.RootElement.GetProperty("isError").GetBoolean(),visit.RootElement.GetProperty("message").GetString());
                        Assert.True(visit.RootElement.GetProperty("result").GetBoolean());Assert.True(visit.RootElement.GetProperty("isSaved").GetBoolean());
                        Assert.False(visit.RootElement.GetProperty("isWarning").GetBoolean());
                    }
                    foreach(var season in new[]{SeasonType.Daily,SeasonType.Weekly,SeasonType.Monthly})
                    {
                        var score=await Scores().GetLeaderboardAsync(CompetitionType.SocialActivity,season);
                        Assert.False(score.IsError,score.Message);var entry=Assert.Single(score.Result);
                        Assert.Equal(actor,entry.AvatarId);Assert.Equal(5,entry.Score);
                    }
                    using(var history=await Call("GetVisitHistoryAsync",actor,50,0))
                    {
                        Assert.False(history.RootElement.GetProperty("isError").GetBoolean());
                        var visit=Assert.Single(history.RootElement.GetProperty("result").EnumerateArray());
                        Assert.Equal(locationId,visit.GetProperty("locationId").GetGuid());Assert.Equal(actor,visit.GetProperty("avatarId").GetGuid());
                        Assert.Contains(locationId.ToString(),await store.Native(visit.GetProperty("id").GetGuid()));
                    }
                    var category=(string)typeof(CompetitionManager).GetMethod("GetLeaderboardCategory",BindingFlags.Instance|BindingFlags.NonPublic)!
                        .Invoke(Scores(),[CompetitionType.SocialActivity,SeasonType.Weekly])!;
                    var invalid=await Data().SaveSettingAsync(Guid.Empty,category,"entries","invalid native competition snapshot");
                    Assert.False(invalid.IsError,invalid.Message);Assert.True(invalid.Result);
                    var weeklyId=(Guid)typeof(HolonManager).GetMethod("GetSettingsHolonId",BindingFlags.Static|BindingFlags.NonPublic)!
                        .Invoke(null,[Guid.Empty,category])!;
                    var corruptBefore=await store.Native(weeklyId);
                    using(var partial=await Call("VisitLocationAsync",actor,locationId,"Native visit with failed weekly score"))
                    {
                        Assert.False(partial.RootElement.GetProperty("isError").GetBoolean());
                        Assert.True(partial.RootElement.GetProperty("result").GetBoolean());Assert.True(partial.RootElement.GetProperty("isSaved").GetBoolean());
                        Assert.True(partial.RootElement.GetProperty("isWarning").GetBoolean());
                        Assert.Contains("Weekly",partial.RootElement.GetProperty("message").GetString());
                    }
                    Assert.Equal(corruptBefore,await store.Native(weeklyId));
                    foreach(var season in new[]{SeasonType.Daily,SeasonType.Monthly})
                    {
                        var score=await Scores().GetLeaderboardAsync(CompetitionType.SocialActivity,season);
                        Assert.False(score.IsError,score.Message);Assert.Equal(10,Assert.Single(score.Result).Score);
                    }
                    using(var history=await Call("GetVisitHistoryAsync",actor,50,0))
                    { Assert.False(history.RootElement.GetProperty("isError").GetBoolean());Assert.Equal(2,history.RootElement.GetProperty("result").GetArrayLength()); }
                    using(var stats=await Call("GetMapStatsAsync",actor))
                    {
                        Assert.False(stats.RootElement.GetProperty("isError").GetBoolean());var value=stats.RootElement.GetProperty("result");
                        Assert.Equal(2,value.GetProperty("totalVisits").GetInt32());Assert.Equal(1,value.GetProperty("uniqueLocations").GetInt32());
                        Assert.Equal(10,value.GetProperty("totalScore").GetInt64());
                    }
                    Assert.Equal(locationBefore,await store.Native(locationId));
                    Assert.Equal(actorBefore,await store.Account(actor));Assert.Equal(otherBefore,await store.Account(other));
                    Assert.Same(globalProvider,ProviderManager.Instance.CurrentStorageProvider);
                    Console.WriteLine($"Map native visits verified: {store.Provider.ProviderType.Value}; actual scoped primary save/history/stats/three score periods; actual malformed weekly snapshot yields explicit saved-visit warning without overwrite; global provider and accounts unchanged.");
                }
            }
            Console.WriteLine("Owned scored-visit Mongo and SQLite scopes disposed and database absence verified.");
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
