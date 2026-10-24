using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class RedisPrivateProviderNativeTests
{
    [TestMethod][TestCategory("OwnedNativePrivatePreflight")]
    public async Task ExistingDedicatedStoreIsEmptyBeforeFirstStorageKeyInitialization()
    {
        const string endpoint="127.0.0.1:18679";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_WORK")??throw new InvalidOperationException("Explicit evidence directory required."));
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/RedisCurrent"),work);
        var options=ConfigurationOptions.Parse(endpoint);options.AbortOnConnectFail=true;options.ConnectRetry=0;options.BacklogPolicy=BacklogPolicy.FailFast;
        options.Password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD");Assert.IsFalse(string.IsNullOrWhiteSpace(options.Password));
        using var connection=await ConnectionMultiplexer.ConnectAsync(options);
        var database=connection.GetDatabase();Assert.AreEqual(0,database.Database);
        var count=await connection.GetServer(endpoint).DatabaseSizeAsync(database.Database);Assert.AreEqual(0L,count,"Existing data requires recovery, never replacement keys.");
        using var proof=new FileStream(Path.Combine(work,"private-provider-empty.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None);
        JsonSerializer.Serialize(proof,new{endpoint,database=database.Database,count,observedUtc=DateTimeOffset.UtcNow});proof.Flush(true);
    }

    // Dedicated retained DB must be empty at admission. Never initialize a replacement key here.
    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("OwnedNativePrivateProvider")]
    public async Task PrivateProviderPreservesInvalidKeyHistoryInventoryAndOwnedCleanup(bool sync)
    {
        const string endpoint="127.0.0.1:18679";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_WORK")??throw new InvalidOperationException("Explicit evidence directory required."));
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/RedisCurrent"),work);
        var stem=Path.Combine(work,sync?"private-provider-sync":"private-provider-async");
        foreach(var suffix in new[]{".pending.json",".resolved.json"})Assert.IsFalse(File.Exists(stem+suffix),"Preserve predecessor evidence.");
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(RedisOASIS.StorageKeyEnvironmentVariable);
        try
        {
            var options=ConfigurationOptions.Parse(endpoint);options.AbortOnConnectFail=true;options.ConnectRetry=0;options.BacklogPolicy=BacklogPolicy.FailFast;
            options.Password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD");Assert.IsFalse(string.IsNullOrWhiteSpace(options.Password));
            using var verifier=await ConnectionMultiplexer.ConnectAsync(options);
            var server=verifier.GetServer(endpoint);var db=verifier.GetDatabase();
            Assert.AreEqual(0L,await server.DatabaseSizeAsync(db.Database),"Dedicated store must be empty; never claim existing records.");
            var id=Guid.NewGuid();var holonId=Guid.NewGuid();var username="owned-private-redis-"+id.ToString("N");
            using var provider=new RedisOASIS(options.ToString(includePassword:true),()=>(byte[])key.Clone(),"oasis/redis-native/"+id.ToString("N")+"/"){IsVersionControlEnabled=true};
            var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
            RedisKey sentinel="foreign-native-guard/"+id.ToString("N");RedisValue sentinelValue="owned independent namespace sentinel "+id;
            using(var intent=new FileStream(stem+".pending.json",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(intent,new{owner="redis-private-provider-native-v1",id,holonId,username,endpoint,initialDatabaseSize=0,sentinel=sentinel.ToString(),sentinelValue=sentinelValue.ToString()});intent.Flush(true);}
            try
            {
                Assert.IsTrue(await db.StringSetAsync(sentinel,sentinelValue,when:When.NotExists));
                var profile=new Avatar{Id=id,Username=username,Email=username+"@example.invalid",IsActive=true};
                var detail=new AvatarDetail{Id=id,Username=username,Email=profile.Email,IsActive=true,XP=321,Karma=29};
                detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="owned preserved inventory"});
                var first=sync?provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile);Assert.IsFalse(first.IsError,first.Message);
                first.Result.Description="updated";
                var second=sync?provider.SaveAvatar(first.Result):await provider.SaveAvatarAsync(first.Result);Assert.IsFalse(second.IsError,second.Message);Assert.AreEqual(2,second.Result.Version);
                var saved=sync?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);
                var holon=new Holon{Id=holonId,IsActive=true,Description="owned native lifecycle"};
                var savedHolon=sync?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(savedHolon.IsError,savedHolon.Message);
                var keys=new List<RedisKey>();await foreach(var physical in server.KeysAsync(db.Database,"oasis/private/v1/*"))keys.Add(physical);
                Assert.IsTrue(keys.Count>0);Assert.AreEqual((long)keys.Count+1,await server.DatabaseSizeAsync(db.Database));
                var before=await db.StringGetAsync(keys.ToArray());
                Assert.IsTrue(keys.All(physical=>!physical.ToString().Contains(username,StringComparison.Ordinal)));
                Assert.IsTrue(before.All(value=>!value.ToString().Contains(username,StringComparison.Ordinal)),"Private identity must not be plaintext on the native wire.");
                var rejected=sync?provider.DeleteAvatar(username,false):await provider.DeleteAvatarAsync(username,false);
                Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<ArgumentException>(rejected.Exception);Assert.IsFalse(rejected.IsDeleted);Assert.AreEqual(0,rejected.DeletedCount);
                CollectionAssert.AreEqual(before,await db.StringGetAsync(keys.ToArray()),"Rejected key cannot change encrypted records/history/indexes.");
                Assert.AreEqual(1,(await provider.LoadAvatarAsync(id,1)).Result.Version);
                var loaded=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(loaded.IsError,loaded.Message);
                Assert.AreEqual(321L,loaded.Result.XP);Assert.AreEqual(29L,loaded.Result.Karma);Assert.AreEqual("owned preserved inventory",loaded.Result.Inventory.Single().Name);
                Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
                Assert.IsFalse((await provider.ActivateProviderAsync()).IsError,"Same persisted key and store must reactivate.");
                var soft=sync?provider.DeleteHolon(holonId):await provider.DeleteHolonAsync(holonId);
                Assert.IsFalse(soft.IsError,soft.Message);Assert.IsTrue(soft.Result.IsDeleted);Assert.AreEqual(1,soft.DeletedCount);Assert.AreEqual(2,soft.Result.Version);
                var historical=await provider.LoadHolonAsync(holonId,loadChildren:false,version:1);Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.IsFalse(historical.Result.IsDeleted);
                var hardHolon=sync?provider.DeleteHolonSoft(holonId,false):await provider.DeleteHolonSoftAsync(holonId,false);Assert.IsFalse(hardHolon.IsError,hardHolon.Message);Assert.IsTrue(hardHolon.Result);
                var deleted=sync?provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false);Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsDeleted);
                Assert.AreEqual(sentinelValue,await db.StringGetAsync(sentinel),"Provider cleanup must preserve unrelated namespace data.");
                Assert.AreEqual(1L,await server.DatabaseSizeAsync(db.Database),"Only the independent sentinel may remain after public cleanup.");
                var cleanup=db.CreateTransaction();cleanup.AddCondition(Condition.StringEqual(sentinel,sentinelValue));
                var removed=cleanup.KeyDeleteAsync(sentinel);Assert.IsTrue(await cleanup.ExecuteAsync(),"Sentinel cleanup requires exact observed value.");Assert.IsTrue(await removed);
                Assert.AreEqual(0L,await server.DatabaseSizeAsync(db.Database),"All owned fixture cleanup requires native absence readback.");
                File.Move(stem+".pending.json",stem+".resolved.json");
            }
            finally{await provider.DeActivateProviderAsync();}
        }
        finally{CryptographicOperations.ZeroMemory(key);}
    }
}
