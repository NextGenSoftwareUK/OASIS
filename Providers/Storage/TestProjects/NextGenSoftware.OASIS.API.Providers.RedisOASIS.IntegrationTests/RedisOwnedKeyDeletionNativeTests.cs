using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

// The former plaintext identity-deletion fixture is now covered by RedisPrivateProviderNativeTests.
// Keep the old format only as explicit refusal evidence, never as a second production reader.
[TestClass][DoNotParallelize]
public class RedisOwnedKeyDeletionNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("OwnedNativeLegacyLayoutAdmission")]
    public async Task LegacyRecordRefusesActivationWithoutMutation(bool sync)
    {
        const string endpoint="127.0.0.1:18679";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_WORK")??throw new InvalidOperationException("Explicit evidence directory required."));
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/RedisCurrent"),work);
        var stem=Path.Combine(work,sync?"legacy-admission-sync":"legacy-admission-async");
        foreach(var suffix in new[]{".pending.json",".resolved.json"})Assert.IsFalse(File.Exists(stem+suffix),"Preserve predecessor proof.");
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(RedisOASIS.StorageKeyEnvironmentVariable);
        try
        {
            var options=ConfigurationOptions.Parse(endpoint);options.AbortOnConnectFail=true;options.ConnectRetry=0;options.BacklogPolicy=BacklogPolicy.FailFast;
            options.Password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD");Assert.IsFalse(string.IsNullOrWhiteSpace(options.Password));
            using var verifier=await ConnectionMultiplexer.ConnectAsync(options);var db=verifier.GetDatabase();var server=verifier.GetServer(endpoint);
            Assert.AreEqual(0L,await server.DatabaseSizeAsync(db.Database),"No existing row may be claimed as a synthetic legacy fixture.");
            var id=Guid.NewGuid();RedisKey legacy="avatar:"+id;RedisValue value=JsonSerializer.Serialize(new{Id=id,Version=1});
            using(var intent=new FileStream(stem+".pending.json",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(intent,new{owner="redis-legacy-admission-v1",endpoint,key=legacy.ToString(),value=value.ToString()});intent.Flush(true);}
            Assert.IsTrue(await db.StringSetAsync(legacy,value,when:When.NotExists));
            using var provider=new RedisOASIS(options.ToString(includePassword:true),()=>(byte[])key.Clone());
            var activated=sync?provider.ActivateProvider():await provider.ActivateProviderAsync();
            Assert.IsTrue(activated.IsError);Assert.IsFalse(provider.IsProviderActivated);
            StringAssert.Contains(activated.Exception?.ToString()??activated.Message,"REDIS_LEGACY_LAYOUT_REVIEW_REQUIRED");
            Assert.AreEqual(value,await db.StringGetAsync(legacy));Assert.AreEqual(1L,await server.DatabaseSizeAsync(db.Database),"Rejected activation must not migrate/write a probe.");
            var cleanup=db.CreateTransaction();cleanup.AddCondition(Condition.StringEqual(legacy,value));var deleted=cleanup.KeyDeleteAsync(legacy);
            Assert.IsTrue(await cleanup.ExecuteAsync());Assert.IsTrue(await deleted);Assert.AreEqual(0L,await server.DatabaseSizeAsync(db.Database));
            File.Move(stem+".pending.json",stem+".resolved.json");
        }
        finally{CryptographicOperations.ZeroMemory(key);}
    }
}
