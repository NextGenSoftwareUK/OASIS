using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task SubscriptionRepositoryReopenVerifiesIndexesWithoutSchemaWrites()
    {
        RequireOwnedFixtures();
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"Docker/oasis-dependency-versions.env")))root=root.Parent;
        Assert.NotNull(root);
        var configuration=typeof(AvatarProfileCommitStampNativeTests).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var artifact=Path.Combine(root!.FullName,"ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/bin",configuration,
            "net10.0/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll");
        Assert.True(File.Exists(artifact),"Build the actual WEB4 API before this test.");
        var resolver=new AssemblyDependencyResolver(artifact);
        Assembly? Resolve(AssemblyLoadContext context,AssemblyName name)
        {var path=resolver.ResolveAssemblyToPath(name);return path==null?null:context.LoadFromAssemblyPath(path);}
        AssemblyLoadContext.Default.Resolving+=Resolve;
        try
        {
            var api=AssemblyLoadContext.Default.LoadFromAssemblyPath(artifact);
            await using var ledger=await SubscriptionSettingsFixture.Create(api);
            var repositoryType=api.GetType("NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.MongoSubscriptionUsageRepository",true)!;
            var tag="owned-subscription-index-"+Guid.NewGuid().ToString("N");
            var uri=new MongoUrlBuilder(MongoUri){ApplicationName=tag}.ToString();
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                ["SUBSCRIPTION_MONGODB_CONNECTION_STRING"]=uri,
                ["SUBSCRIPTION_MONGODB_DATABASE"]=ledger.Database.DatabaseNamespace.DatabaseName,
                ["SUBSCRIPTION_LEDGER_INITIAL_STATE"]="empty-new-installation"
            }).Build();
            var admin=new MongoClient(MongoUri).GetDatabase("admin");
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"createIndexes"}},
                        {"errorCode",8000},{"appName",tag}}}
                });
                var reopened=Activator.CreateInstance(repositoryType,[config])!;
                var read=(Task)repositoryType.GetMethod("GetSubscriptionAsync")!
                    .Invoke(reopened,[ledger.ForeignId.ToString("D"),CancellationToken.None])!;
                await read;
                var record=read.GetType().GetProperty("Result")!.GetValue(read)!;
                Assert.Equal("gold",record.GetType().GetProperty("PlanId")!.GetValue(record));
                var probe=new MongoClient(uri).GetDatabase(ledger.Database.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>("owned_index_write_probe");
                var blocked=await Assert.ThrowsAsync<MongoCommandException>(()=>probe.Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(new BsonDocument("value",1),new CreateIndexOptions{Name="owned_probe"})));
                Assert.Equal(8000,blocked.Code);
                Console.WriteLine("Subscription index read-only reopen verified: actual repository and ledger read succeed while tagged native createIndexes commands are blocked; explicit write probe refused.");
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                Console.WriteLine("Owned subscription index failpoint disabled.");
            }
            var grants=ledger.Database.GetCollection<BsonDocument>("subscription_usage_grants");
            BsonDocument requiredGrantKeys;
            using(var cursor=await grants.Indexes.ListAsync())
                requiredGrantKeys=Assert.Single((await cursor.ToListAsync()).Where(item=>item["name"]=="ix_grant_owner"))
                    ["key"].AsBsonDocument.DeepClone().AsBsonDocument;
            await grants.Indexes.DropOneAsync("ix_grant_owner");
            await grants.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("owned_wrong_key",1),
                new CreateIndexOptions{Name="ix_grant_owner"}));
            var failure=Assert.Throws<TargetInvocationException>(()=>Activator.CreateInstance(repositoryType,[config]));
            var mismatch=Assert.IsType<InvalidOperationException>(failure.InnerException);
            Assert.Contains("ix_grant_owner",mismatch.Message);
            using(var cursor=await grants.Indexes.ListAsync())
            {
                var index=Assert.Single((await cursor.ToListAsync()).Where(item=>item["name"]=="ix_grant_owner"));
                Assert.Equal(new BsonDocument("owned_wrong_key",1),index["key"].AsBsonDocument);
            }
            await grants.Indexes.DropOneAsync("ix_grant_owner");
            Assert.NotNull(Activator.CreateInstance(repositoryType,[config]));
            using(var cursor=await grants.Indexes.ListAsync())
            {
                var index=Assert.Single((await cursor.ToListAsync()).Where(item=>item["name"]=="ix_grant_owner"));
                Assert.Equal(requiredGrantKeys,index["key"].AsBsonDocument);
            }
            Console.WriteLine("Subscription index definition integrity verified: mismatched existing index refused and preserved; missing required index created with exact declared keys.");
        }
        finally {AssemblyLoadContext.Default.Resolving-=Resolve;}
    }
}
