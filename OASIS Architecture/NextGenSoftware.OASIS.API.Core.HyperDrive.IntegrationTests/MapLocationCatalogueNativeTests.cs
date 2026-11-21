using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task MapCatalogueQueriesUsePersistedLocationsAcrossOwnedMongoAndSqlite()
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
                    async Task<JsonDocument> Call(string name,params object?[] args)
                    {
                        var task=(Task)managerType.GetMethod(name)!.Invoke(Cold(),args)!;await task;
                        return JsonDocument.Parse(JsonSerializer.Serialize(task.GetType().GetProperty("Result")!.GetValue(task),
                            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                    }
                    Guid[] Ids(JsonDocument value)
                    {
                        Assert.False(value.RootElement.GetProperty("isError").GetBoolean(),value.RootElement.GetProperty("message").GetString());
                        return value.RootElement.GetProperty("result").EnumerateArray().Select(row=>row.GetProperty("id").GetGuid()).ToArray();
                    }
                    async Task<IHolon> Save(string name,double latitude,double longitude,bool active=true,bool isPublic=true,Guid? owner=null,Guid? parent=null)
                    {
                        var value=Activator.CreateInstance(locationType)!;
                        void Set(string property,object content)=>locationType.GetProperty(property)!.SetValue(value,content);
                        Set("Id",Guid.NewGuid());Set("Name",name);Set("Description","Searchable persisted destination");
                        Set("Latitude",latitude);Set("Longitude",longitude);Set("IsActive",active);
                        var row=(IHolon)locationType.GetMethod("ToHolon")!.Invoke(value,null)!;
                        row.IsPublic=isPublic;if(parent.HasValue)row.ParentHolonId=parent.Value;
                        var saved=await Data().SaveHolonAsync(row,owner??actor,saveChildren:false);
                        Assert.False(saved.IsError,saved.Message);Assert.True(saved.IsSaved);Assert.NotNull(saved.Result);
                        return saved.Result;
                    }
                    using(var empty=await Call("SearchLocationsAsync",null,null,null,null,null))Assert.Empty(Ids(empty));
                    var centre=await Save("Persisted zero origin",0,0);
                    var near=await Save("Persisted near origin",0,0.001);
                    await Save("Persisted distant location",0,20);
                    await Save("Inactive location",0,0,active:false);
                    await Save("Foreign private location",0,0,isPublic:false,owner:other);
                    var own=await Save("Owned private location",0,0,isPublic:false);
                    await Save("Wrong parent",0,0,parent:Guid.NewGuid());
                    var east=await Save("Date line east",0,179.9999);var west=await Save("Date line west",0,-179.9999);
                    var nativeBefore=await store.Native(centre.Id);
                    using(var nearby=await Call("GetNearbyLocationsAsync",actor,0d,0d,1d))
                    {
                        Assert.Equal(new[]{centre.Id,own.Id}.OrderBy(id=>id).Append(near.Id),Ids(nearby));
                        var rows=nearby.RootElement.GetProperty("result").EnumerateArray().ToArray();
                        Assert.Equal(0,rows[0].GetProperty("distance").GetDouble());
                        Assert.InRange(rows[2].GetProperty("distance").GetDouble(),111,112);
                    }
                    using(var repeated=await Call("GetNearbyLocationsAsync",actor,0d,0d,1d))Assert.Contains(centre.Id,Ids(repeated));
                    using(var text=await Call("SearchLocationsAsync","PERSISTED ZERO",null,null,null,null))Assert.Equal(centre.Id,Assert.Single(Ids(text)));
                    using(var dateline=await Call("GetNearbyLocationsAsync",actor,0d,180d,1d))Assert.Equal(new[]{east.Id,west.Id}.OrderBy(id=>id),Ids(dateline).OrderBy(id=>id));
                    foreach(var args in new object?[][] { [actor,double.NaN,0d,1d], [actor,91d,0d,1d], [actor,0d,0d,0d], [actor,0d,0d,double.PositiveInfinity], [other,0d,0d,1d] })
                    { using var invalid=await Call("GetNearbyLocationsAsync",args);Assert.True(invalid.RootElement.GetProperty("isError").GetBoolean()); }
                    using(var partial=await Call("SearchLocationsAsync",null,null,0d,null,1d))Assert.True(partial.RootElement.GetProperty("isError").GetBoolean());
                    using(var missing=await Call("VisitLocationAsync",actor,Guid.NewGuid(),null))Assert.True(missing.RootElement.GetProperty("isError").GetBoolean());
                    Assert.Equal(nativeBefore,await store.Native(centre.Id));
                    var invalidRow=await Save("Invalid persisted coordinates",91,0);
                    using(var invalid=await Call("SearchLocationsAsync",null,null,null,null,null))Assert.True(invalid.RootElement.GetProperty("isError").GetBoolean());
                    Assert.Equal(actorBefore,await store.Account(actor));Assert.Equal(otherBefore,await store.Account(other));
                    Console.WriteLine($"Map native catalogue verified: {store.Provider.ProviderType.Value}; actual ToHolon/save/cold query, stable IDs, zero and near distances, date line, active/private/parent filtering, validation and unchanged accounts; no shared backend.");
                }
            }
            Console.WriteLine("Owned Map Mongo and SQLite scopes disposed and database absence verified.");
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
}
