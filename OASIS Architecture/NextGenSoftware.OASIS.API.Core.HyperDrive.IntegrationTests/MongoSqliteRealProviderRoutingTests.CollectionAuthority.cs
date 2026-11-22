using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    [Fact]
    public async Task CollectionAuthorityPreservesThreeNativeActorScopesAgainstPartialReplica()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!=uri)
            throw new InvalidOperationException("Only the owned loopback MongoDB fixture is admitted.");
        var database="hd_collection_authority_"+Guid.NewGuid().ToString("N");
        Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(database),1,63);
        var client=new MongoClient(uri);
        Assert.DoesNotContain(database,await(await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory=Directory.CreateTempSubdirectory("oasis-collection-authority-");
        var mongo=new MongoDBOASIS(uri,database);
        try
        {
            using var sqlite=new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName,"collections.db")};Pooling=False");
            var activated=await mongo.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
            activated=await sqlite.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
            var ownerA=Guid.NewGuid();var ownerB=Guid.NewGuid();var service=Guid.NewGuid();var foreign=Guid.NewGuid();
            var tag=Guid.NewGuid().ToString("N");
            var rows=new[]{
                new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=ownerA,Name="first owner private"},
                new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=ownerA,Name="first owner second private"},
                new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=ownerB,Name="second owner private"},
                new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=foreign,Name="public one",IsPublic=true},
                new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=foreign,Name="public two",IsPublic=true},
                new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=foreign,Name="foreign private"}};
            foreach(var row in rows) {
                row.IsActive=true;row.MetaData["Status"]=tag;row.MetaData["Kind"]="district";
                var saved=await mongo.SaveHolonAsync(row,saveChildren:false);
                Assert.True(saved.IsSaved&&!saved.IsError,saved.Message);
            }
            var replicaSave=await sqlite.SaveHolonAsync(rows[3],saveChildren:false);
            Assert.True(replicaSave.IsSaved&&!replicaSave.IsError,replicaSave.Message);
            var subset=await sqlite.LoadAllHolonsAsync(loadChildren:false);
            Assert.False(subset.IsError,subset.Message);Assert.Equal(rows[3].Id,Assert.Single(subset.Result).Id);

            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings(),
                SubscriptionConfig=new(){PlanType="Gold"}}};
            var providers=new ProviderManager(null,dna){
                IsAutoLoadBalanceEnabled=true,IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
            providers.RegisterProvider(mongo);providers.RegisterProvider(sqlite);
            Assert.False(providers.SetAndActivateCurrentStorageProvider(mongo).IsError);
            providers.AddProvidersToAutoLoadBalanceList(new[]{ProviderType.SQLLiteDBOASIS});
            var router=new OASISHyperDrive(providers);
            var cases=new[]{
                (Operation:"LoadAllHolons",Actor:ownerA,Expected:new[]{rows[0].Id,rows[1].Id,rows[3].Id,rows[4].Id}),
                (Operation:"LoadHolonsByMetaData",Actor:ownerB,Expected:new[]{rows[2].Id,rows[3].Id,rows[4].Id}),
                (Operation:"LoadHolonsByMetaDataPairs",Actor:service,Expected:new[]{rows[3].Id,rows[4].Id})};
            foreach(var item in cases) {
                var request=new StorageOperationRequest{Operation=item.Operation,AvatarId=item.Actor,IncludePublic=true,
                    LoadChildren=false,Recursive=false,MetaKey="Status",MetaValue=tag,
                    MetaKeyValuePairs=new(){{"Status",tag},{"Kind","district"}},MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All};
                var result=await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
                Assert.False(result.IsError,result.Message);
                Assert.Equal(item.Expected.Order(),result.Result.Select(row=>row.Id).Order());
                Assert.DoesNotContain(result.Result,row=>row.Id==rows[5].Id);
            }
            var privateOnly=await router.RouteRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest{
                Operation="LoadAllHolons",AvatarId=ownerA,IncludePublic=false,LoadChildren=false,Recursive=false});
            Assert.False(privateOnly.IsError,privateOnly.Message);
            Assert.Equal(new[]{rows[0].Id,rows[1].Id}.Order(),privateOnly.Result.Select(row=>row.Id).Order());
            Assert.Same(mongo,providers.CurrentStorageProvider);
            // These are isolated synthetic owner/public scopes, not PIPER account/business-role acceptance.
        }
        finally
        {
            try {
                await mongo.DeActivateProviderAsync();
                await client.DropDatabaseAsync(database);
                Assert.DoesNotContain(database,await(await client.ListDatabaseNamesAsync()).ToListAsync());
            }
            finally {
                var temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                Assert.Equal(temp,Directory.GetParent(Path.GetFullPath(directory.FullName))!.FullName);
                Assert.StartsWith("oasis-collection-authority-",directory.Name);
                directory.Delete(recursive:true);
                Assert.False(Directory.Exists(directory.FullName));
            }
        }
    }
}
